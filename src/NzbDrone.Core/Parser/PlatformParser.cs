using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Parser
{
    public static class PlatformParser
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(PlatformParser));

        // PlayStation platforms
        private static readonly Regex PlayStationRegex = new Regex(
            @"\b(?:PS5|PlayStation\s*5)\b|\[(?:PS5|PlayStation\s*5)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PlayStation4Regex = new Regex(
            @"\b(?:PS4|PlayStation\s*4)\b|\[(?:PS4|PlayStation\s*4)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PlayStation3Regex = new Regex(
            @"\b(?:PS3|Ps3|PlayStation\s*3)\b|\[(?:PS3|Ps3|PlayStation\s*3)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PSVitaRegex = new Regex(
            @"\b(?:PSV(?:ita)?|PS\s*Vita)\b|\[(?:PSV(?:ita)?|PS\s*Vita)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PSPRegex = new Regex(
            @"\b(?:PSP)\b|\[(?:PSP)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Xbox platforms
        private static readonly Regex XboxSeriesRegex = new Regex(
            @"\b(?:Xbox\s*Series\s*[XS]|XSX|XSS)\b|\[(?:Xbox\s*Series\s*[XS]|XSX|XSS)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex XboxOneRegex = new Regex(
            @"\b(?:Xbox\s*One|XB1|XBONE)\b|\[(?:Xbox\s*One|XB1|XBONE)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Xbox360Regex = new Regex(
            @"\b(?:Xbox\s*360|X360|XB360)\b|\[(?:Xbox\s*360|X360|XB360)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex XboxRegex = new Regex(
            @"\b(?:Xbox|XB)\b|\[(?:Xbox|XB)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Nintendo platforms
        private static readonly Regex SwitchRegex = new Regex(
            @"\b(?:Switch|NSW|NSwitch|Nintendo\s*Switch)\b|\[(?:Switch|NSW|NSwitch|Nintendo\s*Switch)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex WiiURegex = new Regex(
            @"\b(?:Wii\s*U|WiiU)\b|\[(?:Wii\s*U|WiiU)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex WiiRegex = new Regex(
            @"\b(?:Wii)\b|\[(?:Wii)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Nintendo3DSRegex = new Regex(
            @"\b(?:3DS|N3DS|Nintendo\s*3DS)\b|\[(?:3DS|N3DS|Nintendo\s*3DS)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex NintendoDSRegex = new Regex(
            @"\b(?:NDS|Nintendo\s*DS)\b|\[(?:NDS|Nintendo\s*DS)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // PC/Mac/Linux platforms
        private static readonly Regex MacRegex = new Regex(
            @"\b(?:MAC|macOS|OSX|Mac\s*OS)\b|\[(?:MAC|macOS|OSX|Mac\s*OS)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex LinuxRegex = new Regex(
            @"\b(?:Linux|LNX)\b|\[(?:Linux|LNX)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parse platform from release title
        /// </summary>
        /// <param name="title">Release title to parse</param>
        /// <returns>Detected platform family, or Unknown if not detected</returns>
        public static PlatformFamily ParsePlatform(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return PlatformFamily.Unknown;
            }

            // Check PlayStation platforms (most specific first)
            if (PlayStationRegex.IsMatch(title))
            {
                Logger.Trace("Detected PS5 platform in title");
                return PlatformFamily.PlayStation;
            }

            if (PlayStation4Regex.IsMatch(title))
            {
                Logger.Trace("Detected PS4 platform in title");
                return PlatformFamily.PlayStation;
            }

            if (PlayStation3Regex.IsMatch(title))
            {
                Logger.Trace("Detected PS3 platform in title");
                return PlatformFamily.PlayStation;
            }

            if (PSVitaRegex.IsMatch(title))
            {
                Logger.Trace("Detected PS Vita platform in title");
                return PlatformFamily.PlayStation;
            }

            if (PSPRegex.IsMatch(title))
            {
                Logger.Trace("Detected PSP platform in title");
                return PlatformFamily.PlayStation;
            }

            // Check Xbox platforms (most specific first)
            if (XboxSeriesRegex.IsMatch(title))
            {
                Logger.Trace("Detected Xbox Series X/S platform in title");
                return PlatformFamily.Xbox;
            }

            if (XboxOneRegex.IsMatch(title))
            {
                Logger.Trace("Detected Xbox One platform in title");
                return PlatformFamily.Xbox;
            }

            if (Xbox360Regex.IsMatch(title))
            {
                Logger.Trace("Detected Xbox 360 platform in title");
                return PlatformFamily.Xbox;
            }

            if (XboxRegex.IsMatch(title))
            {
                Logger.Trace("Detected Xbox platform in title");
                return PlatformFamily.Xbox;
            }

            // Check Nintendo platforms (most specific first)
            if (SwitchRegex.IsMatch(title))
            {
                Logger.Trace("Detected Nintendo Switch platform in title");
                return PlatformFamily.Nintendo;
            }

            if (WiiURegex.IsMatch(title))
            {
                Logger.Trace("Detected Wii U platform in title");
                return PlatformFamily.Nintendo;
            }

            if (WiiRegex.IsMatch(title))
            {
                Logger.Trace("Detected Wii platform in title");
                return PlatformFamily.Nintendo;
            }

            if (Nintendo3DSRegex.IsMatch(title))
            {
                Logger.Trace("Detected Nintendo 3DS platform in title");
                return PlatformFamily.Nintendo;
            }

            if (NintendoDSRegex.IsMatch(title))
            {
                Logger.Trace("Detected Nintendo DS platform in title");
                return PlatformFamily.Nintendo;
            }

            // Check Mac/Linux
            if (MacRegex.IsMatch(title))
            {
                Logger.Trace("Detected Mac platform in title");
                return PlatformFamily.Mac;
            }

            if (LinuxRegex.IsMatch(title))
            {
                Logger.Trace("Detected Linux platform in title");
                return PlatformFamily.Linux;
            }

            // Default to PC (most game releases are for PC)
            return PlatformFamily.Unknown;
        }

        /// <summary>
        /// Parse specific platform string from release title (for display)
        /// </summary>
        /// <param name="title">Release title to parse</param>
        /// <returns>Platform string like "PS3", "Xbox 360", "Switch", etc.</returns>
        public static string ParsePlatformString(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            // PlayStation
            if (PlayStationRegex.IsMatch(title))
            {
                return "PS5";
            }

            if (PlayStation4Regex.IsMatch(title))
            {
                return "PS4";
            }

            if (PlayStation3Regex.IsMatch(title))
            {
                return "PS3";
            }

            if (PSVitaRegex.IsMatch(title))
            {
                return "PS Vita";
            }

            if (PSPRegex.IsMatch(title))
            {
                return "PSP";
            }

            // Xbox
            if (XboxSeriesRegex.IsMatch(title))
            {
                return "Xbox Series X";
            }

            if (XboxOneRegex.IsMatch(title))
            {
                return "Xbox One";
            }

            if (Xbox360Regex.IsMatch(title))
            {
                return "Xbox 360";
            }

            if (XboxRegex.IsMatch(title))
            {
                return "Xbox";
            }

            // Nintendo
            if (SwitchRegex.IsMatch(title))
            {
                return "Switch";
            }

            if (WiiURegex.IsMatch(title))
            {
                return "Wii U";
            }

            if (WiiRegex.IsMatch(title))
            {
                return "Wii";
            }

            if (Nintendo3DSRegex.IsMatch(title))
            {
                return "3DS";
            }

            if (NintendoDSRegex.IsMatch(title))
            {
                return "NDS";
            }

            // Mac/Linux
            if (MacRegex.IsMatch(title))
            {
                return "Mac";
            }

            if (LinuxRegex.IsMatch(title))
            {
                return "Linux";
            }

            return null;
        }
    }
}
