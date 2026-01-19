using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators.Augmenters.Language
{
    public class AugmentLanguageFromFileName : IAugmentLanguage
    {
        public int Order => 1;
        public string Name => "FileName";

        public AugmentLanguageResult AugmentLanguage(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var languages = localGame.FileGameInfo?.Languages;

            if (languages == null)
            {
                return null;
            }

            return new AugmentLanguageResult(languages, Confidence.Filename);
        }
    }
}
