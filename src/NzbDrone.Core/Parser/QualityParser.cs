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

        // DLC-only releases (requires base game)
        private static readonly Regex DlcOnlyRegex = new (@"\b(?<dlconly>DLC[\s._-]?ONLY|(?:^|[\s._-])DLC(?:[\s._-]|$)|ADDON[\s._-]?ONLY|EXPANSION[\s._-]?ONLY)\b",
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
        private static readonly Regex GameVersionRegex = new (@"[._\-\s]v?(?<version>\d+(?:\.\d+){1,3})[._\-\s]|[._\-\s]v(?<spaceversion>\d+(?:\s+\d+){1,3})(?:\s|$)|[._\-\s](?:BUILD|B)[._\-]?(?<build>\d+)|[._\-\s](?:Update|Patch)[._\-\s]?(?<update>\d+(?:\.\d+)*)",
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

            // Try version group first (v1.0.1, 1.2.3)
            if (match.Groups["version"].Success)
            {
                var versionString = match.Groups["version"].Value;
                if (GameVersion.TryParse(versionString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from '{1}'", version, name);
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
                    Logger.Trace("Parsed game version '{0}' from space-separated '{1}'", version, name);
                    return version;
                }
            }

            // Try build number
            if (match.Groups["build"].Success)
            {
                var buildString = "Build " + match.Groups["build"].Value;
                if (GameVersion.TryParse(buildString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from '{1}'", version, name);
                    return version;
                }
            }

            // Try update number
            if (match.Groups["update"].Success)
            {
                var updateString = match.Groups["update"].Value;
                if (!string.IsNullOrEmpty(updateString) && GameVersion.TryParse(updateString, out var version))
                {
                    Logger.Trace("Parsed game version '{0}' from '{1}'", version, name);
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
                Logger.Trace("Detected update-only release: {0}", name);
                return ReleaseContentType.UpdateOnly;
            }

            // Check for DLC-only releases
            if (DlcOnlyRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected DLC-only release: {0}", name);
                return ReleaseContentType.DlcOnly;
            }

            // Check for season pass / DLC bundle
            if (SeasonPassRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected season pass/DLC bundle: {0}", name);
                return ReleaseContentType.SeasonPass;
            }

            // Check for expansion packs
            if (ExpansionRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected expansion pack: {0}", name);
                return ReleaseContentType.Expansion;
            }

            // Check for complete/GOTY editions (base game + all DLC)
            if (CompleteEditionRegex.IsMatch(normalizedName))
            {
                Logger.Trace("Detected complete edition with all DLC: {0}", name);
                return ReleaseContentType.BaseGameWithAllDlc;
            }

            // Default to unknown - could be base game only or undetected
            return ReleaseContentType.Unknown;
        }

        public static QualityModel ParseQuality(string name)
        {
            Logger.Debug("Trying to parse quality for '{0}'", name);

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

            // Check for scene releases (with or without crack) - must be before repack/update checks
            // Scene group takes precedence over update/language pack indicators
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

            // Check for update/patch only releases (after scene group check)
            // Must contain "Update" or "Patch" as standalone word (not as part of game title)
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

            // Check for repack releases
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

            // Check for portable releases
            if (PortableRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Portable;
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
