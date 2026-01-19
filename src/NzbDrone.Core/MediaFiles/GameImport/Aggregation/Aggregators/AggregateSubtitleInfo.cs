using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Extras.Subtitles;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators
{
    public class AggregateSubtitleInfo : IAggregateLocalGame
    {
        public int Order => 2;

        private readonly Logger _logger;

        public AggregateSubtitleInfo(Logger logger)
        {
            _logger = logger;
        }

        public LocalGame Aggregate(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var path = localGame.Path;
            var isSubtitleFile = SubtitleFileExtensions.Extensions.Contains(Path.GetExtension(path));

            if (!isSubtitleFile || localGame.Game == null)
            {
                return localGame;
            }

            localGame.SubtitleInfo = CleanSubtitleTitleInfo(localGame.Game.GameFile, path, localGame.FileNameBeforeRename);

            return localGame;
        }

        public SubtitleTitleInfo CleanSubtitleTitleInfo(GameFile gameFile, string path, string fileNameBeforeRename)
        {
            var subtitleTitleInfo = LanguageParser.ParseSubtitleLanguageInformation(path);

            var gameFileTitle = Path.GetFileNameWithoutExtension(fileNameBeforeRename ?? gameFile.RelativePath);
            var originalGameFileTitle = Path.GetFileNameWithoutExtension(gameFile.OriginalFilePath) ?? string.Empty;

            if (subtitleTitleInfo.TitleFirst && (gameFileTitle.Contains(subtitleTitleInfo.RawTitle, StringComparison.OrdinalIgnoreCase) || originalGameFileTitle.Contains(subtitleTitleInfo.RawTitle, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.Debug("Subtitle title '{0}' is in game file title '{1}'. Removing from subtitle title.", subtitleTitleInfo.RawTitle, gameFileTitle);

                subtitleTitleInfo = LanguageParser.ParseBasicSubtitle(path);
            }

            var cleanedTags = subtitleTitleInfo.LanguageTags.Where(t => !gameFileTitle.Contains(t, StringComparison.OrdinalIgnoreCase)).ToList();

            if (cleanedTags.Count != subtitleTitleInfo.LanguageTags.Count)
            {
                _logger.Debug("Removed language tags '{0}' from subtitle title '{1}'.", string.Join(", ", subtitleTitleInfo.LanguageTags.Except(cleanedTags)), subtitleTitleInfo.RawTitle);
                subtitleTitleInfo.LanguageTags = cleanedTags;
            }

            return subtitleTitleInfo;
        }
    }
}
