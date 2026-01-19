using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.RomanNumerals;

#pragma warning disable CS0618 // Disable obsolete warnings for ImdbId (kept for backward compatibility)

namespace NzbDrone.Core.Games
{
    public interface IGameService
    {
        Game GetGame(int gameId);
        List<Game> GetGames(IEnumerable<int> gameIds);
        PagingSpec<Game> Paged(PagingSpec<Game> pagingSpec);
        Game AddGame(Game newGame);
        List<Game> AddGames(List<Game> newGames);
        Game FindByImdbId(string imdbid);
        Game FindByIgdbId(int igdbid);
        Game FindBySteamAppId(int steamAppId);
        Game FindByTitle(string title);
        Game FindByTitle(string title, int year);
        Game FindByTitle(List<string> titles, int? year, List<string> otherTitles, List<Game> candidates);
        List<Game> FindByTitleCandidates(List<string> titles, out List<string> otherTitles);
        Game FindByPath(string path);
        Dictionary<int, string> AllGamePaths();
        List<int> AllGameIgdbIds();
        bool GameExists(Game game);
        List<Game> GetGamesByFileId(int fileId);
        List<Game> GetGamesByCollectionIgdbId(int collectionId);
        List<Game> GetGamesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored);
        PagingSpec<Game> GamesWithoutFiles(PagingSpec<Game> pagingSpec);
        void DeleteGame(int gameId, bool deleteFiles, bool addImportListExclusion = false);
        void DeleteGames(List<int> gameIds, bool deleteFiles, bool addImportListExclusion = false);
        List<Game> GetAllGames();
        Dictionary<int, List<int>> AllGameTags();
        Game UpdateGame(Game game);
        List<Game> UpdateGame(List<Game> games, bool useExistingRelativeFolder);
        void UpdateLastSearchTime(Game game);
        List<int> GetRecommendedIgdbIds();
        bool GamePathExists(string folder);
        void RemoveAddOptions(Game game);
        bool UpdateTags(Game game);
        bool ExistsByMetadataId(int metadataId);
        HashSet<int> AllGameWithCollectionsIgdbIds();
    }

    public class GameService : IGameService, IHandle<GameFileAddedEvent>,
                                               IHandle<GameFileDeletedEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildGamePaths _gamePathBuilder;
        private readonly IAutoTaggingService _autoTaggingService;
        private readonly Logger _logger;

        public GameService(IGameRepository gameRepository,
                            IEventAggregator eventAggregator,
                            IConfigService configService,
                            IBuildGamePaths gamePathBuilder,
                            IAutoTaggingService autoTaggingService,
                            Logger logger)
        {
            _gameRepository = gameRepository;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _gamePathBuilder = gamePathBuilder;
            _autoTaggingService = autoTaggingService;
            _logger = logger;
        }

        public Game GetGame(int gameId)
        {
            return _gameRepository.Get(gameId);
        }

        public List<Game> GetGames(IEnumerable<int> gameIds)
        {
            return _gameRepository.Get(gameIds).ToList();
        }

        public PagingSpec<Game> Paged(PagingSpec<Game> pagingSpec)
        {
            return _gameRepository.GetPaged(pagingSpec);
        }

        public Game AddGame(Game newGame)
        {
            var game = _gameRepository.Insert(newGame);

            _eventAggregator.PublishEvent(new GameAddedEvent(GetGame(game.Id)));

            return game;
        }

        public List<Game> AddGames(List<Game> newGames)
        {
            _gameRepository.InsertMany(newGames);

            _eventAggregator.PublishEvent(new GamesImportedEvent(newGames));

            return newGames;
        }

        public Game FindByTitle(string title)
        {
            var candidates = FindByTitleCandidates(new List<string> { title }, out var otherTitles);

            return FindByTitle(new List<string> { title }, null, otherTitles, candidates);
        }

        public Game FindByTitle(string title, int year)
        {
            var candidates = FindByTitleCandidates(new List<string> { title }, out var otherTitles);

            return FindByTitle(new List<string> { title }, year, otherTitles, candidates);
        }

        public Game FindByTitle(List<string> titles, int? year, List<string> otherTitles, List<Game> candidates)
        {
            var cleanTitles = titles.Select(t => t.CleanGameTitle().ToLowerInvariant());

            var result = candidates.Where(x => cleanTitles.Contains(x.GameMetadata.Value.CleanTitle) || cleanTitles.Contains(x.GameMetadata.Value.CleanOriginalTitle))
                .AllWithYear(year)
                .ToList();

            if (result == null || result.Count == 0)
            {
                result =
                    candidates.Where(game => otherTitles.Contains(game.GameMetadata.Value.CleanTitle)).AllWithYear(year).ToList();
            }

            if (result == null || result.Count == 0)
            {
                result = candidates
                    .Where(m => m.GameMetadata.Value.AlternativeTitles.Any(t => cleanTitles.Contains(t.CleanTitle) ||
                                                        otherTitles.Contains(t.CleanTitle)))
                    .AllWithYear(year).ToList();
            }

            if (result == null || result.Count == 0)
            {
                result = candidates
                    .Where(m => m.GameMetadata.Value.Translations.Any(t => cleanTitles.Contains(t.CleanTitle) ||
                                                        otherTitles.Contains(t.CleanTitle)))
                    .AllWithYear(year).ToList();
            }

            return ReturnSingleGameOrThrow(result.ToList());
        }

        public List<Game> FindByTitleCandidates(List<string> titles, out List<string> otherTitles)
        {
            var lookupTitles = new List<string>();
            otherTitles = new List<string>();

            foreach (var title in titles)
            {
                var cleanTitle = title.CleanGameTitle().ToLowerInvariant();
                var romanTitle = cleanTitle;
                var arabicTitle = cleanTitle;

                foreach (var arabicRomanNumeral in RomanNumeralParser.GetArabicRomanNumeralsMapping())
                {
                    var arabicNumber = arabicRomanNumeral.ArabicNumeralAsString;
                    var romanNumber = arabicRomanNumeral.RomanNumeral;

                    romanTitle = romanTitle.Replace(arabicNumber, romanNumber);
                    arabicTitle = arabicTitle.Replace(romanNumber, arabicNumber);
                }

                romanTitle = romanTitle.ToLowerInvariant();

                otherTitles.AddRange(new List<string> { arabicTitle, romanTitle });
                lookupTitles.AddRange(new List<string> { cleanTitle, arabicTitle, romanTitle });
            }

            return _gameRepository.FindByTitles(lookupTitles);
        }

        public Game FindByImdbId(string imdbid)
        {
            return _gameRepository.FindByImdbId(imdbid);
        }

        public Game FindByIgdbId(int igdbid)
        {
            return _gameRepository.FindByIgdbId(igdbid);
        }

        public Game FindBySteamAppId(int steamAppId)
        {
            return _gameRepository.FindBySteamAppId(steamAppId);
        }

        public Game FindByPath(string path)
        {
            return _gameRepository.FindByPath(path);
        }

        public Dictionary<int, string> AllGamePaths()
        {
            return _gameRepository.AllGamePaths();
        }

        public List<int> AllGameIgdbIds()
        {
            return _gameRepository.AllGameIgdbIds();
        }

        public void DeleteGame(int gameId, bool deleteFiles, bool addImportListExclusion = false)
        {
            var game = _gameRepository.Get(gameId);

            _gameRepository.Delete(gameId);
            _eventAggregator.PublishEvent(new GamesDeletedEvent(new List<Game> { game }, deleteFiles, addImportListExclusion));
            _logger.Info("Deleted game {0}", game);
        }

        public void DeleteGames(List<int> gameIds, bool deleteFiles, bool addImportListExclusion = false)
        {
            var gamesToDelete = _gameRepository.Get(gameIds).ToList();

            _gameRepository.DeleteMany(gameIds);

            _eventAggregator.PublishEvent(new GamesDeletedEvent(gamesToDelete, deleteFiles, addImportListExclusion));

            foreach (var game in gamesToDelete)
            {
                _logger.Info("Deleted game {0}", game);
            }
        }

        public List<Game> GetAllGames()
        {
            return _gameRepository.All().ToList();
        }

        public Dictionary<int, List<int>> AllGameTags()
        {
            return _gameRepository.AllGameTags();
        }

        public Game UpdateGame(Game game)
        {
            var storedGame = GetGame(game.Id);

            UpdateTags(game);

            var updatedGame = _gameRepository.Update(game);
            _eventAggregator.PublishEvent(new GameEditedEvent(updatedGame, storedGame));

            return updatedGame;
        }

        public List<Game> UpdateGame(List<Game> games, bool useExistingRelativeFolder)
        {
            _logger.Debug("Updating {0} games", games.Count);

            foreach (var m in games)
            {
                _logger.Trace("Updating: {0}", m.Title);

                if (!m.RootFolderPath.IsNullOrWhiteSpace())
                {
                    m.Path = _gamePathBuilder.BuildPath(m, useExistingRelativeFolder);

                    _logger.Trace("Changing path for {0} to {1}", m.Title, m.Path);
                }
                else
                {
                    _logger.Trace("Not changing path for: {0}", m.Title);
                }

                UpdateTags(m);
            }

            _gameRepository.UpdateMany(games);
            _logger.Debug("{0} games updated", games.Count);
            _eventAggregator.PublishEvent(new GamesBulkEditedEvent(games));

            return games;
        }

        public void UpdateLastSearchTime(Game game)
        {
            _gameRepository.SetFields(game, e => e.LastSearchTime);
        }

        public bool GamePathExists(string folder)
        {
            return _gameRepository.GamePathExists(folder);
        }

        public void RemoveAddOptions(Game game)
        {
            _gameRepository.SetFields(game, s => s.AddOptions);
        }

        public bool UpdateTags(Game game)
        {
            _logger.Trace("Updating tags for {0}", game);

            var tagsAdded = new HashSet<int>();
            var tagsRemoved = new HashSet<int>();
            var changes = _autoTaggingService.GetTagChanges(game);

            foreach (var tag in changes.TagsToRemove)
            {
                if (game.Tags.Contains(tag))
                {
                    game.Tags.Remove(tag);
                    tagsRemoved.Add(tag);
                }
            }

            foreach (var tag in changes.TagsToAdd)
            {
                if (!game.Tags.Contains(tag))
                {
                    game.Tags.Add(tag);
                    tagsAdded.Add(tag);
                }
            }

            if (tagsAdded.Any() || tagsRemoved.Any())
            {
                _logger.Debug("Updated tags for '{0}'. Added: {1}, Removed: {2}", game.Title, tagsAdded.Count, tagsRemoved.Count);

                return true;
            }

            _logger.Debug("Tags not updated for '{0}'", game.Title);

            return false;
        }

        public List<Game> GetGamesByFileId(int fileId)
        {
            return _gameRepository.GetGamesByFileId(fileId);
        }

        public List<Game> GetGamesByCollectionIgdbId(int collectionId)
        {
            return _gameRepository.GetGamesByCollectionIgdbId(collectionId);
        }

        public List<Game> GetGamesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored)
        {
            var games = _gameRepository.GamesBetweenDates(start.ToUniversalTime(), end.ToUniversalTime(), includeUnmonitored);

            return games;
        }

        public PagingSpec<Game> GamesWithoutFiles(PagingSpec<Game> pagingSpec)
        {
            var gameResult = _gameRepository.GamesWithoutFiles(pagingSpec);

            return gameResult;
        }

        public bool GameExists(Game game)
        {
            Game result = null;

            if (game.IgdbId != 0)
            {
                result = _gameRepository.FindByIgdbId(game.IgdbId);
                if (result != null)
                {
                    return true;
                }
            }

            if (game.ImdbId.IsNotNullOrWhiteSpace())
            {
                result = _gameRepository.FindByImdbId(game.ImdbId);
                if (result != null)
                {
                    return true;
                }
            }

            if (game.Title.IsNotNullOrWhiteSpace())
            {
                if (game.Year > 1850)
                {
                    result = FindByTitle(game.Title.CleanGameTitle(), game.Year);
                    if (result != null)
                    {
                        return true;
                    }
                }
                else
                {
                    result = FindByTitle(game.Title.CleanGameTitle());
                    if (result != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<int> GetRecommendedIgdbIds()
        {
            return _gameRepository.GetRecommendations();
        }

        public bool ExistsByMetadataId(int metadataId)
        {
            return _gameRepository.ExistsByMetadataId(metadataId);
        }

        public HashSet<int> AllGameWithCollectionsIgdbIds()
        {
            return _gameRepository.AllGameWithCollectionsIgdbIds();
        }

        private Game ReturnSingleGameOrThrow(List<Game> games)
        {
            if (games.Count == 0)
            {
                return null;
            }

            if (games.Count == 1)
            {
                return games.First();
            }

            throw new MultipleGamesFoundException(games, "Expected one game, but found {0}. Matching games: {1}", games.Count, string.Join(",", games));
        }

        public void Handle(GameFileAddedEvent message)
        {
            var game = message.GameFile.Game;
            game.GameFileId = message.GameFile.Id;
            _gameRepository.Update(game);

            // _gameRepository.SetFileId(message.GameFile.Id, message.GameFile.Game.Value.Id);
            _logger.Info("Assigning file [{0}] to game [{1}]", message.GameFile.RelativePath, message.GameFile.Game);
        }

        public void Handle(GameFileDeletedEvent message)
        {
            foreach (var game in GetGamesByFileId(message.GameFile.Id))
            {
                _logger.Debug("Detaching game {0} from file.", game.Id);
                game.GameFileId = 0;

                if (message.Reason != DeleteMediaFileReason.Upgrade && _configService.AutoUnmonitorPreviouslyDownloadedGames)
                {
                    game.Monitored = false;
                }

                UpdateGame(game);
            }
        }
    }
}
