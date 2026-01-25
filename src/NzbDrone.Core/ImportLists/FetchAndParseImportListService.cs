using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.ImportLists
{
    public interface IFetchAndParseImportList
    {
        ImportListFetchResult Fetch();
        ImportListFetchResult FetchSingleList(ImportListDefinition definition);
    }

    public class FetchAndParseImportListService : IFetchAndParseImportList
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListStatusService _importListStatusService;
        private readonly IImportListGameService _listGameService;
        private readonly ISearchForNewGame _gameSearch;
        private readonly IProvideGameInfo _gameInfoService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly Logger _logger;

        public FetchAndParseImportListService(IImportListFactory importListFactory,
                                              IImportListStatusService importListStatusService,
                                              IImportListGameService listGameService,
                                              ISearchForNewGame gameSearch,
                                              IProvideGameInfo gameInfoService,
                                              IGameMetadataService gameMetadataService,
                                              Logger logger)
        {
            _importListFactory = importListFactory;
            _importListStatusService = importListStatusService;
            _listGameService = listGameService;
            _gameSearch = gameSearch;
            _gameInfoService = gameInfoService;
            _gameMetadataService = gameMetadataService;
            _logger = logger;
        }

        public ImportListFetchResult Fetch()
        {
            var result = new ImportListFetchResult();

            var importLists = _importListFactory.Enabled();

            if (!importLists.Any())
            {
                _logger.Debug("No available import lists. check your configuration.");
                return result;
            }

            _logger.Debug("Available import lists {0}", importLists.Count);

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            foreach (var importList in importLists)
            {
                var importListLocal = importList;
                var importListStatus = _importListStatusService.GetLastSyncListInfo(importListLocal.Definition.Id);

                if (importListStatus.HasValue)
                {
                    var importListNextSync = importListStatus.Value + importListLocal.MinRefreshInterval;

                    if (DateTime.UtcNow < importListNextSync)
                    {
                        _logger.Trace("Skipping refresh of Import List {0} ({1}) due to minimum refresh interval. Next Sync after {2}", importList.Name, importListLocal.Definition.Name, importListNextSync);

                        continue;
                    }
                }

                _logger.ProgressInfo("Syncing Games for Import List {0} ({1})", importList.Name, importListLocal.Definition.Name);

                var blockedLists = _importListStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId, v => v);

                if (blockedLists.TryGetValue(importList.Definition.Id, out var blockedListStatus))
                {
                    _logger.Debug("Temporarily ignoring Import List {0} ({1}) till {2} due to recent failures.", importList.Name, importListLocal.Definition.Name, blockedListStatus.DisabledTill.Value.ToLocalTime());
                    result.AnyFailure |= true; // Ensure we don't clean if a list is down
                    continue;
                }

                var task = taskFactory.StartNew(() =>
                {
                    try
                    {
                        var importListReports = importListLocal.Fetch();

                        lock (result)
                        {
                            _logger.Debug("Found {0} from Import List {1} ({2})", importListReports.Games.Count, importList.Name, importListLocal.Definition.Name);

                            if (!importListReports.AnyFailure)
                            {
                                var alreadyMapped = result.Games.Where(x => importListReports.Games.Any(r => r.IgdbId == x.IgdbId));
                                var listGames = MapGameReports(importListReports.Games.Where(x => result.Games.All(r => r.IgdbId != x.IgdbId))).Where(x => x.IgdbId > 0).ToList();

                                listGames.AddRange(alreadyMapped);
                                listGames = listGames.DistinctBy(x => x.IgdbId).ToList();
                                listGames.ForEach(m => m.ListId = importList.Definition.Id);

                                result.Games.AddRange(listGames);
                                _listGameService.SyncGamesForList(listGames, importList.Definition.Id);
                            }

                            result.AnyFailure |= importListReports.AnyFailure;
                            result.SyncedLists++;

                            _importListStatusService.UpdateListSyncStatus(importList.Definition.Id);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Error during Import List Sync of {0} ({1})", importList.Name, importListLocal.Definition.Name);
                    }
                }).LogExceptions();

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());

            result.Games = result.Games.DistinctBy(r => new { r.IgdbId, r.Title }).ToList();

            _logger.Debug("Found {0} total reports from {1} lists", result.Games.Count, result.SyncedLists);

            return result;
        }

        public ImportListFetchResult FetchSingleList(ImportListDefinition definition)
        {
            var result = new ImportListFetchResult();

            var importList = _importListFactory.GetInstance(definition);

            if (importList == null || !definition.Enable)
            {
                _logger.Debug("Import List {0} ({1}) is not enabled, skipping.", importList.Name, importList.Definition.Name);
                return result;
            }

            var importListLocal = importList;

            try
            {
                var importListReports = importListLocal.Fetch();

                lock (result)
                {
                    _logger.Debug("Found {0} games from {1} ({2})", importListReports.Games.Count, importList.Name, importListLocal.Definition.Name);

                    if (!importListReports.AnyFailure)
                    {
                        var listGames = MapGameReports(importListReports.Games)
                            .Where(x => x.IgdbId > 0)
                            .DistinctBy(x => x.IgdbId)
                            .ToList();

                        listGames.ForEach(m => m.ListId = importList.Definition.Id);

                        result.Games.AddRange(listGames);
                        _listGameService.SyncGamesForList(listGames, importList.Definition.Id);
                    }

                    result.AnyFailure |= importListReports.AnyFailure;

                    _importListStatusService.UpdateListSyncStatus(importList.Definition.Id);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during Import List Sync of {0} ({1})", importList.Name, importListLocal.Definition.Name);
            }

            result.Games = result.Games.DistinctBy(r => new { r.IgdbId, r.Title }).ToList();

            _logger.Debug("Found {0} games from {1} ({2})", result.Games.Count, importList.Name, importListLocal.Definition.Name);

            return result;
        }

        private List<ImportListGame> MapGameReports(IEnumerable<ImportListGame> reports)
        {
            var mappedGames = reports.Select(m => _gameSearch.MapGameToIgdbGame(new GameMetadata { Title = m.Title, IgdbId = m.IgdbId, Year = m.Year }))
                .Where(x => x != null)
                .DistinctBy(x => x.IgdbId)
                .ToList();

            _gameMetadataService.UpsertMany(mappedGames);

            var mappedListGames = new List<ImportListGame>();

            foreach (var gameMeta in mappedGames)
            {
                var mappedListGame = new ImportListGame();

                if (gameMeta != null)
                {
                    mappedListGame.GameMetadata = gameMeta;
                    mappedListGame.GameMetadataId = gameMeta.Id;
                }

                mappedListGames.Add(mappedListGame);
            }

            return mappedListGames;
        }
    }
}
