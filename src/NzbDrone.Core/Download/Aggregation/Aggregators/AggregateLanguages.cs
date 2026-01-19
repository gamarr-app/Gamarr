using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Aggregation.Aggregators
{
    public class AggregateLanguages : IAggregateRemoteGame
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;

        public AggregateLanguages(IIndexerFactory indexerFactory,
                                  Logger logger)
        {
            _indexerFactory = indexerFactory;
            _logger = logger;
        }

        public RemoteGame Aggregate(RemoteGame remoteGame)
        {
            var parsedGameInfo = remoteGame.ParsedGameInfo;
            var releaseInfo = remoteGame.Release;
            var languages = parsedGameInfo.Languages;
            var game = remoteGame.Game;
            var releaseTokens = parsedGameInfo.SimpleReleaseTitle ?? parsedGameInfo.ReleaseTitle;
            var normalizedReleaseTokens = Parser.Parser.NormalizeGameTitle(releaseTokens);
            var languagesToRemove = new List<Language>();

            if (game == null)
            {
                _logger.Debug("Unable to aggregate languages, using parsed values: {0}", string.Join(", ", languages.ToList()));

                remoteGame.Languages = releaseInfo != null && releaseInfo.Languages.Any() ? releaseInfo.Languages : languages;

                return remoteGame;
            }

            if (releaseInfo != null && releaseInfo.Languages.Any())
            {
                _logger.Debug("Languages provided by indexer, using release values: {0}", string.Join(", ", releaseInfo.Languages));

                // Use languages from release (given by indexer or user) if available
                languages = releaseInfo.Languages;
            }
            else
            {
                var gameTitleLanguage = LanguageParser.ParseLanguages(game.Title);

                if (!gameTitleLanguage.Contains(Language.Unknown))
                {
                    var normalizedEpisodeTitle = Parser.Parser.NormalizeGameTitle(game.Title);
                    var gameTitleIndex = normalizedReleaseTokens.IndexOf(normalizedEpisodeTitle, StringComparison.CurrentCultureIgnoreCase);

                    if (gameTitleIndex >= 0)
                    {
                        releaseTokens = releaseTokens.Remove(gameTitleIndex, normalizedEpisodeTitle.Length);
                        languagesToRemove.AddRange(gameTitleLanguage);
                    }
                }

                // Remove any languages still in the title that would normally be removed
                languagesToRemove = languagesToRemove.Except(LanguageParser.ParseLanguages(releaseTokens)).ToList();

                // Remove all languages that aren't part of the updated releaseTokens
                languages = languages.Except(languagesToRemove).ToList();
            }

            if (releaseInfo?.Title?.IsNotNullOrWhiteSpace() == true)
            {
                IndexerDefinition indexer = null;

                if (releaseInfo is { IndexerId: > 0 })
                {
                    indexer = _indexerFactory.Find(releaseInfo.IndexerId);
                }

                if (indexer == null && releaseInfo.Indexer?.IsNotNullOrWhiteSpace() == true)
                {
                    indexer = _indexerFactory.FindByName(releaseInfo.Indexer);
                }

                if (indexer?.Settings is IIndexerSettings settings && settings.MultiLanguages.Any() && Parser.Parser.HasMultipleLanguages(releaseInfo.Title))
                {
                    // Use indexer setting for Multi-languages
                    if (languages.Count == 0 || (languages.Count == 1 && languages.First() == Language.Unknown))
                    {
                        languages = settings.MultiLanguages.Select(i => (Language)i).ToList();
                    }
                    else
                    {
                        languages.AddRange(settings.MultiLanguages.Select(i => (Language)i).Except(languages).ToList());
                    }
                }
            }

            // Use game language as fallback if we couldn't parse a language
            if (languages.Count == 0 || (languages.Count == 1 && languages.First() == Language.Unknown))
            {
                languages = new List<Language> { game.GameMetadata.Value.OriginalLanguage };
                _logger.Debug("Language couldn't be parsed from release, fallback to game original language: {0}", game.GameMetadata.Value.OriginalLanguage.Name);
            }

            if (languages.Contains(Language.Original))
            {
                languages.Remove(Language.Original);

                if (!languages.Contains(game.GameMetadata.Value.OriginalLanguage))
                {
                    languages.Add(game.GameMetadata.Value.OriginalLanguage);
                }
                else
                {
                    languages.Add(Language.Unknown);
                }
            }

            _logger.Debug("Selected languages: {0}", string.Join(", ", languages.ToList()));

            remoteGame.Languages = languages;

            return remoteGame;
        }
    }
}
