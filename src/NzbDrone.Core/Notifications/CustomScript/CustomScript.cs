using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tags;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public class CustomScript : NotificationBase<CustomScriptSettings>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IProcessProvider _processProvider;
        private readonly ITagRepository _tagRepository;
        private readonly ILocalizationService _localizationService;
        private readonly Logger _logger;

        public CustomScript(IConfigFileProvider configFileProvider,
            IConfigService configService,
            IDiskProvider diskProvider,
            IProcessProvider processProvider,
            ITagRepository tagRepository,
            ILocalizationService localizationService,
            Logger logger)
        {
            _configFileProvider = configFileProvider;
            _configService = configService;
            _diskProvider = diskProvider;
            _processProvider = processProvider;
            _tagRepository = tagRepository;
            _localizationService = localizationService;
            _logger = logger;
        }

        public override string Name => _localizationService.GetLocalizedString("NotificationsCustomScriptSettingsName");

        public override string Link => "https://wiki.servarr.com/gamarr/settings#connections";

        public override ProviderMessage Message => new ProviderMessage(_localizationService.GetLocalizedString("NotificationsCustomScriptSettingsProviderMessage", new Dictionary<string, object> { { "eventTypeTest", "Test" } }), ProviderMessageType.Warning);

        public override void OnGrab(GrabMessage message)
        {
            var game = message.Game;
            var remoteGame = message.RemoteGame;
            var quality = message.Quality;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "Grab");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Game_Id", game.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game.GameMetadata.Value.Title);
            environmentVariables.Add("Gamarr_Game_Year", game.GameMetadata.Value.Year.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game.GameMetadata.Value.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game.GameMetadata.Value.Genres));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", GetTagLabels(game)));
            environmentVariables.Add("Gamarr_Game_ImdbId", game.GameMetadata.Value.ImdbId ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_IgdbId", game.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_In_Cinemas_Date", game.GameMetadata.Value.InDevelopment.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_Physical_Release_Date", game.GameMetadata.Value.PhysicalRelease.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_Overview", game.GameMetadata.Value.Overview);
            environmentVariables.Add("Gamarr_Release_Title", remoteGame.Release.Title);
            environmentVariables.Add("Gamarr_Release_Indexer", remoteGame.Release.Indexer ?? string.Empty);
            environmentVariables.Add("Gamarr_Release_Size", remoteGame.Release.Size.ToString());
            environmentVariables.Add("Gamarr_Release_ReleaseGroup", remoteGame.ParsedGameInfo.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Gamarr_Release_Quality", quality.Quality.Name);
            environmentVariables.Add("Gamarr_Release_QualityVersion", quality.Revision.Version.ToString());
            environmentVariables.Add("Gamarr_IndexerFlags", remoteGame.Release.IndexerFlags.ToString());
            environmentVariables.Add("Gamarr_Download_Client", message.DownloadClientName ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Client_Type", message.DownloadClientType ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Id", message.DownloadId ?? string.Empty);
            environmentVariables.Add("Gamarr_Release_CustomFormat", string.Join("|", remoteGame.CustomFormats));
            environmentVariables.Add("Gamarr_Release_CustomFormatScore", remoteGame.CustomFormatScore.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnDownload(DownloadMessage message)
        {
            var game = message.Game;
            var gameFile = message.GameFile;
            var sourcePath = message.SourcePath;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "Download");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_IsUpgrade", message.OldGameFiles.Any().ToString());
            environmentVariables.Add("Gamarr_Game_Id", game.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game.GameMetadata.Value.Title);
            environmentVariables.Add("Gamarr_Game_Year", game.GameMetadata.Value.Year.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game.GameMetadata.Value.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game.GameMetadata.Value.Genres));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", GetTagLabels(game)));
            environmentVariables.Add("Gamarr_Game_Path", game.Path);
            environmentVariables.Add("Gamarr_Game_ImdbId", game.GameMetadata.Value.ImdbId ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_IgdbId", game.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_In_Cinemas_Date", game.GameMetadata.Value.InDevelopment.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_Physical_Release_Date", game.GameMetadata.Value.PhysicalRelease.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_Overview", game.GameMetadata.Value.Overview);
            environmentVariables.Add("Gamarr_GameFile_Id", gameFile.Id.ToString());
            environmentVariables.Add("Gamarr_GameFile_RelativePath", gameFile.RelativePath);
            environmentVariables.Add("Gamarr_GameFile_Path", Path.Combine(game.Path, gameFile.RelativePath));
            environmentVariables.Add("Gamarr_GameFile_Quality", gameFile.Quality.Quality.Name);
            environmentVariables.Add("Gamarr_GameFile_QualityVersion", gameFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Gamarr_GameFile_ReleaseGroup", gameFile.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Gamarr_GameFile_SceneName", gameFile.SceneName ?? string.Empty);
            environmentVariables.Add("Gamarr_GameFile_SourcePath", sourcePath);
            environmentVariables.Add("Gamarr_GameFile_SourceFolder", Path.GetDirectoryName(sourcePath));
            environmentVariables.Add("Gamarr_Download_Client", message.DownloadClientInfo?.Name ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Client_Type", message.DownloadClientInfo?.Type ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Id", message.DownloadId ?? string.Empty);
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_AudioChannels", MediaInfoFormatter.FormatAudioChannels(gameFile.MediaInfo).ToString());
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_AudioCodec", MediaInfoFormatter.FormatAudioCodec(gameFile.MediaInfo, null));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_AudioLanguages", gameFile.MediaInfo.AudioLanguages.Distinct().ConcatToString(" / "));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Languages", gameFile.MediaInfo.AudioLanguages.ConcatToString(" / "));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Height", gameFile.MediaInfo.Height.ToString());
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Width", gameFile.MediaInfo.Width.ToString());
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_Subtitles", gameFile.MediaInfo.Subtitles.ConcatToString(" / "));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_VideoCodec", MediaInfoFormatter.FormatVideoCodec(gameFile.MediaInfo, null));
            environmentVariables.Add("Gamarr_GameFile_MediaInfo_VideoDynamicRangeType", MediaInfoFormatter.FormatVideoDynamicRangeType(gameFile.MediaInfo));
            environmentVariables.Add("Gamarr_GameFile_CustomFormat", string.Join("|", message.GameInfo.CustomFormats));
            environmentVariables.Add("Gamarr_GameFile_CustomFormatScore", message.GameInfo.CustomFormatScore.ToString());
            environmentVariables.Add("Gamarr_Release_Indexer", message.Release?.Indexer);
            environmentVariables.Add("Gamarr_Release_Size", message.Release?.Size.ToString());
            environmentVariables.Add("Gamarr_Release_Title", message.Release?.Title);

            if (message.OldGameFiles.Any())
            {
                environmentVariables.Add("Gamarr_DeletedRelativePaths", string.Join("|", message.OldGameFiles.Select(e => e.GameFile.RelativePath)));
                environmentVariables.Add("Gamarr_DeletedPaths", string.Join("|", message.OldGameFiles.Select(e => Path.Combine(game.Path, e.GameFile.RelativePath))));
                environmentVariables.Add("Gamarr_DeletedDateAdded", string.Join("|", message.OldGameFiles.Select(e => e.GameFile.DateAdded)));
                environmentVariables.Add("Gamarr_DeletedRecycleBinPaths", string.Join("|", message.OldGameFiles.Select(e => e.RecycleBinPath ?? string.Empty)));
            }

            ExecuteScript(environmentVariables);
        }

        public override void OnGameRename(Game game, List<RenamedGameFile> renamedFiles)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "Rename");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Game_Id", game.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game.GameMetadata.Value.Title);
            environmentVariables.Add("Gamarr_Game_Year", game.GameMetadata.Value.Year.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game.GameMetadata.Value.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game.GameMetadata.Value.Genres));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", GetTagLabels(game)));
            environmentVariables.Add("Gamarr_Game_Path", game.Path);
            environmentVariables.Add("Gamarr_Game_ImdbId", game.GameMetadata.Value.ImdbId ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_IgdbId", game.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_In_Cinemas_Date", game.GameMetadata.Value.InDevelopment.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_Physical_Release_Date", game.GameMetadata.Value.PhysicalRelease.ToString() ?? string.Empty);
            environmentVariables.Add("Gamarr_GameFile_Ids", string.Join(",", renamedFiles.Select(e => e.GameFile.Id)));
            environmentVariables.Add("Gamarr_GameFile_RelativePaths", string.Join("|", renamedFiles.Select(e => e.GameFile.RelativePath)));
            environmentVariables.Add("Gamarr_GameFile_Paths", string.Join("|", renamedFiles.Select(e => Path.Combine(game.Path, e.GameFile.RelativePath))));
            environmentVariables.Add("Gamarr_GameFile_PreviousRelativePaths", string.Join("|", renamedFiles.Select(e => e.PreviousRelativePath)));
            environmentVariables.Add("Gamarr_GameFile_PreviousPaths", string.Join("|", renamedFiles.Select(e => e.PreviousPath)));

            ExecuteScript(environmentVariables);
        }

        public override void OnGameAdded(Game game)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "GameAdded");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Game_Id", game.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game.GameMetadata.Value.Title);
            environmentVariables.Add("Gamarr_Game_Year", game.GameMetadata.Value.Year.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game.GameMetadata.Value.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game.GameMetadata.Value.Genres));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", GetTagLabels(game)));
            environmentVariables.Add("Gamarr_Game_Path", game.Path);
            environmentVariables.Add("Gamarr_Game_ImdbId", game.GameMetadata.Value.ImdbId ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_IgdbId", game.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_AddMethod", game.AddOptions.AddMethod.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
            var game = deleteMessage.Game;
            var gameFile = deleteMessage.GameFile;

            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "GameFileDelete");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_GameFile_DeleteReason", deleteMessage.Reason.ToString());
            environmentVariables.Add("Gamarr_Game_Id", game.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game.Title);
            environmentVariables.Add("Gamarr_Game_Year", game.Year.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game.GameMetadata.Value.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game.GameMetadata.Value.Genres));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", GetTagLabels(game)));
            environmentVariables.Add("Gamarr_Game_Path", game.Path);
            environmentVariables.Add("Gamarr_Game_ImdbId", game.GameMetadata.Value.ImdbId ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_IgdbId", game.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_Overview", game.GameMetadata.Value.Overview);
            environmentVariables.Add("Gamarr_GameFile_Id", gameFile.Id.ToString());
            environmentVariables.Add("Gamarr_GameFile_RelativePath", gameFile.RelativePath);
            environmentVariables.Add("Gamarr_GameFile_Path", Path.Combine(game.Path, gameFile.RelativePath));
            environmentVariables.Add("Gamarr_GameFile_Size", gameFile.Size.ToString());
            environmentVariables.Add("Gamarr_GameFile_Quality", gameFile.Quality.Quality.Name);
            environmentVariables.Add("Gamarr_GameFile_QualityVersion", gameFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Gamarr_GameFile_ReleaseGroup", gameFile.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Gamarr_GameFile_SceneName", gameFile.SceneName ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnGameDelete(GameDeleteMessage deleteMessage)
        {
            var game = deleteMessage.Game;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "GameDelete");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Game_Id", game.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game.GameMetadata.Value.Title);
            environmentVariables.Add("Gamarr_Game_Year", game.GameMetadata.Value.Year.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game.GameMetadata.Value.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game.GameMetadata.Value.Genres));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", GetTagLabels(game)));
            environmentVariables.Add("Gamarr_Game_Path", game.Path);
            environmentVariables.Add("Gamarr_Game_ImdbId", game.GameMetadata.Value.ImdbId ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_IgdbId", game.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_DeletedFiles", deleteMessage.DeletedFiles.ToString());
            environmentVariables.Add("Gamarr_Game_Overview", game.GameMetadata.Value.Overview);

            if (deleteMessage.DeletedFiles && game.GameFile != null)
            {
                environmentVariables.Add("Gamarr_Game_Folder_Size", game.GameFile.Size.ToString());
            }

            ExecuteScript(environmentVariables);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "HealthIssue");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Health_Issue_Level", Enum.GetName(typeof(HealthCheckResult), healthCheck.Type));
            environmentVariables.Add("Gamarr_Health_Issue_Message", healthCheck.Message);
            environmentVariables.Add("Gamarr_Health_Issue_Type", healthCheck.Source.Name);
            environmentVariables.Add("Gamarr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "HealthRestored");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Health_Restored_Level", Enum.GetName(typeof(HealthCheckResult), previousCheck.Type));
            environmentVariables.Add("Gamarr_Health_Restored_Message", previousCheck.Message);
            environmentVariables.Add("Gamarr_Health_Restored_Type", previousCheck.Source.Name);
            environmentVariables.Add("Gamarr_Health_Restored_Wiki", previousCheck.WikiUrl.ToString() ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "ApplicationUpdate");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Update_Message", updateMessage.Message);
            environmentVariables.Add("Gamarr_Update_NewVersion", updateMessage.NewVersion.ToString());
            environmentVariables.Add("Gamarr_Update_PreviousVersion", updateMessage.PreviousVersion.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnManualInteractionRequired(ManualInteractionRequiredMessage message)
        {
            var game = message.Game;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Gamarr_EventType", "ManualInteractionRequired");
            environmentVariables.Add("Gamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Gamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Gamarr_Game_Id", game?.Id.ToString());
            environmentVariables.Add("Gamarr_Game_Title", game?.GameMetadata.Value.Title);
            environmentVariables.Add("Gamarr_Game_Year", game?.GameMetadata.Value.Year.ToString());
            environmentVariables.Add("Gamarr_Game_OriginalLanguage", IsoLanguages.Get(game?.GameMetadata.Value.OriginalLanguage)?.ThreeLetterCode);
            environmentVariables.Add("Gamarr_Game_Genres", string.Join("|", game?.GameMetadata.Value.Genres ?? new List<string>()));
            environmentVariables.Add("Gamarr_Game_Tags", string.Join("|", GetTagLabels(game)));
            environmentVariables.Add("Gamarr_Game_Path", game?.Path);
            environmentVariables.Add("Gamarr_Game_ImdbId", game?.GameMetadata.Value.ImdbId ?? string.Empty);
            environmentVariables.Add("Gamarr_Game_IgdbId", game?.GameMetadata.Value.IgdbId.ToString());
            environmentVariables.Add("Gamarr_Game_Overview", game?.GameMetadata.Value.Overview);
            environmentVariables.Add("Gamarr_Download_Client", message.DownloadClientInfo?.Name ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Client_Type", message.DownloadClientInfo?.Type ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Id", message.DownloadId ?? string.Empty);
            environmentVariables.Add("Gamarr_Download_Size", message.TrackedDownload.DownloadItem.TotalSize.ToString());
            environmentVariables.Add("Gamarr_Download_Title", message.TrackedDownload.DownloadItem.Title);

            ExecuteScript(environmentVariables);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            if (!_diskProvider.FileExists(Settings.Path))
            {
                failures.Add(new NzbDroneValidationFailure("Path", _localizationService.GetLocalizedString("NotificationsCustomScriptValidationFileDoesNotExist")));
            }

            if (failures.Empty())
            {
                try
                {
                    var environmentVariables = new StringDictionary
                    {
                        { "Gamarr_EventType", "Test" },
                        { "Gamarr_InstanceName", _configFileProvider.InstanceName },
                        { "Gamarr_ApplicationUrl", _configService.ApplicationUrl }
                    };

                    var processOutput = ExecuteScript(environmentVariables);

                    if (processOutput.ExitCode != 0)
                    {
                        failures.Add(new NzbDroneValidationFailure(string.Empty, $"Script exited with code: {processOutput.ExitCode}"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    failures.Add(new NzbDroneValidationFailure(string.Empty, ex.Message));
                }
            }

            return new ValidationResult(failures);
        }

        private ProcessOutput ExecuteScript(StringDictionary environmentVariables)
        {
            _logger.Debug("Executing external script: {0}", Settings.Path);

            var processOutput = _processProvider.StartAndCapture(Settings.Path, Settings.Arguments, environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", Settings.Path, processOutput.ExitCode);
            _logger.Debug("Script Output: \r\n{0}", string.Join("\r\n", processOutput.Lines));

            return processOutput;
        }

        private bool ValidatePathParent(string possibleParent, string path)
        {
            return possibleParent.IsParentPath(path);
        }

        private List<string> GetTagLabels(Game game)
        {
            if (game == null)
            {
                return new List<string>();
            }

            return _tagRepository.GetTags(game.Tags)
                .Select(t => t.Label)
                .Where(l => l.IsNotNullOrWhiteSpace())
                .OrderBy(l => l)
                .ToList();
        }
    }
}
