using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.GameStats
{
    public interface IGameStatisticsService
    {
        List<GameStatistics> GameStatistics();
        GameStatistics GameStatistics(int gameId);
    }

    public class GameStatisticsService : IGameStatisticsService
    {
        private readonly IGameStatisticsRepository _gameStatisticsRepository;

        public GameStatisticsService(IGameStatisticsRepository gameStatisticsRepository)
        {
            _gameStatisticsRepository = gameStatisticsRepository;
        }

        public List<GameStatistics> GameStatistics()
        {
            var gameStatistics = _gameStatisticsRepository.GameStatistics();

            return gameStatistics.GroupBy(m => m.GameId).Select(m => m.First()).ToList();
        }

        public GameStatistics GameStatistics(int gameId)
        {
            var stats = _gameStatisticsRepository.GameStatistics(gameId);

            if (stats == null || stats.Count == 0)
            {
                return new GameStatistics();
            }

            return stats.First();
        }
    }
}
