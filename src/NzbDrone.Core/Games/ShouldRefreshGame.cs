using System;
using NLog;

namespace NzbDrone.Core.Games
{
    public interface ICheckIfGameShouldBeRefreshed
    {
        bool ShouldRefresh(GameMetadata game);
    }

    public class ShouldRefreshGame : ICheckIfGameShouldBeRefreshed
    {
        private readonly Logger _logger;

        public ShouldRefreshGame(Logger logger)
        {
            _logger = logger;
        }

        public bool ShouldRefresh(GameMetadata game)
        {
            try
            {
                if (game == null)
                {
                    _logger.Warn("Game metadata does not exist, should not be refreshed.");
                    return false;
                }

                if (game.LastInfoSync < DateTime.UtcNow.AddDays(-180))
                {
                    _logger.Trace("Game {0} last updated more than 180 days ago, should refresh.", game.Title);
                    return true;
                }

                if (game.LastInfoSync >= DateTime.UtcNow.AddHours(-12))
                {
                    _logger.Trace("Game {0} last updated less than 12 hours ago, should not be refreshed.",
                        game.Title);
                    return false;
                }

                if (game.Status is GameStatusType.Announced or GameStatusType.InDevelopment)
                {
                    _logger.Trace("Game {0} is announced or in cinemas, should refresh.", game.Title);
                    return true;
                }

                if (game.Status == GameStatusType.Released &&
                    game.PhysicalReleaseDate() >= DateTime.UtcNow.AddDays(-30))
                {
                    _logger.Trace("Game {0} is released since less than 30 days, should refresh", game.Title);
                    return true;
                }

                _logger.Trace("Game {0} came out long ago, should not be refreshed.", game.Title);
                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to determine if game metadata should refresh, will try to refresh.");
                return true;
            }
        }
    }
}
