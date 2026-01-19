using NzbDrone.Core.Games;

namespace NzbDrone.Core.Parser.Model
{
    public class FindGameResult
    {
        public Game Game { get; set; }
        public GameMatchType MatchType { get; set; }

        public FindGameResult(Game game, GameMatchType matchType)
        {
            Game = game;
            MatchType = matchType;
        }
    }

    public enum GameMatchType
    {
        Unknown = 0,
        Title = 1,
        Alias = 2,
        Id = 3
    }
}
