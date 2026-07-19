using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NLog;
using NzbDrone.Core.Games;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroCatalogSyncService
    {
        void Sync(int? catalogSourceId = null);
    }

    public class NoIntroCatalogSyncService : IExecute<NoIntroCatalogSyncCommand>, INoIntroCatalogSyncService
    {
        private readonly NoIntroCatalogSourceRepository _sourceRepository;
        private readonly NoIntroCatalogEntryRepository _entryRepository;
        private readonly NoIntroCatalogHashRepository _hashRepository;
        private readonly INoIntroCatalogDocumentClient _documentClient;
        private readonly NoIntroCatalogSnapshotParser _snapshotParser;
        private readonly Logger _logger;

        public NoIntroCatalogSyncService(
            NoIntroCatalogSourceRepository sourceRepository,
            NoIntroCatalogEntryRepository entryRepository,
            NoIntroCatalogHashRepository hashRepository,
            INoIntroCatalogDocumentClient documentClient,
            NoIntroCatalogSnapshotParser snapshotParser,
            Logger logger)
        {
            _sourceRepository = sourceRepository;
            _entryRepository = entryRepository;
            _hashRepository = hashRepository;
            _documentClient = documentClient;
            _snapshotParser = snapshotParser;
            _logger = logger;
        }

        public void Execute(NoIntroCatalogSyncCommand message)
        {
            Sync(message.CatalogSourceId);
        }

        public void Sync(int? catalogSourceId = null)
        {
            var sources = catalogSourceId.HasValue
                ? new List<NoIntroCatalogSource> { _sourceRepository.Get(catalogSourceId.Value) }
                : _sourceRepository.All().ToList();

            if (!catalogSourceId.HasValue)
            {
                EnsureDefaultSources(sources);
            }

            foreach (var source in sources)
            {
                SyncSource(source);
            }
        }

        private void SyncSource(NoIntroCatalogSource source)
        {
            source.LastAttemptedSync = DateTime.UtcNow;
            _sourceRepository.Update(source);

            try
            {
                var snapshot = _snapshotParser.Parse(_documentClient.Fetch(source.SourceUrl));
                EnrichWithNumberedCatalog(snapshot);
                ReplaceCatalog(source, snapshot);

                source.CatalogVersion = snapshot.CatalogVersion;
                source.LastSuccessfulSync = DateTime.UtcNow;
                source.LastSyncError = null;
                _sourceRepository.Update(source);
            }
            catch (Exception ex)
            {
                source.LastSyncError = ex.Message;
                _sourceRepository.Update(source);
                _logger.Warn(ex, "Failed syncing No-Intro catalog source {0}", source.SourceUrl);
                throw;
            }
        }

        private void ReplaceCatalog(NoIntroCatalogSource source, NoIntroCatalogSnapshot snapshot)
        {
            var existingEntries = _entryRepository.GetBySourceId(source.Id);
            var existingEntryIds = existingEntries.Select(x => x.Id).ToList();

            if (existingEntryIds.Count > 0)
            {
                _hashRepository.DeleteByEntryIds(existingEntryIds);
                _entryRepository.DeleteBySourceId(source.Id);
            }

            var entries = snapshot.Entries.Select(entry => new NoIntroCatalogEntry
            {
                CatalogSourceId = source.Id,
                SystemKey = snapshot.SystemKey,
                CanonicalName = entry.CanonicalName,
                ParentCanonicalName = entry.ParentCanonicalName,
                PlatformFamily = MapPlatformFamily(snapshot.SystemKey),
                CanonicalFileName = entry.CanonicalFileName,
                ReleaseNumber = entry.ReleaseNumber,
                NumberedCanonicalFileName = entry.NumberedCanonicalFileName
            }).ToList();

            _entryRepository.InsertMany(entries);

            var hashes = new List<NoIntroCatalogHash>();

            for (var i = 0; i < entries.Count; i++)
            {
                hashes.AddRange(snapshot.Entries[i].Hashes.Select(hash => new NoIntroCatalogHash
                {
                    CatalogEntryId = entries[i].Id,
                    HashType = hash.HashType,
                    HashValue = hash.HashValue,
                    IsPrimary = hash.IsPrimary,
                    IsBadDump = hash.IsBadDump
                }));
            }

            if (hashes.Count > 0)
            {
                _hashRepository.InsertMany(hashes);
            }
        }

        private void EnsureDefaultSources(List<NoIntroCatalogSource> sources)
        {
            AddDefaultSource(sources, "No-Intro Nintendo Game Boy", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy.dat");
            AddDefaultSource(sources, "No-Intro Nintendo Game Boy Color", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy%20Color.dat");
            AddDefaultSource(sources, "No-Intro Nintendo Game Boy Advance", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Game%20Boy%20Advance.dat");
            AddDefaultSource(sources, "No-Intro Nintendo DS", "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DS.dat");
        }

        private void AddDefaultSource(List<NoIntroCatalogSource> sources, string name, string sourceUrl)
        {
            if (sources.Any(source => source.SourceUrl == sourceUrl))
            {
                return;
            }

            sources.Add(_sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = name,
                SourceUrl = sourceUrl
            }));
        }

        private void EnrichWithNumberedCatalog(NoIntroCatalogSnapshot snapshot)
        {
            var systemId = GetDatOMaticSystemId(snapshot.SystemKey);

            if (!systemId.HasValue)
            {
                return;
            }

            try
            {
                var numberedSnapshot = _snapshotParser.Parse(_documentClient.FetchDatOMaticNumbered(systemId.Value));
                var numberedByHash = numberedSnapshot.Entries
                    .SelectMany(entry => entry.Hashes.Select(hash => new { Key = HashKey(hash), Entry = entry }))
                    .GroupBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.First().Entry);

                foreach (var entry in snapshot.Entries)
                {
                    var numberedEntry = entry.Hashes
                        .Select(hash => numberedByHash.GetValueOrDefault(HashKey(hash)))
                        .FirstOrDefault(match => match?.NumberedCanonicalFileName != null);

                    if (numberedEntry == null)
                    {
                        continue;
                    }

                    entry.ReleaseNumber = numberedEntry.ReleaseNumber;
                    entry.NumberedCanonicalFileName = numberedEntry.NumberedCanonicalFileName;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed enriching No-Intro catalog {0} with DAT-o-MATIC numbered filenames", snapshot.SystemKey);
            }

            EnrichWithAdvansceneCatalog(snapshot);
        }

        private void EnrichWithAdvansceneCatalog(NoIntroCatalogSnapshot snapshot)
        {
            var sourceUrl = GetAdvansceneSourceUrl(snapshot.SystemKey);

            if (sourceUrl == null)
            {
                return;
            }

            try
            {
                var releaseNumbersByCrc = ParseAdvansceneReleaseNumbers(_documentClient.FetchAdvanscene(sourceUrl));

                foreach (var entry in snapshot.Entries.Where(x => x.NumberedCanonicalFileName == null))
                {
                    var crc = entry.Hashes.FirstOrDefault(x => x.HashType.Equals("crc32", StringComparison.OrdinalIgnoreCase));

                    if (crc == null || !releaseNumbersByCrc.TryGetValue(crc.HashValue.ToUpperInvariant(), out var releaseNumber))
                    {
                        continue;
                    }

                    entry.ReleaseNumber = releaseNumber;
                    entry.NumberedCanonicalFileName = $"{releaseNumber} - {entry.CanonicalFileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed enriching No-Intro catalog {0} with ADVANsCEne release numbers", snapshot.SystemKey);
            }
        }

        private static Dictionary<string, string> ParseAdvansceneReleaseNumbers(string content)
        {
            var document = XDocument.Parse(content);

            return document.Descendants("game")
                .Select(game => new
                {
                    ReleaseNumber = FormatReleaseNumber(game.Element("releaseNumber")?.Value),
                    Crc = game.Element("files")?.Elements("romCRC").FirstOrDefault()?.Value
                })
                .Where(x => x.ReleaseNumber != null && !string.IsNullOrWhiteSpace(x.Crc))
                .GroupBy(x => x.Crc.ToUpperInvariant())
                .ToDictionary(x => x.Key, x => x.First().ReleaseNumber);
        }

        private static string FormatReleaseNumber(string value)
        {
            return int.TryParse(value, out var number) ? number.ToString("0000") : null;
        }

        private static string HashKey(NoIntroCatalogSnapshotHash hash)
        {
            return $"{hash.HashType}:{hash.HashValue}".ToLowerInvariant();
        }

        private static int? GetDatOMaticSystemId(string systemKey)
        {
            return systemKey switch
            {
                "nintendo---game-boy" => 46,
                "nintendo---game-boy-color" => 47,
                "nintendo---game-boy-advance" => 23,
                "nintendo---nintendo-ds" => 28,
                _ => null
            };
        }

        private static string GetAdvansceneSourceUrl(string systemKey)
        {
            return systemKey switch
            {
                "nintendo---game-boy-advance" => "https://advanscene.com/offline/datas/ADVANsCEne_GBA.zip",
                "nintendo---nintendo-ds" => "https://advanscene.com/offline/datas/ADVANsCEne_NDS_S.zip",
                _ => null
            };
        }

        private static PlatformFamily MapPlatformFamily(string systemKey)
        {
            return systemKey switch
            {
                "nintendo---game-boy" => PlatformFamily.NintendoGB,
                "nintendo---game-boy-color" => PlatformFamily.NintendoGBC,
                "nintendo---game-boy-advance" => PlatformFamily.NintendoGBA,
                "nintendo---nintendo-ds" => PlatformFamily.NintendoDS,
                _ => PlatformFamily.Nintendo
            };
        }
    }
}
