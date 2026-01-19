using System.IO;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public static class SceneNameCalculator
    {
        public static string GetSceneName(LocalGame localGame)
        {
            var otherVideoFiles = localGame.OtherVideoFiles;
            var downloadClientInfo = localGame.DownloadClientGameInfo;

            if (!otherVideoFiles && downloadClientInfo != null)
            {
                return FileExtensions.RemoveFileExtension(downloadClientInfo.ReleaseTitle);
            }

            var fileName = Path.GetFileNameWithoutExtension(localGame.Path.CleanFilePath());

            if (SceneChecker.IsSceneTitle(fileName))
            {
                return fileName;
            }

            var folderTitle = localGame.FolderGameInfo?.ReleaseTitle;

            if (!otherVideoFiles &&
                folderTitle.IsNotNullOrWhiteSpace() &&
                SceneChecker.IsSceneTitle(folderTitle))
            {
                return folderTitle;
            }

            return null;
        }
    }
}
