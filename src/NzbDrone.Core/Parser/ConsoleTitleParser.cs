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
        private const string ConsoleToken = @"\b(?:NSP|NSZ|XCI|NSW|N?Switch|WiiU|Wii|3DS|CIA|WUX|WUD)\b";

        // The region/platform tags a console release carries between the game
        // name and the group: "Game.Name.EUR.NSW-GRP", "Game Name JPN NSW-GRP",
        // "Game.Name.USA.PROPER-GRP" (#209). Used by RemoveRegionMetadata below
        // as anchors only, not as a vocabulary of everything a scene name may
        // hold - MetadataTailToken is what absorbs the rest.
        //
        // Matched case-sensitively (the (?-i:...) group turns IgnoreCase back
        // off for this part alone): these are scene tags and always upper case,
        // whereas "Asia", "Pal" and "Chn" can be, or can start, an ordinary
        // title word. That is also why only 3-letter-and-longer codes are
        // listed and "US"/"EU"/"JP" are not.
        //
        // NSW is the dangerous one - in real release names it is overwhelmingly
        // "New South Wales" - so it is NOT trusted as a bare word anywhere. It
        // is safe here only because of the anchoring: the token has to sit in
        // the metadata TAIL, i.e. everything between it and the end of the name
        // must itself be metadata. "Highway Patrol NSW Australia Simulator"
        // does not qualify; "Game.Name.NSW-VENOM" does. This is the same
        // discriminator QualityParser's trailing demo tier already uses for the
        // bare words DEMO/TRIAL/PREVIEW.
        private const string RegionPlatformToken =
            @"(?-i:EUR|USA|JPN|JAP|ASIA|KOR|CHN|TWN|HKG|LATAM|PAL|NTSC(?:[._-]?[UJC])?|NSW|NSP|NSZ|XCI|WUX|WUD|CIA)";

        // The scene tags that are conventionally written in MIXED case, so the
        // all-caps rule below cannot see them: "iNTERNAL", "MULTi5", "MULTi",
        // "READNFO", "eShop". Spelled out as a closed whitelist and matched
        // case-insensitively (the enclosing regexes all set IgnoreCase), rather
        // than loosening the all-caps rule to "any casing" - that rule is the
        // only thing stopping an ordinary lower-case title word from counting
        // as metadata.
        //
        // Before this list, a mixed-case tag ended the metadata run, the
        // anchored tail regex failed at the first region token and matched only
        // a later one, and the earlier region tag stayed glued to the title:
        // "<Title> DLC Pack 2 EUR MULTi5 NSW-GRP" stripped only from "NSW" and
        // parsed as a game called "<Title> DLC Pack 2 EUR MULTi5" (#212).
        // The all-caps spellings (INTERNAL, READNFO, PROPER, REPACK, DLC,
        // UPDATE) already qualified via the all-caps rule and are listed only
        // where a mixed-case form exists.
        private const string MixedCaseSceneTag =
            @"(?:iNT|INTERNAL|MULTi(?:LANGUAGE)?[._-]?\d*|MULTI[._-]?\d*|READNFO|NFOFIX|DIRFIX|SAMPLEFIX|RERIP|PROPER|REPACK|eShop)";

        // One token of release metadata: an all-caps scene/console token
        // (upper case only, so a lower-case title word does not qualify), a
        // known mixed-case scene tag, a store/container name, or a version.
        // Shared with QualityParser's trailing demo tier, which needs the same
        // "everything after this point is metadata" test (#207).
        internal const string MetadataTailToken =
            @"(?:(?-i:[A-Z][A-Z0-9]*)|" + MixedCaseSceneTag + @"|nsp|nsz|xci|wux|wud|cia|rar|zip|7z|v?\d[\w.]*)";

        // Leading platform prefix: "[Switch NSP] Game Name", "[NSW] Game Name".
        // A prefix moves the start of the title, which no existing regex allows
        // for - they all pin the title to the start of the string.
        // Same nesting rule as TrailingGroupRegex below: the bracket form stops
        // only at "]" and the paren form only at ")", so "[Switch (NSP)]" is one
        // prefix rather than a group that ends early and leaves a stray "]".
        private static readonly Regex LeadingConsolePrefixRegex = new Regex(
            @"^\s*(?:\[[^\]]*" + ConsoleToken + @"[^\]]*\]|\([^)]*" + ConsoleToken + @"[^)]*\))\s*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // One trailing bracketed or parenthesised group: "[NSP]", "(Base Game)",
        // "[01004D300C5AE000]", "[v0]", "[1.1.0]", "[US]", "[sakura]".
        // Content is deliberately not enumerated: at this point the name has
        // already failed to parse, so an unrecognised trailing group is a tag.
        // NOTE: each alternative excludes only its OWN delimiters, so a bracket
        // group may carry parens inside it and vice versa: "[RUS (Mod.)/ENG]" is
        // one tag, not three tokens. The previous single form excluded all four
        // characters from the content, which meant one nested "(" made the group
        // unmatchable - nothing was stripped, the retry gave up, and the release
        // parsed to nothing at all.
        private static readonly Regex TrailingGroupRegex = new Regex(
            @"(?:\[[^\[\]]*\]|\([^()]*\))\s*$",
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

        // A BARE leading console-platform prefix: "Nintendo Switch Hollow Knight
        // NSZ RUS Multi9". Unlike the bracketed prefix above this is NOT safe to
        // remove - "Nintendo Switch Sports" and "Nintendo Switch Sports Club"
        // are real games, and "Switch" is an ordinary word - so it is used only
        // as a VETO on the region strip, never as a replacement. See the note on
        // RemoveRegionMetadata.
        private static readonly Regex LeadingBarePlatformPrefixRegex = new Regex(
            @"^(?:Nintendo[\s._-]+)?(?:N?Switch|WiiU|Wii[\s._-]?U|3DS)[\s._-]+\S",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // The anchor tokens that are ALSO the terminator of the title regex
        // array's "Nintendo Switch <Title> NSP RUS Multi10" shape, and so are
        // the only ones whose removal can break it. Region tags proper (EUR,
        // NSW, JPN) are not terminators there, which is why the veto below is
        // restricted to these three: "Nintendo Switch Sports EUR NSW-GRP"
        // anchors on EUR, keeps its strip, and still parses.
        private static readonly Regex DumpTokenAnchorRegex = new Regex(
            @"^(?:NSP|NSZ|XCI)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly char[] EdgeTrim = { ' ', '.', '_', '-' };

        // The anchor token, then metadata all the way to the end of the name.
        // The optional trailing "-Group" is spelled separately because a mixed
        // case group name ("LiGHTFORCE") is not an all-caps tail token.
        private static readonly Regex TrailingRegionMetadataRegex = new Regex(
            @"(?<=[\s._-])(?<region>" + RegionPlatformToken + @")\b(?:[\s._-]+" + MetadataTailToken + @")*(?:-[A-Za-z0-9_]{2,})?[\s._-]*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // The "-GROUP" suffix of a scene name, searched from the anchor onwards.
        private static readonly Regex TrailingGroupNameRegex = new Regex(
            @"-(?<group>[A-Za-z0-9]{2,})\s*$",
            RegexOptions.Compiled);

        /// <summary>
        /// Drop a trailing region/platform tag and the metadata after it, so the
        /// game name underneath is what the title regexes see.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Normalize"/> this is NOT a post-failure retry: a
        /// name like "Game.Name.EUR.NSW-GRP" parses perfectly well today, just
        /// with "EUR NSW" still glued onto the game title, so nothing ever fails
        /// and no retry fires (#209). The caller therefore tries the stripped
        /// name FIRST and falls back to the untouched one when the stripped name
        /// parses to nothing, which is what bounds the risk of stripping early.
        /// Quality, version, content type, platform and release group are all
        /// read off the full release name, so this only ever feeds the title
        /// regexes.
        /// </remarks>
        /// <returns>The cleaned name, or null when there is no such tag.</returns>
        public static string RemoveRegionMetadata(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var match = TrailingRegionMetadataRegex.Match(title);

            if (!match.Success)
            {
                return null;
            }

            var anchorIndex = match.Groups["region"].Index;
            var cleaned = title.Substring(0, anchorIndex).Trim(EdgeTrim);

            if (cleaned.IsNullOrWhiteSpace() || !HasLetterRegex.IsMatch(cleaned) || cleaned == title.Trim(EdgeTrim))
            {
                return null;
            }

            // Leave a name that leads with a bare console-platform prefix alone.
            // The title regex array has a dedicated shape for these ("Nintendo
            // Switch <Title> NSP RUS Multi10") and it is TERMINATED by the very
            // NSP/NSZ/XCI token this method anchors on, so stripping the tail
            // first takes the terminator away: the shape stops matching, the
            // bare-title fallback picks the name up instead, and the platform
            // prefix ends up inside the parsed title. Since the untouched name
            // parses correctly and the stripped one parses WRONGLY - rather than
            // not at all - the caller's "fall back when the stripped name parses
            // to nothing" rule cannot catch it, so the veto has to be here.
            //
            // The prefix is not removed instead, because it cannot be: "Nintendo
            // Switch Sports" is a real game and dropping its first two words
            // would leave "Sports".
            if (DumpTokenAnchorRegex.IsMatch(match.Groups["region"].Value) &&
                LeadingBarePlatformPrefixRegex.IsMatch(cleaned))
            {
                return null;
            }

            // A dot-separated name needs its group back: "Super.Mario.Odyssey"
            // with no trailing group matches none of the title regexes, so
            // dropping it turns the whole parse into null. A space-separated
            // name is the other way round - "Super Mario Odyssey" is read by the
            // bare-title fallback, and "Super Mario Odyssey-VENOM" is not - so
            // there the group stays off.
            if (cleaned.Contains(' '))
            {
                return cleaned;
            }

            var groupMatch = TrailingGroupNameRegex.Match(title, anchorIndex);

            return groupMatch.Success ? cleaned + "-" + groupMatch.Groups["group"].Value : cleaned;
        }

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
