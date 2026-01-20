using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalGame
    {
        public LocalGame()
        {
            CustomFormats = new List<CustomFormat>();
        }

        public string Path { get; set; }
        public long Size { get; set; }
        public ParsedGameInfo FileGameInfo { get; set; }
        public ParsedGameInfo DownloadClientGameInfo { get; set; }
        public DownloadClientItem DownloadItem { get; set; }
        public ParsedGameInfo FolderGameInfo { get; set; }
        public Game Game { get; set; }
        public List<DeletedGameFile> OldFiles { get; set; }
        public QualityModel Quality { get; set; }
        public List<Language> Languages { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public bool ExistingFile { get; set; }
        public bool SceneSource { get; set; }
        public string ReleaseGroup { get; set; }
        public string Edition { get; set; }
        public string SceneName { get; set; }
        public bool OtherVideoFiles { get; set; }
        public List<CustomFormat> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public GrabbedReleaseInfo Release { get; set; }
        public bool ScriptImported { get; set; }
        public string FileNameBeforeRename { get; set; }
        public bool ShouldImportExtras { get; set; }
        public List<string> PossibleExtraFiles { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}
