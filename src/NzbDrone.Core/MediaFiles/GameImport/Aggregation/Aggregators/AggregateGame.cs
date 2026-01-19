using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators
{
    public class AggregateGame : IAggregateLocalGame
    {
        public int Order => 1;

        private readonly IGameService _gameService;

        public AggregateGame(IGameService gameService)
        {
            _gameService = gameService;
        }

        public LocalGame Aggregate(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            localGame.Game = _gameService.GetGame(localGame.Game.Id);

            return localGame;
        }
    }
}
