using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Download.Aggregation.Aggregators;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Aggregation
{
    public interface IRemoteGameAggregationService
    {
        RemoteGame Augment(RemoteGame remoteGame);
    }

    public class RemoteGameAggregationService : IRemoteGameAggregationService
    {
        private readonly IEnumerable<IAggregateRemoteGame> _augmenters;
        private readonly Logger _logger;

        public RemoteGameAggregationService(IEnumerable<IAggregateRemoteGame> augmenters,
                                  Logger logger)
        {
            _augmenters = augmenters;
            _logger = logger;
        }

        public RemoteGame Augment(RemoteGame remoteGame)
        {
            if (remoteGame == null)
            {
                return null;
            }

            foreach (var augmenter in _augmenters)
            {
                try
                {
                    augmenter.Aggregate(remoteGame);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, ex.Message);
                }
            }

            return remoteGame;
        }
    }
}
