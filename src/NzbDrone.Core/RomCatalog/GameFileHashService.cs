using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.RomCatalog
{
    public interface IGameFileHashService
    {
        List<GameFileHash> GetCurrentHashes(List<GameFile> gameFiles);
        void QueueHashing(int gameId);
        void HashGameFiles(int gameId);
    }

    /// <summary>
    /// Computes and stores the No-Intro hash triplet of every component-linked
    /// game file. Hashing only ever happens here — on import, on rescan, or when
    /// something (the components endpoint) notices a file it has no hashes for.
    /// Readers get whatever is already stored (#205).
    /// </summary>
    public class GameFileHashService : IGameFileHashService,
                                       IExecute<GameFileHashCommand>,
                                       IHandle<GameFileAddedEvent>,
                                       IHandle<GameScannedEvent>,
                                       IHandle<GameFileDeletedEvent>
    {
        private readonly IGameFileHashRepository _repository;
        private readonly IMediaFileService _mediaFileService;
        private readonly IGameService _gameService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public GameFileHashService(IGameFileHashRepository repository,
                                   IMediaFileService mediaFileService,
                                   IGameService gameService,
                                   IManageCommandQueue commandQueueManager,
                                   IDiskProvider diskProvider,
                                   Logger logger)
        {
            _repository = repository;
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _commandQueueManager = commandQueueManager;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        /// <summary>
        /// The stored triplets that still describe the given files. Database only
        /// — no disk access, so this is safe on a request path. The size recorded
        /// at hashing time is compared against the size the scan last saw; the
        /// last-write half of the key is checked by the hashing command itself,
        /// which runs on every rescan.
        /// </summary>
        public List<GameFileHash> GetCurrentHashes(List<GameFile> gameFiles)
        {
            var fileById = gameFiles.ToDictionary(x => x.Id);

            return _repository.GetByGameFileIds(fileById.Keys.ToList())
                .Where(hash => fileById[hash.GameFileId].Size == hash.Size)
                .ToList();
        }

        public void QueueHashing(int gameId)
        {
            if (gameId <= 0)
            {
                return;
            }

            // The queue collapses an identical command that is still pending, so
            // a burst of panel loads for the same game queues one hash pass.
            _commandQueueManager.Push(new GameFileHashCommand(gameId));
        }

        public void HashGameFiles(int gameId)
        {
            var game = _gameService.FindGame(gameId);

            if (game == null)
            {
                return;
            }

            var files = _mediaFileService.GetFilesByGame(gameId).Where(x => x.ComponentId > 0).ToList();
            var storedByFileId = _repository.GetByGameFileIds(files.Select(x => x.Id).ToList())
                .GroupBy(x => x.GameFileId)
                .ToDictionary(x => x.Key, x => x.First());

            foreach (var file in files)
            {
                storedByFileId.TryGetValue(file.Id, out var stored);
                HashGameFile(file, game, stored);
            }
        }

        public void Execute(GameFileHashCommand message)
        {
            HashGameFiles(message.GameId);
        }

        public void Handle(GameFileAddedEvent message)
        {
            QueueHashing(message.GameFile.GameId);
        }

        public void Handle(GameScannedEvent message)
        {
            QueueHashing(message.Game.Id);
        }

        public void Handle(GameFileDeletedEvent message)
        {
            _repository.DeleteByGameFileIds(new List<int> { message.GameFile.Id });
        }

        private void HashGameFile(GameFile file, Game game, GameFileHash stored)
        {
            var path = file.GetPath(game);

            try
            {
                // A folder release has no single file to hash; No-Intro matching
                // doesn't apply to it either.
                if (!_diskProvider.FileExists(path))
                {
                    return;
                }

                var size = _diskProvider.GetFileSize(path);
                var lastModified = _diskProvider.FileGetLastWrite(path);

                if (stored != null && stored.Matches(size, lastModified))
                {
                    return;
                }

                NoIntroHashTriplet hashes;

                using (var stream = _diskProvider.OpenReadStream(path))
                {
                    hashes = NoIntroRomHasher.Compute(stream);
                }

                Store(stored, file.Id, size, lastModified, hashes);
                _logger.Debug("Hashed game file {0} for No-Intro catalog matching", path);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed hashing game file {0} for No-Intro catalog matching", path);
            }
        }

        private void Store(GameFileHash stored, int gameFileId, long size, DateTime lastModified, NoIntroHashTriplet hashes)
        {
            var hash = stored ?? new GameFileHash { GameFileId = gameFileId };

            hash.Size = size;
            hash.LastModifiedUtcTicks = GameFileHash.ToUtcTicks(lastModified);
            hash.Md5 = hashes.Md5;
            hash.Sha1 = hashes.Sha1;
            hash.Crc32 = hashes.Crc32;
            hash.HashedAt = DateTime.UtcNow;

            if (hash.Id == 0)
            {
                _repository.Insert(hash);
            }
            else
            {
                _repository.Update(hash);
            }
        }
    }
}
