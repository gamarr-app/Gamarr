using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Parser
{
    /// <summary>
    /// Strips console dump decorations off a release name so the ordinary title
    /// regexes can see the game name underneath.
    /// </summary>
    /// <remarks>
    /// The title regex array in <see cref="Parser"/> is a whitelist of known
    /// release shapes, and its terminal fallback is fully anchored and permits
    /// only letters, digits, spaces, colons, commas, hyphens and apostrophes.
    /// So a name carrying any token the whitelist does not already recognise
    /// - "[NSP]", "(Portable)", "[v0]", "NSW VENOM" - matches nothing at all and
    /// the whole parse returns null, before any platform or quality logic runs.
    ///
    /// Console scene names are almost entirely made of such tokens, so rather
    /// than enumerate them as new title shapes this normalises them away and
    /// lets the existing regexes do the work. It is only ever used as a retry
    /// after the regex array has already failed, so by construction it cannot
    /// change the result for any name that parses today.
    /// </remarks>
    public static class ConsoleTitleParser
    {
        // Leading platform prefix: "[Switch NSP] Game Name", "[NSW] Game Name".
        // A prefix moves the start of the title, which no existing regex allows
        // for - they all pin the title to the start of the string.
        private static readonly Regex LeadingConsolePrefixRegex = new Regex(
            @"^\s*[\[(][^\])]*\b(?:NSP|NSZ|XCI|NSW|N?Switch|WiiU|Wii|3DS|CIA|WUX|WUD)\b[^\])]*[\])]\s*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // One trailing bracketed or parenthesised group: "[NSP]", "(Base Game)",
        // "[01004D300C5AE000]", "[v0]", "[1.1.0]", "[US]", "[sakura]".
        // Content is deliberately not enumerated: at this point the name has
        // already failed to parse, so an unrecognised trailing group is a tag.
        private static readonly Regex TrailingGroupRegex = new Regex(
            @"[\[(][^\[\]()]*[\])]\s*$",
            RegexOptions.Compiled);

        // A trailing bare container or dump token. Prowlarr/TorrentDownload
        // normalise the extension separator to whitespace, so a real grab reads
        // "... [v0] nsp" rather than "....nsp".
        private static readonly Regex TrailingContainerTokenRegex = new Regex(
            @"[\s._-]+(?:nsp|nsz|xci|wux|wud|cia|rar|zip|7z|part\d+)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // A bare Switch dump token mid-name means everything from there on is
        // release metadata: "Kirby and the Forgotten Land NSW VENOM".
        // "Switch" itself is excluded - it is a real word in real game names
        // ("Nintendo Switch Sports"), and titles carrying it already parse.
        private static readonly Regex BareConsoleTokenRegex = new Regex(
            @"[\s._-]+(?:NSW|NSP|NSZ|XCI)\b.*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HasLetterRegex = new Regex(@"[A-Za-z]", RegexOptions.Compiled);

        private static readonly char[] EdgeTrim = { ' ', '.', '_', '-' };

        /// <summary>
        /// Remove console dump decorations from a release name.
        /// </summary>
        /// <param name="title">The release name that failed to parse.</param>
        /// <returns>
        /// The cleaned name, or null if nothing was removed or if removing
        /// everything would leave nothing usable behind. Returning null when
        /// unchanged is what bounds the retry in <see cref="Parser"/>.
        /// </returns>
        public static string Normalize(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var original = title.Trim();
            var current = LeadingConsolePrefixRegex.Replace(original, string.Empty);

            string previous;

            do
            {
                previous = current;
                current = Reduce(current);
            }
            while (current != previous);

            current = current.Trim(EdgeTrim);

            if (current.IsNullOrWhiteSpace() || !HasLetterRegex.IsMatch(current) || current == original)
            {
                return null;
            }

            return current;
        }

        private static string Reduce(string value)
        {
            var current = value.TrimEnd(EdgeTrim);

            current = Shorten(current, TrailingContainerTokenRegex.Replace(current, string.Empty));
            current = Shorten(current, TrailingGroupRegex.Replace(current, string.Empty));
            current = Shorten(current, BareConsoleTokenRegex.Replace(current, string.Empty));

            return current;
        }

        // Only accept a reduction that leaves a usable name behind, so a release
        // that is nothing but tags does not collapse to an empty string.
        private static string Shorten(string current, string candidate)
        {
            var trimmed = candidate.TrimEnd(EdgeTrim);

            if (trimmed.Length == 0 || !HasLetterRegex.IsMatch(trimmed))
            {
                return current;
            }

            return trimmed;
        }
    }
}
