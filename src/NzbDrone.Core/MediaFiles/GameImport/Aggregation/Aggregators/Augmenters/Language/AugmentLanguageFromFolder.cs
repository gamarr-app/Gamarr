using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators.Augmenters.Language
{
    public class AugmentLanguageFromFolder : IAugmentLanguage
    {
        public int Order => 2;
        public string Name => "FolderName";

        public AugmentLanguageResult AugmentLanguage(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var languages = localGame.FolderGameInfo?.Languages;

            if (languages == null)
            {
                return null;
            }

            return new AugmentLanguageResult(languages, Confidence.Foldername);
        }
    }
}
