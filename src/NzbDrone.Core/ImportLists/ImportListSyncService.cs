using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncService : IExecute<ImportListSyncCommand>
    {
        private readonly Logger _logger;
        private readonly IImportListFactory _importListFactory;
        private readonly IFetchAndParseImportList _listFetcherAndParser;
        private readonly IGameService _gameService;
        private readonly IAddGameService _addGameService;
        private readonly IConfigService _configService;
        private readonly IImportListExclusionService _listExclusionService;
        private readonly IImportListGameService _listGameService;

        public ImportListSyncService(IImportListFactory importListFactory,
                                      IFetchAndParseImportList listFetcherAndParser,
                                      IGameService gameService,
                                      IAddGameService addGameService,
                                      IConfigService configService,
                                      IImportListExclusionService listExclusionService,
                                      IImportListGameService listGameService,
                                      Logger logger)
        {
            _importListFactory = importListFactory;
            _listFetcherAndParser = listFetcherAndParser;
            _gameService = gameService;
            _addGameService = addGameService;
            _listExclusionService = listExclusionService;
            _listGameService = listGameService;
            _logger = logger;
            _configService = configService;
        }

        private void SyncAll()
        {
            if (_importListFactory.Enabled().Empty())
            {
                _logger.Debug("No enabled import lists, skipping sync and cleaning");

                return;
            }

            var listItemsResult = _listFetcherAndParser.Fetch();

            if (listItemsResult.SyncedLists == 0)
            {
                return;
            }

            if (!listItemsResult.AnyFailure)
            {
                CleanLibrary();
            }

            ProcessListItems(listItemsResult);
        }

        private void SyncList(ImportListDefinition definition)
        {
            _logger.ProgressInfo("Starting Import List Refresh for List {0}", definition.Name);

            var listItemsResult = _listFetcherAndParser.FetchSingleList(definition);

            ProcessListItems(listItemsResult);
        }

        private void ProcessGameReport(ImportListDefinition importList, ImportListGame report, List<ImportListExclusion> listExclusions, List<int> dbGames, List<Game> gamesToAdd)
        {
            if (report.IgdbId == 0 || !importList.EnableAuto)
            {
                return;
            }

            // Check to see if game in DB
            if (dbGames.Contains(report.IgdbId))
            {
                _logger.Debug("{0} [{1}] Rejected, Game Exists in DB", report.IgdbId, report.Title);
                return;
            }

            // Check to see if game excluded
            var excludedGame = listExclusions.SingleOrDefault(s => s.IgdbId == report.IgdbId);

            if (excludedGame != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.IgdbId, report.Title);
                return;
            }

            // Append Artist if not already in DB or already on add list
            if (gamesToAdd.All(s => s.IgdbId != report.IgdbId))
            {
                var monitorType = importList.Monitor;

                gamesToAdd.Add(new Game
                {
                    Monitored = monitorType != MonitorTypes.None,
                    RootFolderPath = importList.RootFolderPath,
                    QualityProfileId = importList.QualityProfileId,
                    MinimumAvailability = importList.MinimumAvailability,
                    Tags = importList.Tags,
                    IgdbId = report.IgdbId,
                    Title = report.Title,
                    Year = report.Year,
                    ImdbId = report.ImdbId,
                    AddOptions = new AddGameOptions
                    {
                        SearchForGame = monitorType != MonitorTypes.None && importList.SearchOnAdd,
                        Monitor = monitorType,
                        AddMethod = AddGameMethod.List
                    }
                });
            }
        }

        private void ProcessListItems(ImportListFetchResult listFetchResult)
        {
            listFetchResult.Games = listFetchResult.Games.DistinctBy(x =>
            {
                if (x.IgdbId != 0)
                {
                    return x.IgdbId.ToString();
                }

                if (x.ImdbId.IsNotNullOrWhiteSpace())
                {
                    return x.ImdbId;
                }

                return x.Title;
            }).ToList();

            var listedGames = listFetchResult.Games.ToList();

            var importExclusions = _listExclusionService.All();
            var dbGames = _gameService.AllGameIgdbIds();
            var gamesToAdd = new List<Game>();

            var groupedGames = listedGames.GroupBy(x => x.ListId);

            foreach (var list in groupedGames)
            {
                var importList = _importListFactory.Get(list.Key);

                foreach (var game in list)
                {
                    if (game.IgdbId != 0)
                    {
                        ProcessGameReport(importList, game, importExclusions, dbGames, gamesToAdd);
                    }
                }
            }

            if (gamesToAdd.Any())
            {
                _logger.ProgressInfo("Adding {0} games from your auto enabled lists to library", gamesToAdd.Count);
                _addGameService.AddGames(gamesToAdd, true);
            }
        }

        public void Execute(ImportListSyncCommand message)
        {
            if (message.DefinitionId.HasValue)
            {
                SyncList(_importListFactory.Get(message.DefinitionId.Value));
            }
            else
            {
                SyncAll();
            }
        }

        private void CleanLibrary()
        {
            if (_configService.ListSyncLevel == "disabled")
            {
                return;
            }

            var listGames = _listGameService.GetAllListGames();

            // TODO use AllGameIgdbIds here?
            var gamesInLibrary = _gameService.GetAllGames();

            var gamesToUpdate = new List<Game>();

            foreach (var game in gamesInLibrary)
            {
                var gameExists = listGames.Any(c =>
                    c.IgdbId == game.IgdbId ||
                    (c.ImdbId.IsNotNullOrWhiteSpace() && game.ImdbId.IsNotNullOrWhiteSpace() && c.ImdbId == game.ImdbId));

                if (!gameExists)
                {
                    switch (_configService.ListSyncLevel)
                    {
                        case "logOnly":
                            _logger.Info("{0} was in your library, but not found in your lists --> You might want to unmonitor or remove it", game);
                            break;
                        case "keepAndUnmonitor":
                            _logger.Info("{0} was in your library, but not found in your lists --> Keeping in library but Unmonitoring it", game);
                            game.Monitored = false;
                            gamesToUpdate.Add(game);
                            break;
                        case "removeAndKeep":
                            _logger.Info("{0} was in your library, but not found in your lists --> Removing from library (keeping files)", game);
                            _gameService.DeleteGame(game.Id, false);
                            break;
                        case "removeAndDelete":
                            _logger.Info("{0} was in your library, but not found in your lists --> Removing from library and deleting files", game);
                            _gameService.DeleteGame(game.Id, true);
                            break;
                    }
                }
            }

            _gameService.UpdateGame(gamesToUpdate, true);
        }
    }
}
