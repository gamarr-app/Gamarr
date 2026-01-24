using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForReleases
    {
        Task<List<DownloadDecision>> GameSearch(int gameId, bool userInvokedSearch, bool interactiveSearch);
        Task<List<DownloadDecision>> GameSearch(Game game, bool userInvokedSearch, bool interactiveSearch);
    }

    public class ReleaseSearchService : ISearchForReleases
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly IMakeDownloadDecision _makeDownloadDecision;
        private readonly IGameService _gameService;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly Logger _logger;

        public ReleaseSearchService(IIndexerFactory indexerFactory,
                                IMakeDownloadDecision makeDownloadDecision,
                                IGameService gameService,
                                IGameTranslationService gameTranslationService,
                                IQualityProfileService qualityProfileService,
                                Logger logger)
        {
            _indexerFactory = indexerFactory;
            _makeDownloadDecision = makeDownloadDecision;
            _gameService = gameService;
            _gameTranslationService = gameTranslationService;
            _qualityProfileService = qualityProfileService;
            _logger = logger;
        }

        public async Task<List<DownloadDecision>> GameSearch(int gameId, bool userInvokedSearch, bool interactiveSearch)
        {
            var game = _gameService.GetGame(gameId);
            game.GameMetadata.Value.Translations = _gameTranslationService.GetAllTranslationsForGameMetadata(game.GameMetadataId);

            return await GameSearch(game, userInvokedSearch, interactiveSearch);
        }

        public async Task<List<DownloadDecision>> GameSearch(Game game, bool userInvokedSearch, bool interactiveSearch)
        {
            var downloadDecisions = new List<DownloadDecision>();

            var searchSpec = Get<GameSearchCriteria>(game, userInvokedSearch, interactiveSearch);

            var decisions = await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
            downloadDecisions.AddRange(decisions);

            return DeDupeDecisions(downloadDecisions);
        }

        // Matches common update/patch suffixes appended by metadata sources (e.g. RAWG)
        // Examples: "- Thank You Update", "- Patch 1.5.0", "- v1.2.3", "- Update 2", "- Hotfix"
        private static readonly Regex UpdateSuffixRegex = new Regex(
            @"\s+-\s+(?:(?:v\d|patch|update|hotfix|build)\b.*|(?:\w+\s+)*update)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private TSpec Get<TSpec>(Game game, bool userInvokedSearch, bool interactiveSearch)
            where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec
            {
                Game = game,
                UserInvokedSearch = userInvokedSearch,
                InteractiveSearch = interactiveSearch
            };

            var wantedLanguages = _qualityProfileService.GetAcceptableLanguages(game.QualityProfileId);
            var translations = _gameTranslationService.GetAllTranslationsForGameMetadata(game.GameMetadataId);

            var queryTranslations = new List<string>
            {
                game.GameMetadata.Value.Title,
                game.GameMetadata.Value.OriginalTitle
            };

            // Add Translation of wanted languages to search query
            foreach (var translation in translations.Where(a => wantedLanguages.Contains(a.Language)))
            {
                queryTranslations.Add(translation.Title);
            }

            // Add base titles with update/patch suffixes stripped (indexers rarely include these)
            var baseTitles = queryTranslations
                .Where(t => t.IsNotNullOrWhiteSpace())
                .Select(t => UpdateSuffixRegex.Replace(t, string.Empty))
                .Where(t => t.IsNotNullOrWhiteSpace());
            queryTranslations.AddRange(baseTitles);

            spec.SceneTitles = queryTranslations.Where(t => t.IsNotNullOrWhiteSpace()).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();

            return spec;
        }

        private async Task<List<DownloadDecision>> Dispatch(Func<IIndexer, Task<IList<ReleaseInfo>>> searchAction, SearchCriteriaBase criteriaBase)
        {
            var indexers = criteriaBase.InteractiveSearch ?
                _indexerFactory.InteractiveSearchEnabled() :
                _indexerFactory.AutomaticSearchEnabled();

            // Filter indexers to untagged indexers and indexers with intersecting tags
            indexers = indexers.Where(i => i.Definition.Tags.Empty() || i.Definition.Tags.Intersect(criteriaBase.Game.Tags).Any()).ToList();

            _logger.ProgressInfo("Searching indexers for {0}. {1} active indexers", criteriaBase, indexers.Count);

            var tasks = indexers.Select(indexer => DispatchIndexer(searchAction, indexer, criteriaBase));

            var batch = await Task.WhenAll(tasks);

            var reports = batch.SelectMany(x => x).ToList();

            _logger.ProgressDebug("Total of {0} reports were found for {1} from {2} indexers", reports.Count, criteriaBase, indexers.Count);

            // Update the last search time for game if at least 1 indexer was searched.
            if (indexers.Any())
            {
                var lastSearchTime = DateTime.UtcNow;
                _logger.Debug("Setting last search time to: {0}", lastSearchTime);

                criteriaBase.Game.LastSearchTime = lastSearchTime;
                _gameService.UpdateLastSearchTime(criteriaBase.Game);
            }

            return _makeDownloadDecision.GetSearchDecision(reports, criteriaBase).ToList();
        }

        private async Task<IList<ReleaseInfo>> DispatchIndexer(Func<IIndexer, Task<IList<ReleaseInfo>>> searchAction, IIndexer indexer, SearchCriteriaBase criteriaBase)
        {
            try
            {
                return await searchAction(indexer);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while searching for {0}", criteriaBase);
            }

            return Array.Empty<ReleaseInfo>();
        }

        private List<DownloadDecision> DeDupeDecisions(List<DownloadDecision> decisions)
        {
            // De-dupe reports by guid so duplicate results aren't returned. Pick the one with the least rejections and higher indexer priority.
            return decisions.GroupBy(d => d.RemoteGame.Release.Guid)
                .Select(d => d.OrderBy(v => v.Rejections.Count()).ThenBy(v => v.RemoteGame?.Release?.IndexerPriority ?? IndexerDefinition.DefaultPriority).First())
                .ToList();
        }
    }
}
