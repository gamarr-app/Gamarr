using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatCalculationService
    {
        List<CustomFormat> ParseCustomFormat(RemoteGame remoteGame, long size);
        List<CustomFormat> ParseCustomFormat(GameFile gameFile, Game game);
        List<CustomFormat> ParseCustomFormat(GameFile gameFile);
        List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Game game);
        List<CustomFormat> ParseCustomFormat(GameHistory history, Game game);
        List<CustomFormat> ParseCustomFormat(LocalGame localGame);
    }

    public class CustomFormatCalculationService : ICustomFormatCalculationService
    {
        private readonly ICustomFormatService _formatService;
        private readonly Logger _logger;

        public CustomFormatCalculationService(ICustomFormatService formatService, Logger logger)
        {
            _formatService = formatService;
            _logger = logger;
        }

        public List<CustomFormat> ParseCustomFormat(RemoteGame remoteGame, long size)
        {
            var input = new CustomFormatInput
            {
                GameInfo = remoteGame.ParsedGameInfo,
                Game = remoteGame.Game,
                Size = size,
                Languages = remoteGame.Languages,
                IndexerFlags = remoteGame.Release?.IndexerFlags ?? 0
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(GameFile gameFile, Game game)
        {
            return ParseCustomFormat(gameFile, game, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(GameFile gameFile)
        {
            return ParseCustomFormat(gameFile, gameFile.Game, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Game game)
        {
            var parsed = Parser.Parser.ParseGameTitle(blocklist.SourceTitle);

            var gameInfo = new ParsedGameInfo
            {
                GameTitles = new List<string>() { game.Title },
                SimpleReleaseTitle = parsed?.SimpleReleaseTitle ?? blocklist.SourceTitle.SimplifyReleaseTitle(),
                ReleaseTitle = parsed?.ReleaseTitle ?? blocklist.SourceTitle,
                Edition = parsed?.Edition,
                Quality = blocklist.Quality,
                Languages = blocklist.Languages,
                ReleaseGroup = parsed?.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                GameInfo = gameInfo,
                Game = game,
                Size = blocklist.Size ?? 0,
                Languages = blocklist.Languages,
                IndexerFlags = blocklist.IndexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(GameHistory history, Game game)
        {
            var parsed = Parser.Parser.ParseGameTitle(history.SourceTitle);

            long.TryParse(history.Data.GetValueOrDefault("size"), out var size);
            Enum.TryParse(history.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags indexerFlags);

            var gameInfo = new ParsedGameInfo
            {
                GameTitles = new List<string>() { game.Title },
                SimpleReleaseTitle = parsed?.SimpleReleaseTitle ?? history.SourceTitle.SimplifyReleaseTitle(),
                ReleaseTitle = parsed?.ReleaseTitle ?? history.SourceTitle,
                Edition = parsed?.Edition,
                Quality = history.Quality,
                Languages = history.Languages,
                ReleaseGroup = parsed?.ReleaseGroup,
            };

            var input = new CustomFormatInput
            {
                GameInfo = gameInfo,
                Game = game,
                Size = size,
                Languages = history.Languages,
                IndexerFlags = indexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(LocalGame localGame)
        {
            var gameInfo = new ParsedGameInfo
            {
                GameTitles = new List<string>() { localGame.Game.Title },
                SimpleReleaseTitle = localGame.SceneName.IsNotNullOrWhiteSpace() ? localGame.SceneName.SimplifyReleaseTitle() : Path.GetFileName(localGame.Path).SimplifyReleaseTitle(),
                ReleaseTitle = localGame.SceneName,
                Quality = localGame.Quality,
                Edition = localGame.Edition,
                Languages = localGame.Languages,
                ReleaseGroup = localGame.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                GameInfo = gameInfo,
                Game = localGame.Game,
                Size = localGame.Size,
                Languages = localGame.Languages,
                IndexerFlags = localGame.IndexerFlags,
                Filename = Path.GetFileName(localGame.Path)
            };

            return ParseCustomFormat(input);
        }

        private List<CustomFormat> ParseCustomFormat(CustomFormatInput input)
        {
            return ParseCustomFormat(input, _formatService.All());
        }

        private static List<CustomFormat> ParseCustomFormat(CustomFormatInput input, List<CustomFormat> allCustomFormats)
        {
            var matches = new List<CustomFormat>();

            foreach (var customFormat in allCustomFormats)
            {
                var specificationMatches = customFormat.Specifications
                    .GroupBy(t => t.GetType())
                    .Select(g => new SpecificationMatchesGroup
                    {
                        Matches = g.ToDictionary(t => t, t => t.IsSatisfiedBy(input))
                    })
                    .ToList();

                if (specificationMatches.All(x => x.DidMatch))
                {
                    matches.Add(customFormat);
                }
            }

            return matches.OrderBy(x => x.Name).ToList();
        }

        private List<CustomFormat> ParseCustomFormat(GameFile gameFile, Game game, List<CustomFormat> allCustomFormats)
        {
            var releaseTitle = string.Empty;

            if (gameFile.SceneName.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using scene name for release title: {0}", gameFile.SceneName);
                releaseTitle = gameFile.SceneName;
            }
            else if (gameFile.OriginalFilePath.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using original file path for release title: {0}", Path.GetFileName(gameFile.OriginalFilePath));
                releaseTitle = Path.GetFileName(gameFile.OriginalFilePath);
            }
            else if (gameFile.RelativePath.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using relative path for release title: {0}", Path.GetFileName(gameFile.RelativePath));
                releaseTitle = Path.GetFileName(gameFile.RelativePath);
            }

            var gameInfo = new ParsedGameInfo
            {
                GameTitles = new List<string>() { game.Title },
                SimpleReleaseTitle = releaseTitle.SimplifyReleaseTitle(),
                Quality = gameFile.Quality,
                Languages = gameFile.Languages,
                ReleaseGroup = gameFile.ReleaseGroup,
                Edition = gameFile.Edition
            };

            var input = new CustomFormatInput
            {
                GameInfo = gameInfo,
                Game = game,
                Size = gameFile.Size,
                Languages = gameFile.Languages,
                IndexerFlags = gameFile.IndexerFlags,
                Filename = Path.GetFileName(gameFile.RelativePath)
            };

            return ParseCustomFormat(input, allCustomFormats);
        }
    }
}
