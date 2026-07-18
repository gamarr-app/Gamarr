using System;
using System.Linq;
using System.Xml.Linq;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogSnapshotParser
    {
        NoIntroCatalogSnapshot Parse(string content);
    }

    public class NoIntroCatalogSnapshotParser : INoIntroCatalogSnapshotParser
    {
        public NoIntroCatalogSnapshot Parse(string content)
        {
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

                var entry = new NoIntroCatalogSnapshotEntry
                {
                    CanonicalName = canonicalName,
                    CanonicalFileName = game.Elements("rom").FirstOrDefault()?.Attribute("name")?.Value ?? canonicalName
                };

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
