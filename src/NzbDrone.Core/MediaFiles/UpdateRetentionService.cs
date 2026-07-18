using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    // Retention for update components (#149): when a new update imports, keep
    // the newest N update folders (default 3) plus, optionally, the newest of
    // each major version; older ones go to the recycle bin along with their
    // records and component slots. UpdateRetentionCount of 0 keeps everything.
    public class UpdateRetentionService : IHandle<GameFileAddedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IGameComponentRepository _componentRepository;
        private readonly IGameService _gameService;
        private readonly IConfigService _configService;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public UpdateRetentionService(IMediaFileService mediaFileService,
                                      IGameComponentRepository componentRepository,
                                      IGameService gameService,
                                      IConfigService configService,
                                      IRecycleBinProvider recycleBinProvider,
                                      IDiskProvider diskProvider,
                                      Logger logger)
        {
            _mediaFileService = mediaFileService;
            _componentRepository = componentRepository;
            _gameService = gameService;
            _configService = configService;
            _recycleBinProvider = recycleBinProvider;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        private static bool IsUpdateUnit(GameFile file)
        {
            return file.RelativePath.IsNotNullOrWhiteSpace() &&
                   file.RelativePath.Replace('\\', '/').StartsWith("Updates/");
        }

        public void Handle(GameFileAddedEvent message)
        {
            if (!IsUpdateUnit(message.GameFile))
            {
                return;
            }

            var retention = _configService.UpdateRetentionCount;

            if (retention <= 0)
            {
                return;
            }

            var game = message.GameFile.Game ?? _gameService.GetGame(message.GameFile.GameId);

            if (game == null)
            {
                return;
            }

            var updates = _mediaFileService.GetFilesByGame(game.Id)
                                           .Where(IsUpdateUnit)
                                           .OrderByDescending(f => f.GameVersion ?? new GameVersion())
                                           .ToList();

            if (updates.Count <= retention)
            {
                return;
            }

            var keep = updates.Take(retention).Select(f => f.Id).ToHashSet();

            if (_configService.UpdateRetentionKeepOnePerMajor)
            {
                foreach (var majorGroup in updates.Where(f => f.GameVersion?.HasValue == true)
                                                  .GroupBy(f => f.GameVersion.Major))
                {
                    // List is sorted newest-first, so First() is the newest
                    // update of this major version.
                    keep.Add(majorGroup.First().Id);
                }
            }

            foreach (var stale in updates.Where(f => !keep.Contains(f.Id)).ToList())
            {
                var path = Path.Combine(game.Path, stale.RelativePath);

                if (_diskProvider.FolderExists(path))
                {
                    _recycleBinProvider.DeleteFolder(path);
                }

                _mediaFileService.Delete(stale, DeleteMediaFileReason.Upgrade);

                if (stale.ComponentId > 0)
                {
                    _componentRepository.Delete(stale.ComponentId);
                }

                _logger.Info("Update retention: removed {0} from {1} (keeping {2} newest)", stale.RelativePath, game.Title, retention);
            }
        }
    }
}
