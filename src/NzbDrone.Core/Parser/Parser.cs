using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Parser
{
    public static class Parser
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(Parser));

        private static readonly Regex EditionRegex = new Regex(@"\(?\b(?<edition>(((Recut.|Extended.|Ultimate.)?(Director.?s|Collector.?s|Theatrical|Ultimate|Extended|Despecialized|(Special|Rouge|Final|Assembly|Imperial|Diamond|Signature|Hunter|Rekall)(?=(.(Cut|Edition|Version)))|\d{2,3}(th)?.Anniversary)(?:.(Cut|Edition|Version))?(.(Extended|Uncensored|Remastered|Unrated|Uncut|Open.?Matte|IMAX|Fan.?Edit))?|((Uncensored|Remastered|Unrated|Uncut|Open?.Matte|IMAX|Fan.?Edit|Restored|((2|3|4)in1))))))\b\)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Note: Using EditionRegex directly instead of ^.+? prefix to avoid catastrophic backtracking
        // The old pattern (^.+? + EditionRegex) caused O(n²) or worse performance as the regex engine
        // tried every possible split point in the string when the edition pattern didn't match early.

        private static readonly Regex HardcodedSubsRegex = new Regex(@"\b((?<hcsub>(\w+(?<!SOFT|MULTI|HORRIBLE)SUBS?))|(?<hc>(HC|SUBBED)))\b",
                                                        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex[] ReportGameTitleRegex = new[]
        {
            // Scene release with version AND platform suffix: "Sandwalkers.v2.2.3.Linux-I_KnoW" → "Sandwalkers"
            // MUST BE FIRST - Strip version and platform suffix before release group
            new Regex(@"^(?<title>.+?)[._]v\d+(?:[._]\d+)*[._](?:Linux|MacOS|Mac|Win(?:dows)?|x64|x86)-(?<releasegroup>PLAZA|CODEX|SKIDROW|CPY|EMPRESS|FLT|HOODLUM|RAZOR1911|RAZOR|RazorDOX|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|CHRONOS|SiMPLEX|RUNE|HI2U|TENOKE|DELiGHT|DINOByTES|bADkARMA|PLAYMAGiC|voices38|I_KnoW|GOG)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Scene release with platform suffix (dot-separated, no version): "Lust.Theory.Season.3.MacOS-I_KnoW" → "Lust Theory Season 3"
            // MUST BE EARLY - Strip platform suffix using greedy match
            new Regex(@"^(?<title>.+?)\.(?:Linux|MacOS|Mac|Win(?:dows)?|x64|x86)-(?<releasegroup>PLAZA|CODEX|SKIDROW|CPY|EMPRESS|FLT|HOODLUM|RAZOR1911|RAZOR|RazorDOX|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|CHRONOS|SiMPLEX|RUNE|HI2U|TENOKE|DELiGHT|DINOByTES|bADkARMA|PLAYMAGiC|voices38|I_KnoW|GOG)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // German language releases: "Game.German-DELiGHT" → "Game"
            // MUST BE EARLY - Strip .German suffix before release group
            new Regex(@"^(?<title>.+?)[._]German-(?<releasegroup>DELiGHT|RUNE|TENOKE|CODEX|PLAZA|SKIDROW)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Update/Patch releases with version: "Game.Name.Update.v1.2.3-GROUP" or "Game.Name.Update.3.0-GROUP" - MUST BE BEFORE version regex
            // Title stops at ".Update." or " Update " - handles both dot and space separated formats
            // Now handles version with or without 'v' prefix
            new Regex(@"^(?<title>(?![(\[]).+?)[._\s](?:Update|Patch)[._\s]+v?[\d.]+.*?-(?<releasegroup>[A-Za-z0-9_]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // GOG release with version: GameName.v1.2.3.GOG-DELiGHT or GameName.v1.0.hotfix5.GOG-DELiGHT
            // Must be before general version regex to handle .GOG. marker between version and group
            // Only matches DELiGHT group (other groups like DINOByTES keep version in title)
            new Regex(@"^(?<title>(?![(\[]).+?)[._]v\d+[^-]*[._]GOG-(?<releasegroup>DELiGHT)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Game release with version (but NOT Update releases): GameName.v1.2.3-GROUP or GameName.v1.4.0g-GROUP
            // Negative lookbehind ensures we don't match if title ends with ".Update" or "_Update"
            // Handles letter suffixes like v1.4.0g, v0.4.2f7
            new Regex(@"^(?<title>(?![(\[]).+?(?<![._]Update))[._]v(?<version>\d+(?:\.\d+)*[a-z]*\d*)[._-](?<releasegroup>[A-Za-z0-9_]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Game release with date-based version: "Hades II v2025.08.03" or "Game Name v2025.06.18"
            // Must be before year patterns to avoid v2025 being parsed as year
            new Regex(@"^(?<title>(?![(\[]).+?)\s+v(?<year>\d{4})\.(?<version>\d{2}\.\d{2})$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Game release with European date-based version: "The Witness v24.01.2019" (DD.MM.YYYY format)
            new Regex(@"^(?<title>(?![(\[]).+?)\s+v(?<version>\d{2}\.\d{2})\.(?<year>\d{4})$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Game release with parenthesized version: "Hades II (v2025.06.18)" or "Game (v1.2.3)"
            new Regex(@"^(?<title>(?![(\[]).+?)\s*\(v(?<version>\d+(?:\.\d+)+)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Game release with parenthesized metadata and optional bracketed repack group:
            // "DARQ: Complete Edition (v1.3 + 2 DLCs, MULTi19) [FitGirl Repack]"
            // "Game Name (v2.0 + All DLCs, MULTi10) [DODI Repack]"
            // "Game Name (v1.5 + 3 DLCs)"
            // Note: [FitGirl Repack] may be stripped by CleanQualityBracketsRegex before matching
            new Regex(@"^(?<title>(?![(\[]).+?)\s*\(v\d+[^)]*\)(?:\s*\[(?:FitGirl|DODI|XATAB|Elamigos|CorePack|KaOs)[-_. ]*(?:Monkey\s+)?Repack\])?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // r4v3n release group format: "Game Title r4v3n Edition v123"
            // r4v3n is a scene-style group, title stops at "r4v3n"
            new Regex(@"^(?<title>(?![(\[]).+?)\s+(?<releasegroup>r4v3n)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // FitGirl/DODI bracket format with year-colon in title: "Dead Space (2023): Digital Deluxe Edition (Build...) [FitGirl Repack]"
            // Title includes the year when followed by colon (official game naming)
            // Stops at the Build/version parenthesis
            new Regex(@"^(?<title>.+?\(\d{4}\):\s*[^(]+?)\s*\([^)]+\)\s*\[(?:FitGirl|DODI)(?:\s+(?:Monkey\s+)?Repack)?(?:,\s*[^\]]+)?\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // FitGirl/DODI bracket format with year as metadata: "Game Remake (2023) Digital Deluxe Edition (Build...) [DODI Repack]"
            // Year without colon is release metadata - extract title before the year
            // Allows text between year and build info parentheses
            new Regex(@"^(?<title>[^(]+?)\s*\(\d{4}\)[^(\[]*\([^)]+\)\s*\[(?:FitGirl|DODI)(?:\s+(?:Monkey\s+)?Repack)?(?:,\s*[^\]]+)?\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // FitGirl/DODI bracket format with parenthetical info: "Game Name (v1.2.3 + DLC, MULTi##) [FitGirl Repack]"
            // Title stops at opening parenthesis of version info
            // Handles multiple parenthetical groups like "(2023) (Build 12345, MULTi13)" before [DODI Repack]
            new Regex(@"^(?<title>[^(]+?)\s*(?:\([^)]*\)\s*)+\[(?:FitGirl|DODI)(?:\s+(?:Monkey\s+)?Repack)?(?:,\s*[^\]]+)?\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Simple bracket format without version: "Game Name [FitGirl Repack]" or "Game Name [DODI Repack]" or "Hytale [DODI Repack]"
            // Also handles trailing info: "Game [FitGirl Repack, Selective Download - from 9.5 GB]"
            // Must come AFTER parenthetical patterns - this is the fallback for simple titles without parens
            new Regex(@"^(?<title>.+?)\s+\[(?:FitGirl|DODI)(?:\s+(?:Monkey\s+)?Repack)?(?:,\s*[^\]]+)?\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // PORTABLE release format: "Game Name (year) [PORTABLE]" or "Game Name (year) + DLC [PORTABLE]"
            // Title stops at opening parenthesis or before bracket
            new Regex(@"^(?<title>[^(\[]+?)(?:\s*\((?<year>\d{4})\))?(?:\s*\+[^\[]+)?\s*\[(?:Crack\s*V?\d*\.?\d*\s*)?\]?\s*\[PORTABLE\]$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // CRACK release format: "Game Name (year)[CRACK 1.1][AMD+Intel]"
            new Regex(@"^(?<title>[^(\[]+?)(?:\s*\((?<year>\d{4})\))?\s*\[CRACK\s*[\d.]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // CRACKFIX/PROPER format: "Game Name CRACKFIX-group" or "Game Name PROPER-group"
            new Regex(@"^(?<title>(?![(\[]).+?)\s+(?:CRACKFIX|PROPER)-(?<releasegroup>\w+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Simple game with version in parentheses: "Manor Lords (v2025.12.21)" or "Palworld (v0.7)"
            // Title stops at opening parenthesis with version, handles &/+ for DLCs
            new Regex(@"^(?<title>[^(\[]+?)\s*\(v[\d.]+(?:\s*[&+][^)]+)?\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Update/Patch/Language Pack releases: "Game Update v1.2-CODEX" or "Game.Update.v1.2-RUNE" or "Game.Update.3.0-RUNE"
            // Title stops at Update/Patch/Language Pack keywords, but not if version info comes before
            // Negative lookahead ensures we don't capture title with version info before "Patch"
            // Exclude strings starting with DL (handled by DL prefix pattern) and metadata patterns like "redkit update"
            // Handles both space-separated (Game Name Update v1.0) and dot-separated (Game.Name.Update.v1.0) formats
            // Now also handles version without 'v' prefix (Update.3.0 or Update 1.2.3)
            new Regex(@"^(?!DL\s)(?<title>(?![(\[])(?:(?![\s._]v\d).)+?)[.\s]+(?:Update|Patch|Language[\s._-]?Pack)(?:[.\s]+v?[\d.]|\s*[-_]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Simple game with space-separated version: "DuneCrawl v1.01" or "Schedule I v0.4.2f7"
            new Regex(@"^(?<title>(?![(\[]).+?)\s+v\d+(?:\.\d+)*[a-z]*\d*$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Year-hyphen-repacker format: "Game.Title.2018-FitGirl.Repack" → "Game Title"
            // Title stops at year when followed by hyphen and REPACKER (not scene groups)
            // Scene groups like PLAZA, CODEX keep the year in the title
            new Regex(@"^(?<title>(?![(\[]).+?)[._](?<year>(19|20)\d{2})[-_](?:FitGirl|DODI|XATAB|Elamigos|CorePack|KaOs)(?:[._-]?Repack)?", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Scene release ending with space + scene group (no hyphen): "Hogwarts Legacy Deluxe Edition EMPRESS"
            // Must be before flexible version matching to avoid partial matches
            // Note: GOG excluded here - handled by GOG-specific pattern below
            new Regex(@"^(?<title>.+?)\s+(?<releasegroup>CODEX|PLAZA|SKIDROW|CPY|EMPRESS|FLT|HOODLUM|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|RUNE|TENOKE|DELiGHT)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Russian repacker format: "Game Name 2016 PC RePack от xatab" or "Game Name v1.0 2022 PC RePack от R.G. Механики"
            // Title stops at version indicator (v followed by digits) or year before PC
            // Version pattern handles formats like "v 1.03u5" (letter+digit suffixes), DLC with or without count
            new Regex(@"^(?<title>(?![(\[]).+?)(?=\s+v\s*\d|\s+(19|20)\d{2}\s+PC)(?:\s+v[\s\d.]+(?:[a-z]+\d*)*(?:\s+(?:\d+\s+)?DLCs?)?)?(?:\s+(?<year>(19|20)\d{2}))?\s+PC\s+(?:RePack|Rip)\s+(?:от|by|from)\s+(?:xatab|R\.?G\.?\s*(?:Механики|Механіки|Mechanics|Catalyst|Freedom|SteamGames)?|FitGirl|DODI|Chovka|Decepticon|Wanterlude|Let.?sPlay)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Switch emulator format: "Nintendo Switch Game Name NSP/NSZ RUS Multi10" or "Game Name Switch Emulators"
            // Stop title at version number (v1 or similar) or at Switch marker if no version
            new Regex(@"^(?:Nintendo\s+Switch\s+)?(?<title>(?![(\[]).+?)(?=\s+v\d|\s+(?:NSP|NSZ|Switch\s+Emulators?|Ryujinx|Suyu)\b).*?(?:NSP|NSZ|Switch\s+Emulators?|Ryujinx|Suyu)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // DL prefix format with GOG ending: "DL Hollow Knight L RUS ENG 8 2017 Arcade ... GOG"
            // Title stops at "L" followed by language markers - must come before general GOG pattern
            new Regex(@"^DL\s+(?<title>(?![(\[]).+?)\s+L\s+(?:RUS|ENG|MULTi).+\bGOG$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // GOG release format without brackets: "Game Name GOG" or "Game Name live v4 1 1 win gog"
            // Stop before 'live', version indicators, or 'win/mac/linux' platform
            new Regex(@"^(?<title>(?![(\[]).+?)(?:\s+(?:live|v\s*\d|win|mac|linux|\d{4}\s+(?:Arcade|RPG|Adventure|FPS|TPS|Action|Strategy|Puzzle|Simulation|Sports|Racing|MMORPG)))\b.+?\bGOG\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // DL prefix format without brackets: "DL Game Name P RUS ENG 8 2017 Arcade..." (for stripped brackets)
            // P = Portable marker, L = some other marker - strip both from title
            new Regex(@"^(?:DL\s+)?(?<title>(?![(\[]).+?)\s+(?:P\s+)?(?:L\s+)?(?:RUS|ENG|MULTi)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // DODI/FitGirl repack with YEAR PC before version: "Title 2024 PC v1... DODI-Repack"
            // Title stops at year when followed by "PC" - must be before flexible version pattern
            new Regex(@"^(?<title>(?![(\[]).+?)(?=\s+(19|20)\d{2}\s+PC).*?(?:DODI|FitGirl)[-_. ]?Repack", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Flexible game release with version anywhere: "ELDEN RING Deluxe Edition v1.03.2" or "Game Name v1 2 3"
            // Captures title up to the first "v" followed by version-like numbers
            // Negative lookbehind prevents matching when title ends with Update/Patch (handled by Update-specific regex)
            new Regex(@"^(?<title>(?![(\[]).+?(?<!\s(?:Update|Patch)))\s+v\s*\d+[\s._]\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // FitGirl/DODI repack format with Build: "Game Name Build 12345 MULTi14 FitGirl Repack"
            new Regex(@"^(?<title>(?![(\[]).+?)(?:\s+Build\s+\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Public tracker format with repack/edition info: "ELDEN RING Shadow of the Erdtree Deluxe Edition..."
            // Match title including the edition part, stop at version/DLC/repacker info
            new Regex(@"^(?<title>(?![(\[]).+?(?:Deluxe|Ultimate|Standard|Premium|Gold|Collectors?|Limited|Complete|GOTY|Game[._\-\s]?of[._\-\s]?the[._\-\s]?Year|Definitive|Anniversary|Enhanced|Remastered|Digital\s+Deluxe|Directors?[._\-\s]?Cut)[._\-\s]*Edition)(?:\s+v[\d\s._]+|\s+\d+\s+DLCs?|\s+All\s+DLCs?|\s+MULTi\d+|\s+Build\s+\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // FitGirl/repacker format with space-separated version: "Title v1 0 1 Day 1 Patch ... FitGirl Repack"
            // Title stops at first "v" followed by digit
            new Regex(@"^(?<title>(?![(\[]).+?)(?=\s+v\d).*?FitGirl\s+(?:Monkey\s+)?Repack", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // FitGirl/repacker format with Bonus metadata: "Title Bonus DLCs ... MULTi## FitGirl Repack"
            // Title stops at "Bonus" when followed by DLCs/Content and repacker
            // Requires non-bracket content after "Bonus Content/DLCs" to avoid stripping when it's part of the title name
            new Regex(@"^(?<title>(?![(\[]).+?)(?=\s+Bonus\s+(?:DLCs?|Content)\s+[^\[\(]).*?(?:FitGirl|DODI)[-_. ]?Repack", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // ElAmigos/repacker format with year: "Title YEAR MULTi##-ElAmigos"
            // Strip realistic years (1980-2049) before MULTi but keep fictional years (2050+) like "Cyberpunk 2077"
            new Regex(@"^(?<title>(?![(\[]).+?)(?=\s+(?:198\d|199\d|20[0-4]\d)\s+MULTi|\s+MULTi\d+[-\s]+(?:ElAmigos|FitGirl|DODI)).*?MULTi\d+[-\s]+(?:ElAmigos|FitGirl|DODI)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // FitGirl/DODI repack format: "Game Name MULTi14 FitGirl Repack..." or "Game Name DODI Repack..."
            // Match title up to MULTi or repacker name
            new Regex(@"^(?<title>(?![(\[]).+?)(?:[-_. ]+(?:MULTi\d+|(?:FitGirl|DODI|XATAB|Elamigos|CorePack|KaOs)[-_. ]*Repack))", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Scene group followed by PORTABLE: "ELDEN RING NIGHTREIGN RUNE PORTABLE"
            new Regex(@"^(?<title>(?![(\[]).+?)\s+(?<releasegroup>CODEX|PLAZA|SKIDROW|CPY|EMPRESS|FLT|HOODLUM|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|RUNE|HI2U|TENOKE|DELiGHT)\s+PORTABLE$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Scene release with underscore platform suffix: "Kinsfolk_Linux-bADkARMA" → "Kinsfolk Linux" (keeps platform name)
            // Note: Underscore-separated platform is kept as part of the title (converts to space)
            new Regex(@"^(?<title>(?![(\[]).+?_(?:Linux|MacOS|Mac|Win(?:dows)?|x64|x86))-(?<releasegroup>PLAZA|CODEX|SKIDROW|CPY|EMPRESS|FLT|HOODLUM|RAZOR1911|RAZOR|RazorDOX|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|CHRONOS|SiMPLEX|RUNE|HI2U|TENOKE|DELiGHT|DINOByTES|bADkARMA|PLAYMAGiC|voices38|I_KnoW|GOG)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Scene release with hyphenated group: "ELDEN RING-PLAZA" or "Game.Name-CODEX"
            // Match title up to hyphen followed by known scene group
            new Regex(@"^(?<title>(?![(\[]).+?)-(?<releasegroup>PLAZA|CODEX|SKIDROW|CPY|EMPRESS|FLT|HOODLUM|RAZOR1911|RAZOR|RazorDOX|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|CHRONOS|SiMPLEX|RUNE|HI2U|TENOKE|DELiGHT|DINOByTES|bADkARMA|PLAYMAGiC|voices38|I_KnoW|GOG)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Game releases without year - match title up to known release group at END of string (must be before year patterns to keep years in game titles)
            // Scene groups: CODEX, PLAZA, SKIDROW, CPY, EMPRESS, RELOADED, etc.
            // Repackers: FitGirl, DODI, XATAB, Elamigos, etc.
            // Optional version (v20251216) and REPACK before group are stripped from title
            new Regex(@"^(?<title>(?![(\[]).+?)(?:[._]v\d+(?:[._]\d+)*)?(?:[._]REPACK)?[-_. ](?<releasegroup>CODEX|PLAZA|SKIDROW|CPY|EMPRESS|FLT|DOGE|HOODLUM|RAZOR1911|RAZOR|RazorDOX|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|CHRONOS|SiMPLEX|ALI213|3DM|STEAMPUNKS|FCKDRM|ANOMALY|RUNE|VREX|HI2U|TENOKE|I_KnoW|DELiGHT|DINOByTES|bADkARMA|PLAYMAGiC|voices38|FITGIRL|DODI|XATAB|ELAMIGOS|COREPACK|KAOS|MASQUERADE|GOG|STEAM[-_.]?RIP|EPIC[-_.]?RIP|P2P)(?:[-_. ]?REPACK)?(?:\.[a-z0-9]{2,4})?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Russian tracker format: [DL] Title [L] [langs] (year, genre) (date) [source]
            // Example: [DL] The Witness [L] [RUS + ENG + 13 / ENG] (2016, Adventure) (21-12-2017) [GOG]
            // Example: [CD] Half-Life 2 [P] [RUS + ENG / ENG] (2004, FPS) (1.0.1.0) [Tycoon]
            // Example: [DL] Hades II (2) [P] [RUS + ENG + 13 / ENG] (2025, TPS) - the (2) is a sequel indicator to strip
            new Regex(@"^\[(?:DL|UL|SP|CD|DVD|P|L)\]\s*(?<title>[^\[\]]+?)\s*(?:\(\d+\)\s*)?(?:\[[^\]]*\]\s*)*\((?<year>(1(8|9)|20)\d{2})", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Anime [Subgroup] and Year
            new Regex(@"^(?:\[(?<subgroup>.+?)\][-_. ]?)(?<title>(?![(\[]).+?)?(?:(?:[-_\W](?<![)\[!]))*(?<year>(1(8|9)|20)\d{2}(?!p|i|x|\d+|\]|\W\d+)))+.*?(?<hash>\[\w{8}\])?(?:$|\.)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Anime [Subgroup] no year, versioned title, hash
            new Regex(@"^(?:\[(?<subgroup>.+?)\][-_. ]?)(?<title>(?![(\[]).+?)((v)(?:\d{1,2})(?:([-_. ])))(\[.*)?(?:[\[(][^])])?.*?(?<hash>\[\w{8}\])(?:$|\.)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Anime [Subgroup] no year, info in double sets of brackets, hash
            new Regex(@"^(?:\[(?<subgroup>.+?)\][-_. ]?)(?<title>(?![(\[]).+?)(\[.*).*?(?<hash>\[\w{8}\])(?:$|\.)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Anime [Subgroup] no year, info in parentheses or brackets, hash
            new Regex(@"^(?:\[(?<subgroup>.+?)\][-_. ]?)(?<title>(?![(\[]).+)(?:[\[(][^])]).*?(?<hash>\[\w{8}\])(?:$|\.)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Some german or french tracker formats (missing year, ...) (Only applies to german and TrueFrench releases) - see ParserFixture for examples and tests - french removed as it broke all games w/ french titles
            new Regex(@"^(?<title>(?![(\[]).+?)((\W|_))(" + EditionRegex + @".{1,3})?(?:(?<!(19|20)\d{2}.*?)(?<!(?:Good|The)[_ .-])(German|TrueFrench))(.+?)(?=((19|20)\d{2}|$))(?<year>(19|20)\d{2}(?!p|i|\d+|\]|\W\d+))?(\W+|_|$)(?!\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Special, Despecialized, etc. Edition Games, e.g: Mission.Impossible.3.Special.Edition.2011
            new Regex(@"^(?<title>(?![(\[]).+?)?(?:(?:[-_\W](?<![)\[!]))*" + EditionRegex + @".{1,3}(?<year>(1(8|9)|20)\d{2}(?!p|i|\d+|\]|\W\d+)))+(\W+|_|$)(?!\\)",
                          RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Special, Despecialized, etc. Edition Games, e.g: Mission.Impossible.3.2011.Special.Edition //TODO: Seems to slow down parsing heavily!
            /*new Regex(@"^(?<title>(?![(\[]).+?)?(?:(?:[-_\W](?<![)\[!]))*(?<year>(19|20)\d{2}(?!p|i|(19|20)\d{2}|\]|\W(19|20)\d{2})))+(\W+|_|$)(?!\\)\(?(?<edition>(((Extended.|Ultimate.)?(Director.?s|Collector.?s|Theatrical|Ultimate|Final(?=(.(Cut|Edition|Version)))|Extended|Rogue|Special|Despecialized|\d{2,3}(th)?.Anniversary)(.(Cut|Edition|Version))?(.(Extended|Uncensored|Remastered|Unrated|Uncut|IMAX|Fan.?Edit))?|((Uncensored|Remastered|Unrated|Uncut|IMAX|Fan.?Edit|Edition|Restored|((2|3|4)in1))))))\)?",
                          RegexOptions.IgnoreCase | RegexOptions.Compiled),*/

            // Normal game format, e.g: Mission.Impossible.3.2011
            new Regex(@"^(?<title>(?![(\[]).+?)?(?:(?:[-_\W](?<![)\[!]))*(?<year>(1(8|9)|20)\d{2}(?!p|i|(1(8|9)|20)\d{2}|\]|\W(1(8|9)|20)\d{2})))+(\W+|_|$)(?!\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // PassThePopcorn Torrent names: Star.Wars[PassThePopcorn]
            new Regex(@"^(?<title>.+?)?(?:(?:[-_\W](?<![()\[!]))*(?<year>(\[\w *\])))+(\W+|_|$)(?!\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // That did not work? Maybe some tool uses [] for years. Who would do that?
            new Regex(@"^(?<title>(?![(\[]).+?)?(?:(?:[-_\W](?<![)!]))*(?<year>(1(8|9)|20)\d{2}(?!p|i|\d+|\W\d+)))+(\W+|_|$)(?!\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // As a last resort for games that have ( or [ in their title.
            new Regex(@"^(?<title>.+?)?(?:(?:[-_\W](?<![)\[!]))*(?<year>(1(8|9)|20)\d{2}(?!p|i|\d+|\]|\W\d+)))+(\W+|_|$)(?!\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Game release without version: GameName-GROUP (fallback for scene-style naming, group must be uppercase 4+ chars)
            new Regex(@"^(?<title>[A-Za-z0-9][A-Za-z0-9._-]*?[A-Za-z0-9])-(?<releasegroup>[A-Z]{4,})$", RegexOptions.Compiled),

            // Fallback: Simple game name without any markers (e.g., "Hytale", "My Winter Car", "StarRupture")
            // Only matches if no other regex matched - must look like a proper game title
            // Requirements:
            //   - Words start with uppercase followed by 1+ lowercase letters
            //   - Allows PascalCase (StarRupture), spaces (My Winter Car), and colon separators
            //   - Optional trailing number/Roman numeral for sequels
            //   - Rejects all-caps, all-lowercase, and random hash strings
            new Regex(@"^(?<title>[A-Z][a-z]+(?:[A-Z][a-z]+)*(?:(?::\s+|\s+)[A-Za-z][a-z]+(?:[A-Z][a-z]+)*)*(?:\s+(?:\d{1,4}|[IVXLCDM]+))?)$", RegexOptions.Compiled)
        };

        private static readonly Regex[] ReportGameTitleFolderRegex = new[]
        {
            // When year comes first.
            new Regex(@"^(?:(?:[-_\W](?<![)!]))*(?<year>(19|20)\d{2}(?!p|i|\d+|\W\d+)))+(\W+|_|$)(?<title>.+?)?$")
        };

        private static readonly Regex[] RejectHashedReleasesRegex = new Regex[]
            {
                // Generic match for md5 and mixed-case hashes.
                new Regex(@"^[0-9a-zA-Z]{32}", RegexOptions.Compiled),

                // Generic match for shorter lower-case hashes.
                new Regex(@"^[a-z0-9]{24}$", RegexOptions.Compiled),

                // Format seen on some NZBGeek releases
                // Be very strict with these coz they are very close to the valid 101 ep numbering.
                new Regex(@"^[A-Z]{11}\d{3}$", RegexOptions.Compiled),
                new Regex(@"^[a-z]{12}\d{3}$", RegexOptions.Compiled),

                // Backup filename (Unknown origins)
                new Regex(@"^Backup_\d{5,}S\d{2}-\d{2}$", RegexOptions.Compiled),

                // 123 - Started appearing December 2014
                new Regex(@"^123$", RegexOptions.Compiled),

                // abc - Started appearing January 2015
                new Regex(@"^abc$", RegexOptions.Compiled | RegexOptions.IgnoreCase),

                // abc - Started appearing 2020
                new Regex(@"^abc[-_. ]xyz", RegexOptions.Compiled | RegexOptions.IgnoreCase),

                // b00bs - Started appearing January 2015
                new Regex(@"^b00bs$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
            };

        // Regex to detect whether the title was reversed.
        private static readonly Regex ReversedTitleRegex = new Regex(@"(?:^|[-._ ])(p027|p0801)[-._ ]", RegexOptions.Compiled);

        // Regex to split game titles that contain `AKA`.
        private static readonly Regex AlternativeTitleRegex = new Regex(@"[ ]+(?:AKA|\/)[ ]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Regex to unbracket alternative titles.
        private static readonly Regex BracketedAlternativeTitleRegex = new Regex(@"(.*) \([ ]*AKA[ ]+(.*)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex NormalizeAlternativeTitleRegex = new Regex(@"[ ]+(?:A\.K\.A\.)[ ]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex NormalizeRegex = new Regex(@"((?:\b|_)(?<!^|[^a-zA-Z0-9_']\w[^a-zA-Z0-9_'])([aà](?!$|[^a-zA-Z0-9_']\w[^a-zA-Z0-9_'])|an|the|and|or|of)(?!$)(?:\b|_))|\W|_",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ReportIgdbId = new Regex(@"igdb(id)?-(?<igdbid>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly RegexReplace SimpleTitleRegex = new RegexReplace(@"(?:(480|540|576|720|1080|2160)[ip]|[xh][\W_]?26[45]|DD\W?5\W1|[<>?*]|848x480|1280x720|1920x1080|3840x2160|4096x2160|(8|10)b(it)?|10-bit)\s*?(?![a-b0-9])",
                                                                string.Empty,
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SimpleReleaseTitleRegex = new Regex(@"\s*(?:[<>?*|])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Valid TLDs http://data.iana.org/TLD/tlds-alpha-by-domain.txt

        private static readonly Regex CleanQualityBracketsRegex = new Regex(@"\[[a-z0-9 ._-]+\]$",
                                                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YearInTitleRegex = new Regex(@"^(?<title>.+?)(?:\W|_.)?[\(\[]?(?<year>\d{4})[\]\)]?",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SpecialCharRegex = new Regex(@"(\&|\:|\\|\/)+", RegexOptions.Compiled);
        private static readonly Regex PunctuationRegex = new Regex(@"[^\w\s]", RegexOptions.Compiled);
        private static readonly Regex ArticleWordRegex = new Regex(@"^(a|an|the)\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SpecialEpisodeWordRegex = new Regex(@"\b(part|special|edition|christmas)\b\s?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DuplicateSpacesRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

        private static readonly Regex RequestInfoRegex = new Regex(@"^(?:\[.+?\])+", RegexOptions.Compiled);

        private static readonly string[] Numbers = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

        private static readonly Regex MultiRegex = new (@"[_. ](?<multi>multi)[_. ]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static Dictionary<string, string> _umlautMappings = new Dictionary<string, string>
        {
            { "ö", "oe" },
            { "ä", "ae" },
            { "ü", "ue" },
        };

        public static ParsedGameInfo ParseGamePath(string path)
        {
            var fileInfo = new FileInfo(path);

            var result = ParseGameTitle(fileInfo.Name, true);

            if (result == null)
            {
                Logger.Debug("Attempting to parse game info using directory and file names. {0}", fileInfo.Directory.Name);
                result = ParseGameTitle(fileInfo.Directory.Name + " " + fileInfo.Name);
            }

            if (result == null)
            {
                Logger.Debug("Attempting to parse game info using directory name. {0}", fileInfo.Directory.Name);
                result = ParseGameTitle(fileInfo.Directory.Name + fileInfo.Extension);
            }

            return result;
        }

        public static ParsedGameInfo ParseGameTitle(string title, bool isDir = false)
        {
            var originalTitle = title;
            try
            {
                if (!ValidateBeforeParsing(title))
                {
                    return null;
                }

                Logger.Debug("Parsing string '{0}'", title);

                if (ReversedTitleRegex.IsMatch(title))
                {
                    var titleWithoutExtension = FileExtensions.RemoveFileExtension(title).ToCharArray();
                    Array.Reverse(titleWithoutExtension);

                    title = $"{titleWithoutExtension}{title.Substring(titleWithoutExtension.Length)}";

                    Logger.Debug("Reversed name detected. Converted to '{0}'", title);
                }

                var releaseTitle = FileExtensions.RemoveFileExtension(title);

                // Trim dashes from end
                releaseTitle = releaseTitle.Trim('-', '_');

                releaseTitle = releaseTitle.Replace("【", "[").Replace("】", "]");

                foreach (var replace in ParserCommon.PreSubstitutionRegex)
                {
                    if (replace.TryReplace(ref releaseTitle))
                    {
                        Logger.Trace($"Replace regex: {replace}");
                        Logger.Debug("Substituted with " + releaseTitle);
                    }
                }

                var simpleTitle = SimpleTitleRegex.Replace(releaseTitle);

                // TODO: Quick fix stripping [url] - prefixes.
                simpleTitle = ParserCommon.WebsitePrefixRegex.Replace(simpleTitle);
                simpleTitle = ParserCommon.WebsitePostfixRegex.Replace(simpleTitle);

                simpleTitle = ParserCommon.CleanTorrentSuffixRegex.Replace(simpleTitle);

                simpleTitle = CleanQualityBracketsRegex.Replace(simpleTitle, m =>
                {
                    // Preserve FitGirl/DODI brackets as they're needed for title parsing
                    if (Regex.IsMatch(m.Value, @"\b(FitGirl|DODI)\b", RegexOptions.IgnoreCase))
                    {
                        return m.Value;
                    }

                    if (QualityParser.ParseQualityName(m.Value).Quality != Qualities.Quality.Unknown)
                    {
                        return string.Empty;
                    }

                    return m.Value;
                });

                var allRegexes = ReportGameTitleRegex.ToList();

                if (isDir)
                {
                    allRegexes.AddRange(ReportGameTitleFolderRegex);
                }

                foreach (var regex in allRegexes)
                {
                    var match = regex.Matches(simpleTitle);

                    if (match.Count != 0)
                    {
                        Logger.Trace(regex);
                        try
                        {
                            var result = ParseGameMatchCollection(match);

                            if (result != null)
                            {
                                var simpleReleaseTitle = SimpleReleaseTitleRegex.Replace(releaseTitle, string.Empty);

                                var simpleTitleReplaceString = match[0].Groups["title"].Success ? match[0].Groups["title"].Value : result.PrimaryGameTitle;

                                if (simpleTitleReplaceString.IsNotNullOrWhiteSpace())
                                {
                                    if (match[0].Groups["title"].Success)
                                    {
                                        simpleReleaseTitle = simpleReleaseTitle.Remove(match[0].Groups["title"].Index, match[0].Groups["title"].Length)
                                                                               .Insert(match[0].Groups["title"].Index, simpleTitleReplaceString.Contains('.') ? "A.Game" : "A Game");
                                    }
                                    else
                                    {
                                        simpleReleaseTitle = simpleReleaseTitle.Replace(simpleTitleReplaceString, simpleTitleReplaceString.Contains('.') ? "A.Game" : "A Game");
                                    }
                                }

                                result.ReleaseGroup = ReleaseGroupParser.ParseReleaseGroup(simpleReleaseTitle);

                                var subGroup = GetSubGroup(match);
                                if (!subGroup.IsNullOrWhiteSpace())
                                {
                                    result.ReleaseGroup = subGroup;
                                }

                                // Check for release group from game-style release regex
                                if (match[0].Groups["releasegroup"].Success && !match[0].Groups["releasegroup"].Value.IsNullOrWhiteSpace())
                                {
                                    result.ReleaseGroup = match[0].Groups["releasegroup"].Value;
                                }

                                result.HardcodedSubs = ParseHardcodeSubs(title);

                                Logger.Debug("Release Group parsed: {0}", result.ReleaseGroup);

                                // Use simpleReleaseTitle (with title replaced by "A Game") to avoid false positives from language words in titles (e.g. "The Italian Job")
                                // But also check original releaseTitle for specific language markers unlikely to be in titles (HebDubbed, UKR)
                                var langTitle = result.ReleaseGroup.IsNotNullOrWhiteSpace() ? simpleReleaseTitle.Replace(result.ReleaseGroup, "RlsGrp") : simpleReleaseTitle;
                                result.Languages = LanguageParser.ParseLanguages(langTitle);

                                // Check for specific language markers in the full release title that might have been stripped
                                // Hebrew (HebDubbed) and Ukrainian (UKR) use specific markers unlikely to be in game titles
                                var fullTitle = result.ReleaseGroup.IsNotNullOrWhiteSpace() ? releaseTitle.Replace(result.ReleaseGroup, "RlsGrp") : releaseTitle;
                                var additionalLangs = LanguageParser.ParseLanguages(fullTitle);

                                // Add Hebrew/Ukrainian from the full title if not already detected
                                var specificLangs = additionalLangs.Where(l =>
                                    l == Languages.Language.Hebrew ||
                                    l == Languages.Language.Ukrainian).ToList();
                                if (specificLangs.Any())
                                {
                                    // If current result is just Unknown, replace with specific langs
                                    // Otherwise, add any missing specific langs to the result
                                    if (result.Languages.Count == 1 && result.Languages[0] == Languages.Language.Unknown)
                                    {
                                        result.Languages = specificLangs;
                                    }
                                    else
                                    {
                                        foreach (var lang in specificLangs)
                                        {
                                            if (!result.Languages.Contains(lang))
                                            {
                                                result.Languages.Add(lang);
                                            }
                                        }
                                    }
                                }

                                Logger.Debug("Languages parsed: {0}", string.Join(", ", result.Languages));

                                result.Quality = QualityParser.ParseQuality(title);
                                Logger.Debug("Quality parsed: {0}", result.Quality);

                                result.GameVersion = QualityParser.ParseGameVersion(title);
                                if (result.GameVersion.HasValue)
                                {
                                    Logger.Debug("Game version parsed: {0}", result.GameVersion);
                                }

                                result.ContentType = QualityParser.ParseContentType(title);
                                if (result.ContentType != Model.ReleaseContentType.Unknown)
                                {
                                    Logger.Debug("Content type parsed: {0}", result.ContentType);
                                }

                                result.Platform = PlatformParser.ParsePlatform(title);
                                result.PlatformString = PlatformParser.ParsePlatformString(title);
                                if (result.Platform != Games.PlatformFamily.Unknown)
                                {
                                    Logger.Debug("Platform parsed: {0} ({1})", result.Platform, result.PlatformString);
                                }

                                if (result.Edition.IsNullOrWhiteSpace())
                                {
                                    result.Edition = ParseEdition(simpleReleaseTitle);
                                    Logger.Debug("Edition parsed: {0}", result.Edition);
                                }

                                result.ReleaseHash = GetReleaseHash(match);
                                if (!result.ReleaseHash.IsNullOrWhiteSpace())
                                {
                                    Logger.Debug("Release Hash parsed: {0}", result.ReleaseHash);
                                }

                                result.OriginalTitle = originalTitle;
                                result.ReleaseTitle = releaseTitle;
                                result.SimpleReleaseTitle = simpleReleaseTitle;

                                result.IgdbId = ParseIgdbId(simpleReleaseTitle);

                                return result;
                            }
                        }
                        catch (InvalidDateException ex)
                        {
                            Logger.Debug(ex, ex.Message);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!title.ToLower().Contains("password") && !title.ToLower().Contains("yenc"))
                {
                    Logger.Error(e, "An error has occurred while trying to parse {0}", title);
                }
            }

            Logger.Debug("Unable to parse {0}", title);
            return null;
        }

        public static int ParseIgdbId(string title)
        {
            var match = ReportIgdbId.Match(title);
            if (match.Success)
            {
                if (match.Groups["igdbid"].Value != null)
                {
                    return int.TryParse(match.Groups["igdbid"].Value, out var igdbId) ? igdbId : 0;
                }
            }

            return 0;
        }

        public static string ParseEdition(string languageTitle)
        {
            // Use EditionRegex directly for better performance - it will find the edition anywhere in the string
            // without the O(n²) backtracking caused by the old ^.+? prefix pattern
            var editionMatch = EditionRegex.Match(languageTitle);

            if (editionMatch.Success && editionMatch.Groups["edition"].Value != null &&
                editionMatch.Groups["edition"].Value.IsNotNullOrWhiteSpace())
            {
                return editionMatch.Groups["edition"].Value.Replace(".", " ");
            }

            return "";
        }

        public static string ReplaceGermanUmlauts(string s)
        {
            var t = s;
            t = t.Replace("ä", "ae");
            t = t.Replace("ö", "oe");
            t = t.Replace("ü", "ue");
            t = t.Replace("Ä", "Ae");
            t = t.Replace("Ö", "Oe");
            t = t.Replace("Ü", "Ue");
            t = t.Replace("ß", "ss");
            return t;
        }

        public static string ToUrlSlug(string value, bool invalidDashReplacement = false, string trimEndChars = "-_", string deduplicateChars = "-_")
        {
            // First to lower case
            value = value.ToLowerInvariant();

            // Remove all accents
            value = value.RemoveAccent();

            // Replace spaces
            value = Regex.Replace(value, @"\s", "-", RegexOptions.Compiled);

            // Should invalid characters be replaced with dash or empty string?
            var replaceCharacter = invalidDashReplacement ? "-" : string.Empty;

            // Remove invalid chars
            value = Regex.Replace(value, @"[^a-z0-9\s-_]", replaceCharacter, RegexOptions.Compiled);

            // Trim dashes or underscores from end, or user defined character set
            if (!string.IsNullOrEmpty(trimEndChars))
            {
                value = value.Trim(trimEndChars.ToCharArray());
            }

            // Replace double occurrences of - or _, or user defined character set
            if (!string.IsNullOrEmpty(deduplicateChars))
            {
                value = Regex.Replace(value, @"([" + deduplicateChars + "]){2,}", "$1", RegexOptions.Compiled);
            }

            return value;
        }

        public static string CleanGameTitle(this string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return title;
            }

            // If Title only contains numbers return it as is.
            if (long.TryParse(title, out _))
            {
                return title;
            }

            return ReplaceGermanUmlauts(NormalizeRegex.Replace(title, string.Empty).ToLowerInvariant()).RemoveAccent();
        }

        public static string NormalizeGameTitle(string title)
        {
            title = SpecialEpisodeWordRegex.Replace(title, string.Empty);
            title = PunctuationRegex.Replace(title, " ");
            title = DuplicateSpacesRegex.Replace(title, " ");

            return title.Trim().ToLower();
        }

        public static string NormalizeTitle(string title)
        {
            if (title == null)
            {
                return string.Empty;
            }

            title = PunctuationRegex.Replace(title, string.Empty);
            title = ArticleWordRegex.Replace(title, string.Empty);
            title = DuplicateSpacesRegex.Replace(title, " ");
            title = SpecialCharRegex.Replace(title, string.Empty);

            return title.Trim().ToLower();
        }

        public static string SimplifyReleaseTitle(this string title)
        {
            return SimpleReleaseTitleRegex.Replace(title, string.Empty);
        }

        public static string ParseHardcodeSubs(string title)
        {
            var subMatch = HardcodedSubsRegex.Matches(title).OfType<Match>().LastOrDefault();

            if (subMatch != null && subMatch.Success)
            {
                if (subMatch.Groups["hcsub"].Success)
                {
                    return subMatch.Groups["hcsub"].Value;
                }
                else if (subMatch.Groups["hc"].Success)
                {
                    return "Generic Hardcoded Subs";
                }
            }

            return null;
        }

        public static bool HasMultipleLanguages(string title)
        {
            return MultiRegex.IsMatch(title);
        }

        private static ParsedGameInfo ParseGameMatchCollection(MatchCollection matchCollection)
        {
            if (!matchCollection[0].Groups["title"].Success || matchCollection[0].Groups["title"].Value == "(")
            {
                return null;
            }

            var gameName = matchCollection[0].Groups["title"].Value.Replace('_', ' ');
            gameName = NormalizeAlternativeTitleRegex.Replace(gameName, " AKA ");
            gameName = RequestInfoRegex.Replace(gameName, "").Trim(' ');

            var parts = gameName.Split('.');
            gameName = "";
            var n = 0;
            var previousAcronym = false;
            var nextPart = "";
            foreach (var part in parts)
            {
                if (parts.Length >= n + 2)
                {
                    nextPart = parts[n + 1];
                }
                else
                {
                    nextPart = "";
                }

                // Treat single non-numeric, non-article characters as abbreviations (preserve period)
                // when continuing an acronym sequence, or when not at end of title and not before a number
                // Examples: "Dragon.Ball.Z.Kakarot" → "Dragon Ball Z. Kakarot", "R.I.P.D" → "R.I.P.D."
                if (part.Length == 1 && part.ToLower() != "a" && !int.TryParse(part, out _) &&
                    (previousAcronym || (n < parts.Length - 1 && !int.TryParse(nextPart, out _))))
                {
                    gameName += part + ".";
                    previousAcronym = true;
                }
                else if (part.ToLower() == "a" && (previousAcronym || nextPart.Length == 1))
                {
                    gameName += part + ".";
                    previousAcronym = true;
                }
                else if (part.ToLower() == "dr")
                {
                    gameName += part + ".";
                    previousAcronym = true;
                }
                else
                {
                    if (previousAcronym)
                    {
                        gameName += " ";
                        previousAcronym = false;
                    }

                    gameName += part + " ";
                }

                n++;
            }

            gameName = gameName.Trim(' ');

            int.TryParse(matchCollection[0].Groups["year"].Value, out var airYear);

            ParsedGameInfo result;

            result = new ParsedGameInfo { Year = airYear };

            if (matchCollection[0].Groups["edition"].Success)
            {
                result.Edition = matchCollection[0].Groups["edition"].Value.Replace(".", " ");
            }

            var gameTitles = new List<string>();
            gameTitles.Add(gameName);

            // Delete parentheses of the form (aka ...).
            var unbracketedName = BracketedAlternativeTitleRegex.Replace(gameName, "$1 AKA $2");

            // Split by AKA and filter out empty and duplicate names.
            gameTitles
                .AddRange(AlternativeTitleRegex
                        .Split(unbracketedName)
                        .Where(alternativeName => alternativeName.IsNotNullOrWhiteSpace() && alternativeName != gameName));

            result.GameTitles = gameTitles;

            Logger.Debug("Game Parsed. {0}", result);

            return result;
        }

        private static bool ValidateBeforeParsing(string title)
        {
            if (title.ToLower().Contains("password") && title.ToLower().Contains("yenc"))
            {
                Logger.Debug("");
                return false;
            }

            if (!title.Any(char.IsLetterOrDigit))
            {
                return false;
            }

            var titleWithoutExtension = FileExtensions.RemoveFileExtension(title);

            if (RejectHashedReleasesRegex.Any(v => v.IsMatch(titleWithoutExtension)))
            {
                Logger.Debug("Rejected Hashed Release Title: " + title);
                return false;
            }

            return true;
        }

        private static string GetSubGroup(MatchCollection matchCollection)
        {
            var subGroup = matchCollection[0].Groups["subgroup"];

            if (subGroup.Success)
            {
                return subGroup.Value;
            }

            return string.Empty;
        }

        private static string GetReleaseHash(MatchCollection matchCollection)
        {
            var hash = matchCollection[0].Groups["hash"];

            if (hash.Success)
            {
                var hashValue = hash.Value.Trim('[', ']');

                if (hashValue.Equals("1280x720"))
                {
                    return string.Empty;
                }

                return hashValue;
            }

            return string.Empty;
        }
    }
}
