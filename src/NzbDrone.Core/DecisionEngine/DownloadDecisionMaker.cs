using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IMakeDownloadDecision
    {
        List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false);
        List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase);
    }

    public class DownloadDecisionMaker : IMakeDownloadDecision
    {
        private readonly IEnumerable<IDownloadDecisionEngineSpecification> _specifications;
        private readonly IParsingService _parsingService;
        private readonly IConfigService _configService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IRemoteGameAggregationService _aggregationService;
        private readonly Logger _logger;

        public DownloadDecisionMaker(IEnumerable<IDownloadDecisionEngineSpecification> specifications,
                                     IParsingService parsingService,
                                     IConfigService configService,
                                     ICustomFormatCalculationService formatCalculator,
                                     IRemoteGameAggregationService aggregationService,
                                     Logger logger)
        {
            _specifications = specifications;
            _parsingService = parsingService;
            _configService = configService;
            _formatCalculator = formatCalculator;
            _aggregationService = aggregationService;
            _logger = logger;
        }

        public List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false)
        {
            return GetDecisions(reports, pushedRelease).ToList();
        }

        public List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase)
        {
            return GetDecisions(reports, false, searchCriteriaBase).ToList();
        }

        private IEnumerable<DownloadDecision> GetDecisions(List<ReleaseInfo> reports, bool pushedRelease = false, SearchCriteriaBase searchCriteria = null)
        {
            if (reports.Any())
            {
                _logger.ProgressInfo("Processing {0} releases", reports.Count);
            }
            else
            {
                _logger.ProgressInfo("No results found");
            }

            var reportNumber = 1;

            foreach (var report in reports)
            {
                DownloadDecision decision = null;
                _logger.ProgressTrace("Processing release {0}/{1}", reportNumber, reports.Count);
                _logger.Debug("Processing release '{0}' from '{1}'", report.Title, report.Indexer);

                try
                {
                    var parsedGameInfo = Parser.Parser.ParseGameTitle(report.Title);

                    if (parsedGameInfo != null && !parsedGameInfo.PrimaryGameTitle.IsNullOrWhiteSpace())
                    {
                        // Pass Steam App ID as primary, IGDB ID as secondary
                        var remoteGame = _parsingService.Map(parsedGameInfo, report.SteamAppId, report.IgdbId, searchCriteria);
                        remoteGame.Release = report;

                        if (remoteGame.Game == null)
                        {
                            decision = new DownloadDecision(remoteGame, new DownloadRejection(DownloadRejectionReason.UnknownGame, pushedRelease ? "Unknown Game. Unable to match to existing game in Library using release title." : "Unknown Game. Unable to match to correct game using release title."));
                        }
                        else
                        {
                            _aggregationService.Augment(remoteGame);

                            remoteGame.CustomFormats = _formatCalculator.ParseCustomFormat(remoteGame, remoteGame.Release.Size);
                            remoteGame.CustomFormatScore = remoteGame?.Game?.QualityProfile?.CalculateCustomFormatScore(remoteGame.CustomFormats) ?? 0;

                            _logger.Trace("Custom Format Score of '{0}' [{1}] calculated for '{2}'", remoteGame.CustomFormatScore, remoteGame.CustomFormats?.ConcatToString(), report.Title);

                            remoteGame.DownloadAllowed = remoteGame.Game != null;
                            decision = GetDecisionForReport(remoteGame, searchCriteria);
                        }
                    }

                    if (searchCriteria != null)
                    {
                        if (parsedGameInfo == null)
                        {
                            parsedGameInfo = new ParsedGameInfo
                            {
                                Languages = LanguageParser.ParseLanguages(report.Title),
                                Quality = QualityParser.ParseQuality(report.Title)
                            };
                        }

                        if (parsedGameInfo.PrimaryGameTitle.IsNullOrWhiteSpace())
                        {
                            var remoteGame = new RemoteGame
                            {
                                Release = report,
                                ParsedGameInfo = parsedGameInfo,
                                Languages = parsedGameInfo.Languages
                            };

                            decision = new DownloadDecision(remoteGame, new DownloadRejection(DownloadRejectionReason.UnableToParse, "Unable to parse release"));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't process release.");

                    var remoteGame = new RemoteGame { Release = report };
                    decision = new DownloadDecision(remoteGame, new DownloadRejection(DownloadRejectionReason.Error, "Unexpected error processing release"));
                }

                reportNumber++;

                if (decision != null)
                {
                    var source = pushedRelease ? ReleaseSourceType.ReleasePush : ReleaseSourceType.Rss;

                    if (searchCriteria != null)
                    {
                        if (searchCriteria.InteractiveSearch)
                        {
                            source = ReleaseSourceType.InteractiveSearch;
                        }
                        else if (searchCriteria.UserInvokedSearch)
                        {
                            source = ReleaseSourceType.UserInvokedSearch;
                        }
                        else
                        {
                            source = ReleaseSourceType.Search;
                        }
                    }

                    decision.RemoteGame.ReleaseSource = source;

                    if (decision.Rejections.Any())
                    {
                        _logger.Debug("Release '{0}' from '{1}' rejected for the following reasons: {2}", report.Title, report.Indexer, string.Join(", ", decision.Rejections));
                    }
                    else
                    {
                        _logger.Debug("Release '{0}' from '{1}' accepted", report.Title, report.Indexer);
                    }

                    yield return decision;
                }
            }
        }

        private DownloadDecision GetDecisionForReport(RemoteGame remoteGame, SearchCriteriaBase searchCriteria = null)
        {
            var reasons = Array.Empty<DownloadRejection>();

            foreach (var specifications in _specifications.GroupBy(v => v.Priority).OrderBy(v => v.Key))
            {
                reasons = specifications.Select(c => EvaluateSpec(c, remoteGame, searchCriteria))
                                        .Where(c => c != null)
                                        .ToArray();

                if (reasons.Any())
                {
                    break;
                }
            }

            return new DownloadDecision(remoteGame, reasons.ToArray());
        }

        private DownloadRejection EvaluateSpec(IDownloadDecisionEngineSpecification spec, RemoteGame remoteGame, SearchCriteriaBase searchCriteriaBase = null)
        {
            try
            {
                var result = spec.IsSatisfiedBy(remoteGame, searchCriteriaBase);

                if (!result.Accepted)
                {
                    return new DownloadRejection(result.Reason, result.Message, spec.Type);
                }
            }
            catch (NotImplementedException)
            {
                _logger.Trace("Spec " + spec.GetType().Name + " does not care about games.");
            }
            catch (Exception e)
            {
                e.Data.Add("report", remoteGame.Release.ToJson());
                e.Data.Add("parsed", remoteGame.ParsedGameInfo.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on {0}, with spec: {1}", remoteGame.Release.Title, spec.GetType().Name);
                return new DownloadRejection(DownloadRejectionReason.DecisionError, $"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }
    }
}
