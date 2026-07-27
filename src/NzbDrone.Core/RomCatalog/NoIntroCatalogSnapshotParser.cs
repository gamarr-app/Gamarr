using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogSnapshotParser
    {
        NoIntroCatalogSnapshot Parse(string content);
    }

    public class NoIntroCatalogSnapshotParser : INoIntroCatalogSnapshotParser
    {
        private static readonly Regex ClrMameHeaderRegex = new Regex(@"clrmamepro\s*\((?<body>.*?)\)\s*game\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex ClrMameGameRegex = new Regex(@"game\s*\((?<body>.*?)(?=\n\s*game\s*\(|\z)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex ClrMameRomRegex = new Regex(@"rom\s*\((?<body>[^\r\n]*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ClrMameFieldRegex = new Regex("(?<name>[a-z0-9_]+)\\s+(?:(?:\"(?<quoted>[^\"]*)\")|(?<bare>[^\\s\\)]+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NumberedNameRegex = new Regex(@"^(?<number>(?:xB|x|z)\d{3,4}|\d{4})\s+-\s+(?<name>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public NoIntroCatalogSnapshot Parse(string content)
        {
            if (!content.TrimStart().StartsWith("<", StringComparison.Ordinal))
            {
                return ParseClrMamePro(content);
            }

            var document = XDocument.Parse(content);
            var root = document.Element("datafile") ?? throw new InvalidOperationException("Unsupported No-Intro catalog format");
            var header = root.Element("header");

            var systemName = header?.Element("name")?.Value ?? "unknown";
            var version = header?.Element("version")?.Value;

            var snapshot = new NoIntroCatalogSnapshot
            {
                CatalogVersion = version,
                SystemKey = NormalizeSystemKey(systemName)
            };

            foreach (var game in root.Elements("game"))
            {
                var canonicalName = game.Attribute("name")?.Value;

                if (string.IsNullOrWhiteSpace(canonicalName))
                {
                    continue;
                }

                var entry = CreateEntry(canonicalName,
                                        game.Attribute("cloneof")?.Value,
                                        game.Elements("rom").FirstOrDefault()?.Attribute("name")?.Value ?? canonicalName,
                                        game.Attribute("id")?.Value);

                foreach (var rom in game.Elements("rom"))
                {
                    AddHash(entry, "crc32", rom.Attribute("crc")?.Value, true, IsBadDump(rom));
                    AddHash(entry, "md5", rom.Attribute("md5")?.Value, false, IsBadDump(rom));
                    AddHash(entry, "sha1", rom.Attribute("sha1")?.Value, false, IsBadDump(rom));
                }

                snapshot.Entries.Add(entry);
            }

            return snapshot;
        }

        private static NoIntroCatalogSnapshot ParseClrMamePro(string content)
        {
            var header = ClrMameHeaderRegex.Match(content);
            var headerFields = header.Success ? ParseFields(header.Groups["body"].Value) : new System.Collections.Generic.Dictionary<string, string>();
            var systemName = headerFields.TryGetValue("name", out var name) ? name : "unknown";
            var version = headerFields.TryGetValue("version", out var headerVersion) ? headerVersion : null;

            var snapshot = new NoIntroCatalogSnapshot
            {
                CatalogVersion = version,
                SystemKey = NormalizeSystemKey(systemName)
            };

            foreach (Match gameMatch in ClrMameGameRegex.Matches(content))
            {
                var body = gameMatch.Groups["body"].Value;
                var romMatch = ClrMameRomRegex.Match(body);
                var gameFieldsBody = romMatch.Success ? body.Substring(0, romMatch.Index) : body;
                var fields = ParseFields(gameFieldsBody);

                if (!fields.TryGetValue("name", out var canonicalName) || string.IsNullOrWhiteSpace(canonicalName))
                {
                    continue;
                }

                var romFields = romMatch.Success ? ParseFields(romMatch.Groups["body"].Value) : new System.Collections.Generic.Dictionary<string, string>();

                var entry = CreateEntry(canonicalName,
                                        fields.TryGetValue("cloneof", out var parent) ? parent : null,
                                        romFields.TryGetValue("name", out var fileName) ? fileName : canonicalName,
                                        fields.TryGetValue("id", out var id) ? id : null);

                AddHash(entry, "crc32", romFields.TryGetValue("crc", out var crc) ? crc : null, true, false);
                AddHash(entry, "md5", romFields.TryGetValue("md5", out var md5) ? md5 : null, false, false);
                AddHash(entry, "sha1", romFields.TryGetValue("sha1", out var sha1) ? sha1 : null, false, false);

                snapshot.Entries.Add(entry);
            }

            return snapshot;
        }

        private static System.Collections.Generic.Dictionary<string, string> ParseFields(string body)
        {
            var fields = new System.Collections.Generic.Dictionary<string, string>();

            foreach (Match match in ClrMameFieldRegex.Matches(body))
            {
                fields[match.Groups["name"].Value.ToLowerInvariant()] = match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["bare"].Value;
            }

            return fields;
        }

        private static NoIntroCatalogSnapshotEntry CreateEntry(string name, string parentName, string fileName, string id)
        {
            var nameMatch = NumberedNameRegex.Match(name);
            var fileNameMatch = NumberedNameRegex.Match(fileName);
            var releaseNumber = id;
            var canonicalName = name;
            var canonicalFileName = fileName;

            if (nameMatch.Success)
            {
                releaseNumber = nameMatch.Groups["number"].Value;
                canonicalName = nameMatch.Groups["name"].Value;
            }

            if (fileNameMatch.Success)
            {
                releaseNumber ??= fileNameMatch.Groups["number"].Value;
                canonicalFileName = fileNameMatch.Groups["name"].Value;
            }

            return new NoIntroCatalogSnapshotEntry
            {
                CanonicalName = canonicalName,
                ParentCanonicalName = StripNumberPrefix(parentName),
                CanonicalFileName = canonicalFileName,
                ReleaseNumber = releaseNumber,
                NumberedCanonicalFileName = releaseNumber != null ? fileName : null
            };
        }

        private static string StripNumberPrefix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var match = NumberedNameRegex.Match(value);
            return match.Success ? match.Groups["name"].Value : value;
        }

        private static void AddHash(NoIntroCatalogSnapshotEntry entry, string hashType, string hashValue, bool isPrimary, bool isBadDump)
        {
            if (string.IsNullOrWhiteSpace(hashValue))
            {
                return;
            }

            entry.Hashes.Add(new NoIntroCatalogSnapshotHash
            {
                HashType = hashType,
                HashValue = hashValue,
                IsPrimary = isPrimary,
                IsBadDump = isBadDump
            });
        }

        private static bool IsBadDump(XElement rom)
        {
            var status = rom.Attribute("status")?.Value;
            return status != null && !status.Equals("verified", StringComparison.OrdinalIgnoreCase) && !status.Equals("good", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSystemKey(string value)
        {
            return string.Concat(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
        }
    }
}
