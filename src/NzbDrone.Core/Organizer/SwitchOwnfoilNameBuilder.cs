using System;
using System.Text;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    /// <summary>
    /// Builds the Nintendo Switch dump file name that ownfoil-style libraries use:
    /// <c>Title [TitleId][vVersion][Semver][Base|UPD][Region]</c>.
    /// </summary>
    /// <remarks>
    /// Confirmed against a real library: "Super Mario Galaxy™ [010099C022B96000][v0][Base].nsp"
    /// alongside "Super Mario Galaxy™ [010099C022B96800][v327680][1.3.1][UPD].nsp", and
    /// "Game Boy™ - Nintendo Switch Online [0100C62011050800][v1441792][4.0.0][UPD][US].nsp".
    /// The semver and region groups are optional; the type tag is part of the convention.
    ///
    /// Note that ownfoil itself identifies a title by the cnmt metadata inside the NSP,
    /// not by the file name, so "it still shows up in ownfoil" proves nothing about the
    /// name. The only check that means anything is a string comparison against names a
    /// dumper actually wrote, which is what the fixture does.
    ///
    /// Like the No-Intro profiles this ignores the user's format string entirely and
    /// emits a fixed convention. It returns null - falling the caller back to normal
    /// Gamarr naming - for anything without a title id, because a title id is the one
    /// field of the layout that cannot be derived or defaulted.
    /// </remarks>
    public static class SwitchOwnfoilNameBuilder
    {
        // Any bracketed or parenthesised group, so the tags trailing the title id can
        // be read back off a name a dumper already wrote.
        private static readonly Regex TagGroupRegex = new Regex(@"[\[(](?<value>[^\[\]()]*)[\])]",
                                                                RegexOptions.Compiled);

        // "[v0]", "[v327680]" - the nsp's own version integer, always a bare number.
        private static readonly Regex VersionTagRegex = new Regex(@"^v(?<version>\d+)$",
                                                                  RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // "[1.3.1]", "[4.0.0]" - the display version, always dotted. A bare number
        // here would be the version integer above, not a semver.
        private static readonly Regex SemanticVersionTagRegex = new Regex(@"^\d+(?:\.\d+)+$",
                                                                          RegexOptions.Compiled);

        // "[US]", "[EU]", "[JP]". Not enumerated: at this point the group is neither a
        // version nor a type tag, and a bare pair of capitals in a dump name is a region.
        private static readonly Regex RegionTagRegex = new Regex(@"^[A-Z]{2}$",
                                                                 RegexOptions.Compiled);

        // Only a dump container is stripped as an extension. Path.GetFileNameWithoutExtension
        // cannot be used here: the semver group puts dots inside the name, so it would
        // cut "[v65536][1.1.0] nsp" down to "[v65536][1.1".
        private static readonly Regex ContainerExtensionRegex = new Regex(@"\.(?:nsp|nsz|xci|xcz|zip|7z|rar)$",
                                                                          RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly char[] TitleEdgeTrim = { ' ', '.', '_', '-' };

        public static string BuildFileName(string sourceName, string gameTitle, GameVersion parsedVersion = null)
        {
            if (sourceName.IsNullOrWhiteSpace())
            {
                return null;
            }

            sourceName = ContainerExtensionRegex.Replace(sourceName.Trim(), string.Empty);

            // The 16-hex title id is recognised by the parser's regex rather than a
            // second copy of it here, so the two cannot drift apart.
            var idMatch = Parser.Parser.SwitchTitleIdRegex.Match(sourceName);

            if (!idMatch.Success)
            {
                return null;
            }

            var titleIdGroup = idMatch.Groups["titleid"];
            var titleId = titleIdGroup.Value.ToUpperInvariant();

            var title = sourceName.Substring(0, idMatch.Index).Trim(TitleEdgeTrim);

            if (title.IsNullOrWhiteSpace())
            {
                title = gameTitle?.Trim(TitleEdgeTrim);
            }

            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            string version = null;
            string semanticVersion = null;
            string type = null;
            string region = null;

            // Only groups after the id are its tags; anything before it is title text.
            foreach (Match tag in TagGroupRegex.Matches(sourceName))
            {
                if (tag.Index < titleIdGroup.Index + titleIdGroup.Length)
                {
                    continue;
                }

                var value = tag.Groups["value"].Value.Trim();

                if (value.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var versionMatch = VersionTagRegex.Match(value);

                if (versionMatch.Success)
                {
                    version ??= versionMatch.Groups["version"].Value;
                    continue;
                }

                if (SemanticVersionTagRegex.IsMatch(value))
                {
                    semanticVersion ??= value;
                    continue;
                }

                var contentType = NormalizeContentType(value);

                if (contentType != null)
                {
                    type ??= contentType;
                    continue;
                }

                if (RegionTagRegex.IsMatch(value))
                {
                    region ??= value;
                }
            }

            // A dump with no version group is a base dump, which is v0 - that is what
            // the version integer of an un-updated title is, not a missing value.
            version ??= "0";

            // No semver in the name: the parsed release version can supply it, but only
            // when it is dotted. A bare integer there is the nsp version ("v327680") or
            // a build number, and neither is a display version any dumper writes.
            if (semanticVersion == null && parsedVersion != null && (parsedVersion.Minor > 0 || parsedVersion.Patch > 0))
            {
                semanticVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Patch}";
            }

            type ??= DeriveContentType(titleId);

            var builder = new StringBuilder();

            builder.Append(title);
            builder.Append(" [").Append(titleId).Append(']');
            builder.Append("[v").Append(version).Append(']');

            if (semanticVersion.IsNotNullOrWhiteSpace())
            {
                builder.Append('[').Append(semanticVersion).Append(']');
            }

            if (type.IsNotNullOrWhiteSpace())
            {
                builder.Append('[').Append(type).Append(']');
            }

            if (region.IsNotNullOrWhiteSpace())
            {
                builder.Append('[').Append(region).Append(']');
            }

            return builder.ToString();
        }

        private static string NormalizeContentType(string value)
        {
            if (value.Equals("Base", StringComparison.OrdinalIgnoreCase))
            {
                return "Base";
            }

            if (value.Equals("UPD", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Update", StringComparison.OrdinalIgnoreCase))
            {
                return "UPD";
            }

            if (value.Equals("DLC", StringComparison.OrdinalIgnoreCase))
            {
                return "DLC";
            }

            return null;
        }

        // A Switch title id carries its own content type in its last three nibbles:
        // a base title ends in 000, its update is that id + 0x800, and anything else
        // is DLC. The real library writes no type tag at all on DLC, so neither do we.
        private static string DeriveContentType(string titleId)
        {
            if (titleId.EndsWith("000", StringComparison.Ordinal))
            {
                return "Base";
            }

            if (titleId.EndsWith("800", StringComparison.Ordinal))
            {
                return "UPD";
            }

            return null;
        }
    }
}
