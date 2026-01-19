using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser
{
    public class QualityParser
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(QualityParser));

        // Scene groups - most common game scene release groups
        private static readonly Regex SceneGroupRegex = new (@"\b(?<scene>CODEX|PLAZA|SKIDROW|CPY|EMPRESS|FLT|DOGE|HOODLUM|RAZOR1911|RELOADED|PROPHET|DARKSiDERS|TiNYiSO|CHRONOS|SiMPLEX|ALI213|3DM|STEAMPUNKS|FCKDRM|ANOMALY)\b",
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
        private static readonly Regex RepackRegex = new (@"\b(?<repack>REPACK|FITGIRL|DODI|XATAB|ELAMIGOS|COREPACK|MR[\s._-]?DJ|KAOS|DARCK[\s._-]?REPACKS?|MASQUERADE|R[\s.]?G[\s._-]?(?:MECHANICS|CATALYST|FREEDOM|STEAMGAMES))\b",
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

        private static readonly Regex UpdateOnlyRegex = new (@"\b(?<update>UPDATE[\s._-]?(?:ONLY)?|PATCH[\s._-]?(?:ONLY)?|V\d+[._]\d+(?:[._]\d+)*[\s._-]?(?:UPDATE|PATCH)?)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DLCRegex = new (@"\b(?<dlc>(?:INCL(?:UDES?)?[\s._-]?)?(?:ALL[\s._-]?)?DLC[sS]?|DLC[\s._-]?(?:PACK|UNLOCKER)|COMPLETE[\s._-]?(?:EDITION|PACK)|ULTIMATE[\s._-]?EDITION|GOTY|GAME[\s._-]?OF[\s._-]?THE[\s._-]?YEAR)\b",
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

            // Check for update/patch only
            if (UpdateOnlyRegex.IsMatch(normalizedName) && !DLCRegex.IsMatch(normalizedName))
            {
                // Make sure it's update-only and not a full game with version
                if (normalizedName.ContainsIgnoreCase("update only") ||
                    normalizedName.ContainsIgnoreCase("patch only") ||
                    Regex.IsMatch(normalizedName, @"\bupdate\b.*\bv\d+", RegexOptions.IgnoreCase))
                {
                    result.SourceDetectionSource = QualityDetectionSource.Name;
                    result.Quality = Quality.UpdateOnly;
                    return result;
                }
            }

            // Check for GOG releases (DRM-free)
            if (GOGRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.GOG;
                return result;
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
