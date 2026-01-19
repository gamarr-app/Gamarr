using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Parser.RomanNumerals;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        Game GetGame(string title);
        RemoteGame Map(ParsedGameInfo parsedGameInfo, int igdbId, SearchCriteriaBase searchCriteria = null);
        RemoteGame Map(ParsedGameInfo parsedGameInfo, int gameId);
        ParsedGameInfo ParseMinimalPathGameInfo(string path);
    }

    public class ParsingService : IParsingService
    {
        private static HashSet<ArabicRomanNumeral> _arabicRomanNumeralMappings;

        private readonly IGameService _gameService;
        private readonly Logger _logger;

        public ParsingService(IGameService gameService,
                              Logger logger)
        {
            _gameService = gameService;
            _logger = logger;

            if (_arabicRomanNumeralMappings == null)
            {
                _arabicRomanNumeralMappings = RomanNumeralParser.GetArabicRomanNumeralsMapping();
            }
        }

        public ParsedGameInfo ParseMinimalPathGameInfo(string path)
        {
            var fileInfo = new FileInfo(path);

            var result = Parser.ParseGameTitle(fileInfo.Name, true);

            if (result == null)
            {
                _logger.Debug("Attempting to parse game info using directory and file names. '{0}'", fileInfo.Directory.Name);
                result = Parser.ParseGameTitle(fileInfo.Directory.Name + " " + fileInfo.Name);
            }

            if (result == null)
            {
                _logger.Debug("Attempting to parse game info using directory name. '{0}'", fileInfo.Directory.Name);
                result = Parser.ParseGameTitle(fileInfo.Directory.Name + fileInfo.Extension);
            }

            return result;
        }

        public Game GetGame(string title)
        {
            var parsedGameInfo = Parser.ParseGameTitle(title);

            if (parsedGameInfo == null)
            {
                return _gameService.FindByTitle(title);
            }

            var result = TryGetGameByTitleAndOrYear(parsedGameInfo);

            if (result != null)
            {
                return result.Game;
            }

            return null;
        }

        public RemoteGame Map(ParsedGameInfo parsedGameInfo, int igdbId, SearchCriteriaBase searchCriteria = null)
        {
            return Map(parsedGameInfo, igdbId, null, searchCriteria);
        }

        public RemoteGame Map(ParsedGameInfo parsedGameInfo, int gameId)
        {
            return new RemoteGame
            {
                ParsedGameInfo = parsedGameInfo,
                Game = _gameService.GetGame(gameId)
            };
        }

        private RemoteGame Map(ParsedGameInfo parsedGameInfo, int igdbId, Game game, SearchCriteriaBase searchCriteria)
        {
            var remoteGame = new RemoteGame
            {
                ParsedGameInfo = parsedGameInfo
            };

            if (game == null)
            {
                var gameMatch = FindGame(parsedGameInfo, igdbId, searchCriteria);

                if (gameMatch != null)
                {
                    game = gameMatch.Game;
                    remoteGame.GameMatchType = gameMatch.MatchType;
                }
            }

            if (game != null)
            {
                remoteGame.Game = game;
            }

            remoteGame.Languages = parsedGameInfo.Languages;

            if (searchCriteria != null)
            {
                remoteGame.GameRequested = remoteGame.Game?.Id == searchCriteria.Game?.Id;
            }

            return remoteGame;
        }

        private FindGameResult FindGame(ParsedGameInfo parsedGameInfo, int igdbId, SearchCriteriaBase searchCriteria)
        {
            FindGameResult result = null;

            if (igdbId > 0)
            {
                result = TryGetGameByIgdbId(parsedGameInfo, igdbId);
            }

            if (result == null)
            {
                if (searchCriteria != null)
                {
                    result = TryGetGameBySearchCriteria(parsedGameInfo, igdbId, searchCriteria);
                }
                else
                {
                    result = TryGetGameByTitleAndOrYear(parsedGameInfo);
                }
            }

            if (result == null)
            {
                _logger.Debug($"No matching game for titles '{string.Join(", ", parsedGameInfo.GameTitles)} ({parsedGameInfo.Year})'");
            }

            return result;
        }

        private FindGameResult TryGetGameByIgdbId(ParsedGameInfo parsedGameInfo, int igdbId)
        {
            var game = _gameService.FindByIgdbId(igdbId);

            // Should fix practically all problems, where indexer is shite at adding correct IDs to games.
            if (game != null && (parsedGameInfo.Year < 1800 || game.GameMetadata.Value.Year == parsedGameInfo.Year || game.GameMetadata.Value.SecondaryYear == parsedGameInfo.Year))
            {
                return new FindGameResult(game, GameMatchType.Id);
            }

            return null;
        }

        private FindGameResult TryGetGameByTitleAndOrYear(ParsedGameInfo parsedGameInfo)
        {
            var candidates = _gameService.FindByTitleCandidates(parsedGameInfo.GameTitles, out var otherTitles);

            Game gameByTitleAndOrYear;
            if (parsedGameInfo.Year > 1800)
            {
                gameByTitleAndOrYear = _gameService.FindByTitle(parsedGameInfo.GameTitles, parsedGameInfo.Year, otherTitles, candidates);
                if (gameByTitleAndOrYear != null)
                {
                    return new FindGameResult(gameByTitleAndOrYear, GameMatchType.Title);
                }

                return null;
            }

            gameByTitleAndOrYear = _gameService.FindByTitle(parsedGameInfo.GameTitles, null, otherTitles, candidates);
            if (gameByTitleAndOrYear != null)
            {
                return new FindGameResult(gameByTitleAndOrYear, GameMatchType.Title);
            }

            return null;
        }

        private FindGameResult TryGetGameBySearchCriteria(ParsedGameInfo parsedGameInfo, int igdbId, SearchCriteriaBase searchCriteria)
        {
            Game possibleGame = null;

            var possibleTitles = new List<string>
            {
                searchCriteria.Game.GameMetadata.Value.CleanTitle
            };
            possibleTitles.AddIfNotNull(searchCriteria.Game.GameMetadata.Value.CleanOriginalTitle);
            possibleTitles.AddRange(searchCriteria.Game.GameMetadata.Value.AlternativeTitles.Select(t => t.CleanTitle));
            possibleTitles.AddRange(searchCriteria.Game.GameMetadata.Value.Translations.Select(t => t.CleanTitle));

            var cleanTitles = parsedGameInfo.GameTitles.Select(t => t.CleanGameTitle()).ToArray();

            if (possibleTitles.Any(pt =>
                cleanTitles.Contains(pt)
                || _arabicRomanNumeralMappings.Any(mn =>
                    cleanTitles.Contains(pt.Replace(mn.ArabicNumeralAsString, mn.RomanNumeralLowerCase))
                    || cleanTitles.Any(t => t.Replace(mn.ArabicNumeralAsString, mn.RomanNumeralLowerCase) == pt))))
            {
                possibleGame = searchCriteria.Game;
            }

            if (possibleGame != null)
            {
                if (parsedGameInfo.Year < 1800 || possibleGame.GameMetadata.Value.Year == parsedGameInfo.Year || possibleGame.GameMetadata.Value.SecondaryYear == parsedGameInfo.Year)
                {
                    return new FindGameResult(possibleGame, GameMatchType.Title);
                }
            }

            if (igdbId > 0 && igdbId == searchCriteria.Game.IgdbId)
            {
                return new FindGameResult(searchCriteria.Game, GameMatchType.Id);
            }

            return null;
        }
    }
}
