using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators.Augmenters.Quality
{
    public class AugmentQualityFromDownloadClientItem : IAugmentQuality
    {
        public int Order => 3;
        public string Name => "DownloadClientItem";

        public AugmentQualityResult AugmentQuality(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var quality = localGame.DownloadClientGameInfo?.Quality;

            if (quality == null)
            {
                return null;
            }

            var sourceConfidence = quality.SourceDetectionSource == QualityDetectionSource.Name
                ? Confidence.Tag
                : Confidence.Fallback;

            var resolutionConfidence = quality.ResolutionDetectionSource == QualityDetectionSource.Name
                ? Confidence.Tag
                : Confidence.Fallback;

            var modifierConfidence = quality.ModifierDetectionSource == QualityDetectionSource.Name
                ? Confidence.Tag
                : Confidence.Fallback;

            var revisionConfidence = quality.RevisionDetectionSource == QualityDetectionSource.Name
                ? Confidence.Tag
                : Confidence.Fallback;

            return new AugmentQualityResult(quality.Quality.Source,
                                            sourceConfidence,
                                            quality.Quality.Resolution,
                                            resolutionConfidence,
                                            quality.Quality.Modifier,
                                            modifierConfidence,
                                            quality.Revision,
                                            revisionConfidence);
        }
    }
}
