using NLog;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public interface IDetectSample
    {
        DetectSampleResult IsSample(GameMetadata game, string path);
    }

    public class DetectSample : IDetectSample
    {
        private readonly Logger _logger;

        public DetectSample(Logger logger)
        {
            _logger = logger;
        }

        public DetectSampleResult IsSample(GameMetadata game, string path)
        {
            // Game files are never samples - sample detection via ffprobe is not applicable
            _logger.Debug("Skipping sample check for game file: {0}", path);
            return DetectSampleResult.NotSample;
        }
    }
}
