using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Games
{
    public class RefreshGameService : IExecute<RefreshGameCommand>
    {
        private readonly IProvideGameInfo _gameInfo;
        private readonly IGameService _gameService;
        private readonly IAddGameCollectionService _gameCollectionService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly IRootFolderService _folderService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly IAlternativeTitleService _alternativeTitleService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDiskScanService _diskScanService;
        private readonly ICheckIfGameShouldBeRefreshed _checkIfGameShouldBeRefreshed;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public RefreshGameService(IProvideGameInfo gameInfo,
                                    IGameService gameService,
                                    IAddGameCollectionService gameCollectionService,
                                    IGameMetadataService gameMetadataService,
                                    IRootFolderService folderService,
                                    IGameTranslationService gameTranslationService,
                                    IAlternativeTitleService alternativeTitleService,
                                    IEventAggregator eventAggregator,
                                    IDiskScanService diskScanService,
                                    ICheckIfGameShouldBeRefreshed checkIfGameShouldBeRefreshed,
                                    IConfigService configService,
                                    Logger logger)
        {
            _gameInfo = gameInfo;
            _gameService = gameService;
            _gameCollectionService = gameCollectionService;
            _gameMetadataService = gameMetadataService;
            _folderService = folderService;
            _gameTranslationService = gameTranslationService;
            _alternativeTitleService = alternativeTitleService;
            _eventAggregator = eventAggregator;
            _diskScanService = diskScanService;
            _checkIfGameShouldBeRefreshed = checkIfGameShouldBeRefreshed;
            _configService = configService;
            _logger = logger;
        }

        private Game RefreshGameInfo(int gameId)
        {
            // Get the game before updating, that way any changes made to the game after the refresh started,
            // but before this game was refreshed won't be lost.
            var game = _gameService.GetGame(gameId);
            var gameMetadata = _gameMetadataService.Get(game.GameMetadataId);

            _logger.ProgressInfo("Updating info for {0}", game.Title);

            GameMetadata gameInfo;

            try
            {
                // Use SteamAppId if IgdbId is not available (Steam-only games)
                if (game.IgdbId <= 0 && gameMetadata.SteamAppId > 0)
                {
                    gameInfo = _gameInfo.GetGameInfoBySteamAppId(gameMetadata.SteamAppId);

                    if (gameInfo == null)
                    {
                        _logger.Warn("Could not refresh Steam game {0} (SteamAppId: {1})", game.Title, gameMetadata.SteamAppId);
                        return game;
                    }
                }
                else
                {
                    gameInfo = _gameInfo.GetGameInfo(game.IgdbId);
                }
            }
            catch (GameNotFoundException)
            {
                if (gameMetadata.Status != GameStatusType.Deleted)
                {
                    gameMetadata.Status = GameStatusType.Deleted;
                    _gameMetadataService.Upsert(gameMetadata);
                    _logger.Debug("Game marked as deleted on IGDB for {0}", game.Title);
                    _eventAggregator.PublishEvent(new GameUpdatedEvent(game));
                }

                throw;
            }

            if (gameMetadata.IgdbId != gameInfo.IgdbId)
            {
                _logger.Warn("Game '{0}' (IGDB: {1}) was replaced with '{2}' (IGDB: {3}), because the original was a duplicate.", game.Title, game.IgdbId, gameInfo.Title, gameInfo.IgdbId);
                gameMetadata.IgdbId = gameInfo.IgdbId;
            }

            gameMetadata.Title = gameInfo.Title;
            gameMetadata.Overview = gameInfo.Overview;
            gameMetadata.Status = gameInfo.Status;
            gameMetadata.Images = gameInfo.Images;
            gameMetadata.CleanTitle = gameInfo.CleanTitle;
            gameMetadata.SortTitle = gameInfo.SortTitle;
            gameMetadata.LastInfoSync = DateTime.UtcNow;
            gameMetadata.Runtime = gameInfo.Runtime;
            gameMetadata.Ratings = gameInfo.Ratings;
            gameMetadata.Genres = gameInfo.Genres;
            gameMetadata.Keywords = gameInfo.Keywords;
            gameMetadata.Certification = gameInfo.Certification;
            gameMetadata.EarlyAccess = gameInfo.EarlyAccess;
            gameMetadata.Website = gameInfo.Website;

            gameMetadata.Year = gameInfo.Year;
            gameMetadata.SecondaryYear = gameInfo.SecondaryYear;
            gameMetadata.PhysicalRelease = gameInfo.PhysicalRelease;
            gameMetadata.DigitalRelease = gameInfo.DigitalRelease;
            gameMetadata.YouTubeTrailerId = gameInfo.YouTubeTrailerId;
            gameMetadata.Studio = gameInfo.Studio;
            gameMetadata.OriginalTitle = gameInfo.OriginalTitle;
            gameMetadata.CleanOriginalTitle = gameInfo.CleanOriginalTitle;
            gameMetadata.OriginalLanguage = gameInfo.OriginalLanguage;
            gameMetadata.Recommendations = gameInfo.Recommendations;
            gameMetadata.Popularity = gameInfo.Popularity;

            // Update parent game ID for DLC linking
            if (gameInfo.ParentGameId > 0)
            {
                gameMetadata.ParentGameId = gameInfo.ParentGameId;
            }

            // Update DLC IDs and references
            if (gameInfo.IgdbDlcIds != null && gameInfo.IgdbDlcIds.Any())
            {
                gameMetadata.IgdbDlcIds = gameInfo.IgdbDlcIds;
            }

            if (gameInfo.SteamDlcIds != null && gameInfo.SteamDlcIds.Any())
            {
                gameMetadata.SteamDlcIds = gameInfo.SteamDlcIds;
            }

            if (gameInfo.DlcReferences != null && gameInfo.DlcReferences.Any())
            {
                gameMetadata.DlcReferences = gameInfo.DlcReferences;
            }

            // add collection
            if (gameInfo.CollectionIgdbId > 0)
            {
                var newCollection = _gameCollectionService.AddGameCollection(new GameCollection
                {
                    IgdbId = gameInfo.CollectionIgdbId,
                    Title = gameInfo.CollectionTitle,
                    Monitored = game.AddOptions?.Monitor == MonitorTypes.GameAndCollection,
                    SearchOnAdd = game.AddOptions?.SearchForGame ?? false,
                    QualityProfileId = game.QualityProfileId,
                    MinimumAvailability = game.MinimumAvailability,
                    RootFolderPath = _folderService.GetBestRootFolderPath(game.Path).GetCleanPath(),
                    Tags = game.Tags
                });

                if (newCollection != null)
                {
                    gameMetadata.CollectionIgdbId = newCollection.IgdbId;
                    gameMetadata.CollectionTitle = newCollection.Title;
                }
            }
            else
            {
                gameMetadata.CollectionIgdbId = 0;
                gameMetadata.CollectionTitle = null;
            }

            gameMetadata.AlternativeTitles = _alternativeTitleService.UpdateTitles(gameInfo.AlternativeTitles ?? new List<AlternativeTitle>(), gameMetadata);

            _gameTranslationService.UpdateTranslations(gameInfo.Translations ?? new List<GameTranslation>(), gameMetadata);

            _gameMetadataService.Upsert(gameMetadata);

            game.GameMetadata = gameMetadata;

            _logger.Debug("Finished game metadata refresh for {0}", gameMetadata.Title);
            _eventAggregator.PublishEvent(new GameUpdatedEvent(game));

            return game;
        }

        private void RescanGame(Game game, bool isNew, CommandTrigger trigger)
        {
            var rescanAfterRefresh = _configService.RescanAfterRefresh;

            if (isNew)
            {
                _logger.Trace("Forcing rescan of {0}. Reason: New game", game);
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.Never)
            {
                _logger.Trace("Skipping rescan of {0}. Reason: Never rescan after refresh", game);
                _eventAggregator.PublishEvent(new GameScanSkippedEvent(game, GameScanSkippedReason.NeverRescanAfterRefresh));

                return;
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.AfterManual && trigger != CommandTrigger.Manual)
            {
                _logger.Trace("Skipping rescan of {0}. Reason: Not after automatic scans", game);
                _eventAggregator.PublishEvent(new GameScanSkippedEvent(game, GameScanSkippedReason.RescanAfterManualRefreshOnly));

                return;
            }

            try
            {
                _diskScanService.Scan(game);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Couldn't rescan game {0}", game);
            }
        }

        private void UpdateTags(Game game, bool isNew)
        {
            if (isNew)
            {
                _logger.Trace("Skipping tag update for {0}. Reason: New game", game);
                return;
            }

            var tagsUpdated = _gameService.UpdateTags(game);

            if (tagsUpdated)
            {
                _gameService.UpdateGame(game);
            }
        }

        public void Execute(RefreshGameCommand message)
        {
            var trigger = message.Trigger;
            var isNew = message.IsNewGame;
            _eventAggregator.PublishEvent(new GameRefreshStartingEvent(message.Trigger == CommandTrigger.Manual));

            if (message.GameIds.Any())
            {
                foreach (var gameId in message.GameIds)
                {
                    var game = _gameService.GetGame(gameId);

                    try
                    {
                        game = RefreshGameInfo(gameId);
                        UpdateTags(game, isNew);
                        RescanGame(game, isNew, trigger);
                    }
                    catch (GameNotFoundException)
                    {
                        _logger.Error("Game '{0}' (IGDB {1}) was not found, it may have been removed from The Game Database.", game.Title, game.IgdbId);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Couldn't refresh info for {0}", game);
                        UpdateTags(game, isNew);
                        RescanGame(game, isNew, trigger);
                        throw;
                    }
                }
            }
            else
            {
                // TODO refresh all gamemetadata here, even if not used by a Game
                var allGames = _gameService.GetAllGames();

                var updatedIgdbGames = new HashSet<int>();

                if (message.LastStartTime.HasValue && message.LastStartTime.Value.AddDays(14) > DateTime.UtcNow)
                {
                    updatedIgdbGames = _gameInfo.GetChangedGames(message.LastStartTime.Value);
                }

                foreach (var game in allGames)
                {
                    var gameLocal = game;
                    if ((updatedIgdbGames.Count == 0 && _checkIfGameShouldBeRefreshed.ShouldRefresh(game.GameMetadata)) || updatedIgdbGames.Contains(game.IgdbId) || message.Trigger == CommandTrigger.Manual)
                    {
                        try
                        {
                            gameLocal = RefreshGameInfo(gameLocal.Id);
                        }
                        catch (GameNotFoundException)
                        {
                            _logger.Error("Game '{0}' (IGDB {1}) was not found, it may have been removed from The Game Database.", gameLocal.Title, gameLocal.IgdbId);
                            continue;
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Couldn't refresh info for {0}", gameLocal);
                        }

                        UpdateTags(game, false);
                        RescanGame(gameLocal, false, trigger);
                    }
                    else
                    {
                        _logger.Debug("Skipping refresh of game: {0}", gameLocal.Title);
                        UpdateTags(game, false);
                        RescanGame(gameLocal, false, trigger);
                    }
                }
            }

            _eventAggregator.PublishEvent(new GameRefreshCompleteEvent());
        }
    }
}
