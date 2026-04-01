using NLog;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public interface IUpdateMediaInfo
    {
        bool Update(GameFile gameFile, Game game);
        bool UpdateMediaInfo(GameFile gameFile, Game game);
    }

    public class UpdateMediaInfoService : IUpdateMediaInfo, IHandle<GameScannedEvent>
    {
        private readonly Logger _logger;

        public UpdateMediaInfoService(Logger logger)
        {
            _logger = logger;
        }

        public void Handle(GameScannedEvent message)
        {
            // No-op: ffprobe media info scanning is not applicable for game files
            _logger.Debug("Skipping media info update for game files (not applicable)");
        }

        public bool Update(GameFile gameFile, Game game)
        {
            // No-op: ffprobe media info scanning is not applicable for game files
            return false;
        }

        public bool UpdateMediaInfo(GameFile gameFile, Game game)
        {
            // No-op: ffprobe media info scanning is not applicable for game files
            return false;
        }
    }
}
