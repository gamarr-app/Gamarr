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

        private void ProcessGameReport(ImportListDefinition importList, ImportListGame report, ListItemIds listExclusions, HashSet<int> dbGameSteamIds, HashSet<int> dbGameIgdbIds, ListItemIds pendingGameIds, List<Game> gamesToAdd)
        {
            // Need either Steam App ID (primary) or IGDB ID (secondary) to proceed
            if ((report.SteamAppId == 0 && report.IgdbId == 0) || !importList.EnableAuto)
            {
                return;
            }

            // Check to see if game in DB by Steam App ID (primary) or IGDB ID (secondary)
            if ((report.SteamAppId > 0 && dbGameSteamIds.Contains(report.SteamAppId)) ||
                (report.IgdbId > 0 && dbGameIgdbIds.Contains(report.IgdbId)))
            {
                _logger.Debug("Steam:{0}/IGDB:{1} [{2}] Rejected, Game Exists in DB", report.SteamAppId, report.IgdbId, report.Title);
                return;
            }

            // Check to see if game excluded (by Steam App ID or IGDB ID)
            if (listExclusions.Contains(report.SteamAppId, report.IgdbId))
            {
                _logger.Debug("Steam:{0}/IGDB:{1} [{2}] Rejected due to list exclusion", report.SteamAppId, report.IgdbId, report.Title);
                return;
            }

            // Append Game if not already in DB or already on add list
            if (pendingGameIds.Contains(report.SteamAppId, report.IgdbId))
            {
                return;
            }

            var monitorType = importList.Monitor;

            gamesToAdd.Add(new Game
            {
                Monitored = monitorType != MonitorTypes.None,
                RootFolderPath = importList.RootFolderPath,
                QualityProfileId = importList.QualityProfileId,
                MinimumAvailability = importList.MinimumAvailability,

                // Copy the definition's tags: the definition instance is shared
                // across every item contributed by that list, so assigning it
                // directly would let one list's tags leak onto another's games.
                Tags = new HashSet<int>(importList.Tags),
                SteamAppId = report.SteamAppId,
                IgdbId = report.IgdbId,
                Title = report.Title,
                Year = report.Year,
                AddOptions = new AddGameOptions
                {
                    SearchForGame = monitorType != MonitorTypes.None && importList.SearchOnAdd,
                    Monitor = monitorType,
                    AddMethod = AddGameMethod.List
                }
            });

            pendingGameIds.Add(report.SteamAppId, report.IgdbId);
        }

        private void ProcessListItems(ImportListFetchResult listFetchResult)
        {
            // Deduplicate by Steam App ID (primary) then IGDB ID (secondary) then Title
            listFetchResult.Games = listFetchResult.Games.DistinctBy(x =>
            {
                // Primary identifier - Steam App ID
                if (x.SteamAppId != 0)
                {
                    return $"steam:{x.SteamAppId}";
                }

                // Secondary identifier - IGDB ID
                if (x.IgdbId != 0)
                {
                    return $"igdb:{x.IgdbId}";
                }

                return x.Title;
            }).ToList();

            var listedGames = listFetchResult.Games.ToList();

            var importExclusions = new ListItemIds(_listExclusionService.All().Select(e => (e.SteamAppId, e.IgdbId)));
            var dbGameSteamIds = _gameService.AllGameSteamAppIds().ToHashSet();
            var dbGameIgdbIds = _gameService.AllGameIgdbIds().ToHashSet();
            var pendingGameIds = new ListItemIds();
            var gamesToAdd = new List<Game>();

            var groupedGames = listedGames.GroupBy(x => x.ListId);

            foreach (var list in groupedGames)
            {
                var importList = _importListFactory.Get(list.Key);

                foreach (var game in list)
                {
                    // Process if we have either Steam App ID or IGDB ID
                    if (game.SteamAppId != 0 || game.IgdbId != 0)
                    {
                        ProcessGameReport(importList, game, importExclusions, dbGameSteamIds, dbGameIgdbIds, pendingGameIds, gamesToAdd);
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

            var listGames = new ListItemIds(_listGameService.GetAllListGames().Select(c => (c.SteamAppId, c.IgdbId)));

            var gamesInLibrary = _gameService.GetAllGames();

            var gamesToUpdate = new List<Game>();

            foreach (var game in gamesInLibrary)
            {
                // Check if game exists in lists by Steam App ID (primary) or IGDB ID (secondary)
                if (!listGames.Contains(game.SteamAppId, game.IgdbId))
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

        /// <summary>
        /// Hash-set wrapper over the identity ids of a set of list items (or
        /// exclusions), so membership tests are O(1) instead of a nested scan.
        /// Only positive ids are ever matched: 0 means "not set" for both
        /// Steam App ID (primary) and IGDB ID (secondary).
        /// </summary>
        private class ListItemIds
        {
            private readonly HashSet<int> _steamAppIds = new HashSet<int>();
            private readonly HashSet<int> _igdbIds = new HashSet<int>();

            public ListItemIds()
            {
            }

            public ListItemIds(IEnumerable<(int SteamAppId, int IgdbId)> items)
            {
                foreach (var item in items)
                {
                    Add(item.SteamAppId, item.IgdbId);
                }
            }

            public void Add(int steamAppId, int igdbId)
            {
                if (steamAppId > 0)
                {
                    _steamAppIds.Add(steamAppId);
                }

                if (igdbId > 0)
                {
                    _igdbIds.Add(igdbId);
                }
            }

            public bool Contains(int steamAppId, int igdbId)
            {
                return (steamAppId > 0 && _steamAppIds.Contains(steamAppId)) ||
                       (igdbId > 0 && _igdbIds.Contains(igdbId));
            }
        }
    }
}
