using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
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
                CanonicalFileName = entry.CanonicalFileName
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
    }
}
