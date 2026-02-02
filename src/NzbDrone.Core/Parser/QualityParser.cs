using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser
{
    public class QualityParser
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(QualityParser));

        // Scene groups - most common game scene release groups
        private static readonly Regex SceneGroupRegex = new (@"\b(?<scene>CODEX|PLAZA|SKIDROW|CPY|EMPRESS|FLT|DOGE|HOODLUM|RAZOR1911|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|CHRONOS|SiMPLEX|ALI213|3DM|STEAMPUNKS|FCKDRM|ANOMALY|RUNE)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Cracked indicators
        private static readonly Regex CrackedRegex = new (@"\b(?<cracked>CRACKED|CRACK[\s._-]?ONLY|CRACK[\s._-]?FIX|NO[\s._-]?DRM)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // GOG releases
        private static readonly Regex GOGRegex = new (@"\b(?<gog>GOG(?:[._-]?RIP)?)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Steam releases
        private static readonly Regex SteamRegex = new (@"\b(?<steam>STEAM[\s._-]?RIP|STEAM[\s._-]?UNLOCKED)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Epic Games releases
        private static readonly Regex EpicRegex = new (@"\b(?<epic>EPIC[\s._-]?(?:GAMES)?[\s._-]?RIP)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Origin/EA releases
        private static readonly Regex OriginRegex = new (@"\b(?<origin>ORIGIN[\s._-]?RIP|EA[\s._-]?(?:APP)?[\s._-]?RIP)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Uplay/Ubisoft Connect releases
        private static readonly Regex UplayRegex = new (@"\b(?<uplay>UPLAY[\s._-]?RIP|UBISOFT[\s._-]?CONNECT[\s._-]?RIP)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Repack releases (FitGirl, DODI, XATAB, etc.)
        private static readonly Regex RepackRegex = new (@"\b(?<repack>REPACK|FITGIRL|DODI|XATAB|ELAMIGOS|COREPACK|MR[\s._-]?DJ|KAOS|DARCK[\s._-]?REPACKS?|MASQUERADE|CHOVKA|DECEPTICON|WANTERLUDE|R[\s.]?G[\s._-]?(?:МЕХАНИКИ|МЕХАНІКИ|MECHANICS|CATALYST|FREEDOM|STEAMGAMES))\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ISO/Retail releases
        private static readonly Regex ISORegex = new (@"\b(?<iso>ISO|DISC[\s._-]?IMAGE)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RetailRegex = new (@"\b(?<retail>RETAIL|DVD[\s._-]?RIP|BD[\s._-]?RIP)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Portable releases
        private static readonly Regex PortableRegex = new (@"\b(?<portable>PORTABLE|NO[\s._-]?INSTALL)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Special modifiers
        private static readonly Regex PreloadRegex = new (@"\b(?<preload>PRELOAD|PRE[\s._-]?RELEASE)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Matches releases that are updates/patches only (not full games)
        // Examples: "Game Update v1.2", "Game Language Pack", but NOT "Game v1.2 FitGirl"
        private static readonly Regex UpdateOnlyRegex = new (@"\b(?<update>UPDATE(?:[\s._-]?ONLY)?|PATCH(?:[\s._-]?ONLY)?|LANGUAGE[\s._-]?PACK|HOTFIX)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DLCRegex = new (@"\b(?<dlc>(?:INCL(?:UDES?)?[\s._-]?)?(?:ALL[\s._-]?)?DLC[sS]?|DLC[\s._-]?(?:PACK|UNLOCKER)|COMPLETE[\s._-]?(?:EDITION|PACK)|ULTIMATE[\s._-]?EDITION|GOTY|GAME[\s._-]?OF[\s._-]?THE[\s._-]?YEAR)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // DLC-only releases (requires base game) - standalone "DLC" but NOT "All DLC", "DLC Bundle", "Includes DLC"
        private static readonly Regex DlcOnlyRegex = new (@"\b(?<dlconly>DLC[\s._-]?ONLY|ADDON[\s._-]?ONLY|EXPANSION[\s._-]?ONLY)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Standalone DLC (not part of "All DLC", "DLC Bundle", etc.)
        private static readonly Regex StandaloneDlcRegex = new (@"(?<!ALL[\s._-])(?<!INCLUDES[\s._-])(?<!WITH[\s._-])[\s._-]DLC(?:[\s._-]|$)(?!BUNDLE|PACK|UNLOCKER|S[\s._-])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Season pass / DLC bundle
        private static readonly Regex SeasonPassRegex = new (@"\b(?<seasonpass>SEASON[\s._-]?PASS|DLC[\s._-]?BUNDLE|EXPANSION[\s._-]?PASS|CONTENT[\s._-]?PACK)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Expansion packs
        private static readonly Regex ExpansionRegex = new (@"\b(?<expansion>EXPANSION(?:[\s._-]?PACK)?|STANDALONE[\s._-]?EXPANSION)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Complete/GOTY editions (base game + all DLC)
        private static readonly Regex CompleteEditionRegex = new (@"\b(?<complete>COMPLETE[\s._-]?(?:EDITION|PACK)|DEFINITIVE[\s._-]?EDITION|ULTIMATE[\s._-]?EDITION|GOTY|GAME[\s._-]?OF[\s._-]?THE[\s._-]?YEAR|GOLD[\s._-]?EDITION|LEGENDARY[\s._-]?EDITION|PREMIUM[\s._-]?EDITION|(?:INCL(?:UDES?)?|WITH)[\s._-]?ALL[\s._-]?DLC[sS]?|ALL[\s._-]?DLC[sS]?[\s._-]?(?:INCL(?:UDED)?|PACK))\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Update/Patch only (requires base game)
        private static readonly Regex UpdatePatchOnlyRegex = new (@"\b(?<updateonly>UPDATE[\s._-]?ONLY|PATCH[\s._-]?ONLY|(?:UPDATE|PATCH)[\s._-]?\d+[\s._-]?ONLY|HOTFIX[\s._-]?ONLY)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MultiLangRegex = new (@"\b(?<multilang>MULTI[._-]?\d+|MULTi(?:LANGUAGE)?)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Proper/Repack revision patterns
        private static readonly Regex ProperRegex = new (@"\b(?<proper>PROPER)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex VersionRegex = new (@"[._-]v?(?<version>\d+(?:\.\d+)+)[._-]|[._-](?:BUILD|B)[._-]?(?<version>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RealRegex = new (@"\b(?<real>REAL|PROPER[._-]?FIX)\b",
            RegexOptions.Compiled);

        // More comprehensive version regex for game releases
        // Handles both dot-separated (v1.2.3) and space-separated (v1 2 3) versions
        // Space-separated limited to 4 components (major minor patch build)
        // Update/Patch requires a version number (no trailing ? to avoid matching "Update" alone)
        // Patterns:
        // 1. v prefix with dotted version (v1.0.1, v.1.0.1, v 1.0.1, v1.0u4) - most common
        // 2. v prefix with date version (v20250317) - single number, 4+ digits
        // 3. Version with non-space delimiters and at least 3 parts (Game-1.2.3-SKIDROW)
        // 4. Space-separated after v (v1 0 1)
        // 5. Space-delimited version without v prefix (Game 1.12 MULTi9, Game? 1.0.11)
        // 6. Parenthesized version without v prefix ((1.0.11))
        // 7. Space-separated version without v prefix (Game 1 0 11 Release)
        // 8. Standalone build number before MULTi (jc141 format: Game 1131346 MULTi15)
        // 9. Build number (Build 12345, B12345)
        // 10. Update/Patch version
        // Negative lookahead excludes file sizes (52.9 GB)
        private static readonly Regex GameVersionRegex = new (
            @"[._\-\s\[\(]v[\.\s]?(?<version>\d+(?:\.\d+){1,3}[a-z]?\d*)(?![._]?\d*\s*[GMKT]i?B)(?=[._\-\s\]\)<,]|$)|" +
            @"[._\-\s\[\(]v(?<dateversion>\d{4,})(?=[._\-\s\]\)<,]|$)|" +
            @"[._\-](?<version2>\d+(?:\.\d+){2,3})(?![._]?\d*\s*[GMKT]i?B)[._\-]|" +
            @"[._\-\s\[\(]v(?<spaceversion>\d+(?:\s+\d+){1,3})(?=[\s\-._\]\),]|$)|" +
            @"[\s\?\!](?<version3>\d+(?:\.\d+)+)(?![._]?\d*\s*[GMKT]i?B)(?=\s|$)|" +
            @"\((?<parenversion>\d+(?:\.\d+)+)\)|" +
            @"[\s\?\!](?<spacenov>\d+(?:\s+\d+){2,3})(?=\s|$)|" +
            @"\s(?<buildonly>\d{6,})(?=\s+MULTi)|" +
            @"[._\-\s\[\(](?:BUILD|B)[._\-\s]?(?<build>\d+)|" +
            @"[._\-\s\[\(](?:Update|Patch)[._\-\s\]\)]?(?<update>\d+(?:\.\d+)*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parse game version from release title
        /// </summary>
        public static GameVersion ParseGameVersion(string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return new GameVersion();
            }

            var match = GameVersionRegex.Match(name);
            if (!match.Success)
            {
                return new GameVersion();
            }

            // Try version group first (v1.0.1, v.1.0.1, v1.0u4)
            if (match.Groups["version"].Success)
            {
                var versionString = match.Groups["version"].Value;

                // Strip alphanumeric suffix like "u4" from "1.0u4"
                versionString = Regex.Replace(versionString, @"[a-z]\d*$", "", RegexOptions.IgnoreCase);
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try date version group (v20250317 - single number, 6+ digits)
            if (match.Groups["dateversion"].Success)
            {
                var versionString = match.Groups["dateversion"].Value;
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game date version '{0}' from '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try version2 group (1.2.3 without v prefix, requires non-space delimiters)
            if (match.Groups["version2"].Success)
            {
                var versionString = match.Groups["version2"].Value;
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from non-prefixed '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try version3 group (1.1.0.17370 without v prefix, space-delimited, exactly 4 parts)
            if (match.Groups["version3"].Success)
            {
                var versionString = match.Groups["version3"].Value;
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from space-delimited '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try space-separated version (v1 2 3 -> 1.2.3)
            // Convert space-separated to dot-separated for parsing
            if (match.Groups["spaceversion"].Success)
            {
                var versionString = match.Groups["spaceversion"].Value.Trim();
                versionString = Regex.Replace(versionString, @"\s+", ".");
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from space-separated '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try parenthesized version without v prefix (e.g., "(1.0.11)")
            if (match.Groups["parenversion"].Success)
            {
                var versionString = match.Groups["parenversion"].Value;
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from parenthesized '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try space-separated version without v prefix (e.g., "Game 1 0 11 Release")
            if (match.Groups["spacenov"].Success)
            {
                var versionString = match.Groups["spacenov"].Value.Trim();
                versionString = Regex.Replace(versionString, @"\s+", ".");
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from space-separated no-v '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try buildonly group (standalone build number before MULTi, jc141 format)
            if (match.Groups["buildonly"].Success)
            {
                var buildString = "Build " + match.Groups["buildonly"].Value;
                if (GameVersion.TryParse(buildString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from standalone build '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try build number
            if (match.Groups["build"].Success)
            {
                var buildString = "Build " + match.Groups["build"].Value;
                if (GameVersion.TryParse(buildString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            // Try update number
            if (match.Groups["update"].Success)
            {
                var updateString = match.Groups["update"].Value;
                if (!string.IsNullOrEmpty(updateString) && GameVersion.TryParse(updateString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from '{1}'", version, CleanseLogMessage.SanitizeLogParam(name));
                    return version;
                }
            }

            return new GameVersion();
        }

        /// <summary>
        /// Parse the content type from release title (DLC-only, Complete Edition, etc.)
        /// </summary>
        public static ReleaseContentType ParseContentType(string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return ReleaseContentType.Unknown;
            }

            var normalizedName = name.Replace('_', ' ').Replace('.', ' ').Trim();

            // Check for update/patch only first (most specific)
            if (UpdatePatchOnlyRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected update-only release: {0}", CleanseLogMessage.SanitizeLogParam(name));
                return ReleaseContentType.UpdateOnly;
            }

            // Check for complete/GOTY editions (base game + all DLC) - before DLC-only
            if (CompleteEditionRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected complete edition with all DLC: {0}", CleanseLogMessage.SanitizeLogParam(name));
                return ReleaseContentType.BaseGameWithAllDlc;
            }

            // Check for season pass / DLC bundle - before DLC-only
            if (SeasonPassRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected season pass/DLC bundle: {0}", CleanseLogMessage.SanitizeLogParam(name));
                return ReleaseContentType.SeasonPass;
            }

            // Check for expansion packs
            if (ExpansionRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected expansion pack: {0}", CleanseLogMessage.SanitizeLogParam(name));
                return ReleaseContentType.Expansion;
            }

            // Check for DLC-only releases (explicit "DLC Only", "Addon Only", etc.)
            if (DlcOnlyRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected DLC-only release: {0}", CleanseLogMessage.SanitizeLogParam(name));
                return ReleaseContentType.DlcOnly;
            }

            // Check for standalone DLC (just "DLC" but not part of "All DLC", "DLC Bundle", etc.)
            if (StandaloneDlcRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected standalone DLC release: {0}", CleanseLogMessage.SanitizeLogParam(name));
                return ReleaseContentType.DlcOnly;
            }

            // Default to unknown - could be base game only or undetected
            return ReleaseContentType.Unknown;
        }

        public static QualityModel ParseQuality(string name)
        {
            Logger.Debug("Trying to parse quality for '{0}'", CleanseLogMessage.SanitizeLogParam(name));

            if (name.IsNullOrWhiteSpace())
            {
                return new QualityModel { Quality = Quality.Unknown };
            }

            name = name.Trim();
            var result = ParseQualityName(name);

            return result;
        }

        public static QualityModel ParseQualityName(string name)
        {
            var normalizedName = name.Replace('_', ' ').Replace('.', ' ').Trim();
            var result = ParseQualityModifiers(name, normalizedName);

            // Check for preload (incomplete/pre-release)
            if (PreloadRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Preload;
                return result;
            }

            // Check for GOG releases (DRM-free) - high priority
            if (GOGRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.GOG;
                return result;
            }

            // Check for update/patch only releases BEFORE scene groups
            // Update releases should be marked as UpdateOnly even if they have a scene group
            if (UpdateOnlyRegex.IsMatch(normalizedName))
            {
                // Check for indicators that this is an update-only release
                var isUpdateRelease = normalizedName.ContainsIgnoreCase("update only") ||
                    normalizedName.ContainsIgnoreCase("patch only") ||
                    normalizedName.ContainsIgnoreCase("hotfix") ||
                    Regex.IsMatch(normalizedName, @"\bupdate\s+v[\d\s._]+", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(normalizedName, @"\bpatch\s+v[\d\s._]+", RegexOptions.IgnoreCase);

                if (isUpdateRelease)
                {
                    result.SourceDetectionSource = QualityDetectionSource.Name;
                    result.Quality = Quality.UpdateOnly;
                    return result;
                }
            }

            // Check for repack releases BEFORE scene groups
            // (releases can have both scene group and repack, e.g., "EMPRESS DODI-Repack")
            if (RepackRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;

                // Check if it includes all DLC
                if (DLCRegex.IsMatch(normalizedName))
                {
                    result.Quality = Quality.RepackAllDLC;
                }
                else
                {
                    result.Quality = Quality.Repack;
                }

                return result;
            }

            // Check for portable releases BEFORE scene groups
            if (PortableRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Portable;
                return result;
            }

            // Check for scene releases (with or without crack)
            if (SceneGroupRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;

                if (CrackedRegex.IsMatch(normalizedName))
                {
                    result.Quality = Quality.SceneCracked;
                }
                else
                {
                    result.Quality = Quality.Scene;
                }

                return result;
            }

            // Check for Steam rips
            if (SteamRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Steam;
                return result;
            }

            // Check for Epic rips
            if (EpicRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Epic;
                return result;
            }

            // Check for Origin rips
            if (OriginRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Origin;
                return result;
            }

            // Check for Uplay rips
            if (UplayRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Uplay;
                return result;
            }

            // Check for ISO/disc image releases
            if (ISORegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.ISO;
                return result;
            }

            // Check for retail releases
            if (RetailRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Retail;
                return result;
            }

            // Check for cracked releases without scene group
            if (CrackedRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.SceneCracked;
                return result;
            }

            // Check for multi-language indicator
            if (MultiLangRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.MultiLang;
                return result;
            }

            // Default to scene if nothing else matches but has common game release patterns
            if (Regex.IsMatch(normalizedName, @"[-_.](?:x86|x64|win(?:32|64)?|pc)[-_.]", RegexOptions.IgnoreCase))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Scene;
                return result;
            }

            return result;
        }

        private static QualityModel ParseQualityModifiers(string name, string normalizedName)
        {
            var result = new QualityModel { Quality = Quality.Unknown };

            var versionRegexResult = VersionRegex.Match(normalizedName);

            if (versionRegexResult.Success)
            {
                // For games, version usually indicates a specific build, not a revision
                result.RevisionDetectionSource = QualityDetectionSource.Name;
            }

            if (ProperRegex.IsMatch(normalizedName))
            {
                result.Revision.Version = 2;
                result.RevisionDetectionSource = QualityDetectionSource.Name;
            }

            var realRegexResult = RealRegex.Matches(name);

            if (realRegexResult.Count > 0)
            {
                result.Revision.Real = realRegexResult.Count;
                result.RevisionDetectionSource = QualityDetectionSource.Name;
            }

            return result;
        }
    }
}
