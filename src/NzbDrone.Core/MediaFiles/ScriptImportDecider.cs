using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tags;

namespace NzbDrone.Core.MediaFiles
{
    public interface IImportScript
    {
        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalGame localGame, GameFile gameFile, TransferMode mode);
    }

    public class ImportScriptService : IImportScript
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly IProcessProvider _processProvider;
        private readonly IConfigService _configService;
        private readonly ITagRepository _tagRepository;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public ImportScriptService(IProcessProvider processProvider,
                                   IVideoFileInfoReader videoFileInfoReader,
                                   IConfigService configService,
                                   IConfigFileProvider configFileProvider,
                                   ITagRepository tagRepository,
                                   IDiskProvider diskProvider,
                                   Logger logger)
        {
            _processProvider = processProvider;
            _videoFileInfoReader = videoFileInfoReader;
            _configService = configService;
            _configFileProvider = configFileProvider;
            _tagRepository = tagRepository;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        private static readonly Regex OutputRegex = new Regex(@"^(?:\[(?:(?<mediaFile>MediaFile)|(?<extraFile>ExtraFile))\]\s?(?<fileName>.+)|(?<preventExtraImport>\[PreventExtraImport\])|\[MoveStatus\]\s?(?:(?<deferMove>DeferMove)|(?<moveComplete>MoveComplete)|(?<renameRequested>RenameRequested)))$", RegexOptions.Compiled);

        private ScriptImportInfo ProcessOutput(List<ProcessOutputLine> processOutputLines)
        {
            var possibleExtraFiles = new List<string>();
            string mediaFile = null;
            var decision = ScriptImportDecision.MoveComplete;
            var importExtraFiles = true;

            foreach (var line in processOutputLines)
            {
                var match = OutputRegex.Match(line.Content);

                if (match.Groups["mediaFile"].Success)
                {
                    if (mediaFile is not null)
                    {
                        throw new ScriptImportException("Script output contains multiple media files. Only one media file can be returned.");
                    }

                    mediaFile = match.Groups["fileName"].Value;

                    if (!MediaFileExtensions.Extensions.Contains(Path.GetExtension(mediaFile)))
                    {
                        throw new ScriptImportException("Script output contains invalid media file: {0}", mediaFile);
                    }
                    else if (!_diskProvider.FileExists(mediaFile))
                    {
                        throw new ScriptImportException("Script output contains non-existent media file: {0}", mediaFile);
                    }
                }
                else if (match.Groups["extraFile"].Success)
                {
                    var fileName = match.Groups["fileName"].Value;

                    if (!_diskProvider.FileExists(fileName))
                    {
                        _logger.Warn("Script output contains non-existent possible extra file: {0}", fileName);
                    }

                    possibleExtraFiles.Add(fileName);
                }
                else if (match.Groups["moveComplete"].Success)
                {
                    decision = ScriptImportDecision.MoveComplete;
                }
                else if (match.Groups["renameRequested"].Success)
                {
                    decision = ScriptImportDecision.RenameRequested;
                }
                else if (match.Groups["deferMove"].Success)
                {
                    decision = ScriptImportDecision.DeferMove;
                }
                else if (match.Groups["preventExtraImport"].Success)
                {
                    importExtraFiles = false;
                }
            }

            return new ScriptImportInfo(possibleExtraFiles, mediaFile, decision, importExtraFiles);
        }

        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalGame localGame, GameFile gameFile, TransferMode mode)
        {
            var game = localGame.Game;
            var oldFiles = localGame.OldFiles;
            var downloadClientInfo = localGame.DownloadItem?.DownloadClientInfo;
            var downloadId = localGame.DownloadItem?.DownloadId;

            if (!_configService.UseScriptImport)
            {
                return ScriptImportDecision.DeferMove;
            }

            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_SourcePath", sourcePath);
            environmentVariables.Add("Gamarr_DestinationPath", destinationFilePath);

            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_TransferMode", mode.ToString());

            environmentVariables.Add("Gamarr_Game_Id", game.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game.GameMetadata.Value.Title);
            environmentVariables.Add("Gamarr_Game_Year", game.GameMetadata.Value.Year.ToString());
            environmentVariables.Add("Gamarr_Game_Path", game.Path);
            environmentVariables.Add("Gamarr_Game_IgdbId", game.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game.GameMetadata.Value.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game.GameMetadata.Value.Genres));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", game.Tags.Select(t => _tagRepository.Get(t).Label)));

            environmentVariables.Add("Gamarr_Game_In_Cinemas_Date", game.GameMetadata.Value.EarlyAccess.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_Physical_Release_Date", game.GameMetadata.Value.PhysicalRelease.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_Overview", game.GameMetadata.Value.Overview);
            environmentVariables.Add("Gamarr_GameFile_Id", gameFile.Id.ToString());
            environmentVariables.Add("Gamarr_GameFile_RelativePath", gameFile.RelativePath);
            environmentVariables.Add("Gamarr_GameFile_Path", Path.Combine(game.Path, gameFile.RelativePath));
            environmentVariables.Add("Gamarr_GameFile_Quality", gameFile.Quality.Quality.Name);
            environmentVariables.Add("Gamarr_GameFile_QualityVersion", gameFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Gamarr_GameFile_ReleaseGroup", gameFile.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Gamarr_GameFile_SceneName", gameFile.SceneName ?? string.Empty);

            environmentVariables.Add("Gamarr_Download_Client", downloadClientInfo?.Name ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Client_Type", downloadClientInfo?.Type ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Id", downloadId ?? string.Empty);
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_AudioChannels", MediaInfoFormatter.FormatAudioChannels(localGame.MediaInfo).ToString());
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_AudioCodec", MediaInfoFormatter.FormatAudioCodec(gameFile.MediaInfo, null));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_AudioLanguages", gameFile.MediaInfo.AudioLanguages.Distinct().ConcatToString(" / "));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Languages", gameFile.MediaInfo.AudioLanguages.ConcatToString(" / "));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Height", gameFile.MediaInfo.Height.ToString());
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Width", gameFile.MediaInfo.Width.ToString());
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Subtitles", gameFile.MediaInfo.Subtitles.ConcatToString(" / "));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_VideoCodec", MediaInfoFormatter.FormatVideoCodec(gameFile.MediaInfo, null));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_VideoDynamicRangeType", MediaInfoFormatter.FormatVideoDynamicRangeType(gameFile.MediaInfo));

            environmentVariables.Add("Gamarr_GameFile_CustomFormat", string.Join("|", localGame.CustomFormats));
            environmentVariables.Add("Gamarr_GameFile_CustomFormatScore", localGame.CustomFormatScore.ToString());

            if (oldFiles.Any())
            {
                environmentVariables.Add("Gamarr_DeletedRelativePaths", string.Join("|", oldFiles.Select(e => e.GameFile.RelativePath)));
                environmentVariables.Add("Gamarr_DeletedPaths", string.Join("|", oldFiles.Select(e => Path.Combine(game.Path, e.GameFile.RelativePath))));
                environmentVariables.Add("Gamarr_DeletedDateAdded", string.Join("|", oldFiles.Select(e => e.GameFile.DateAdded)));
            }

            _logger.Debug("Executing external script: {0}", _configService.ScriptImportPath);

            var processOutput = _processProvider.StartAndCapture(_configService.ScriptImportPath, $"\"{sourcePath}\" \"{destinationFilePath}\"", environmentVariables);

            _logger.Debug("Script Output: \r\n{0}", string.Join("\r\n", processOutput.Lines));

            if (processOutput.ExitCode != 0)
            {
                throw new ScriptImportException("Script exited with non-zero exit code: {0}", processOutput.ExitCode);
            }

            var scriptImportInfo = ProcessOutput(processOutput.Lines);

            var mediaFile = scriptImportInfo.MediaFile ?? destinationFilePath;
            localGame.PossibleExtraFiles = scriptImportInfo.PossibleExtraFiles;

            gameFile.RelativePath = game.Path.GetRelativePath(mediaFile);
            gameFile.Path = mediaFile;

            var exitCode = processOutput.ExitCode;

            localGame.ShouldImportExtras = scriptImportInfo.ImportExtraFiles;

            if (scriptImportInfo.Decision != ScriptImportDecision.DeferMove)
            {
                localGame.ScriptImported = true;
            }

            if (scriptImportInfo.Decision == ScriptImportDecision.RenameRequested)
            {
                gameFile.MediaInfo = _videoFileInfoReader.GetMediaInfo(mediaFile);
                gameFile.Path = null;
            }

            return scriptImportInfo.Decision;
        }
    }
}
