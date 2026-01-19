using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public class RedownloadFailedDownloadService : IHandle<DownloadFailedEvent>
    {
        private readonly IConfigService _configService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public RedownloadFailedDownloadService(IConfigService configService,
                                               IManageCommandQueue commandQueueManager,
                                               Logger logger)
        {
            _configService = configService;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(DownloadFailedEvent message)
        {
            if (message.SkipRedownload)
            {
                _logger.Debug("Skip redownloading requested by user");
                return;
            }

            if (!_configService.AutoRedownloadFailed)
            {
                _logger.Debug("Auto redownloading failed games is disabled");
                return;
            }

            if (message.ReleaseSource == ReleaseSourceType.InteractiveSearch && !_configService.AutoRedownloadFailedFromInteractiveSearch)
            {
                _logger.Debug("Auto redownloading failed games from interactive search is disabled");
                return;
            }

            if (message.GameId != 0)
            {
                _logger.Debug("Failed download contains a game, searching again.");
                _commandQueueManager.Push(new GamesSearchCommand { GameIds = new List<int> { message.GameId } });
            }
        }
    }
}
