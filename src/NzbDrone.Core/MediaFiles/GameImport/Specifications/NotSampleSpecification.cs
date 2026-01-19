using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class NotSampleSpecification : IImportDecisionEngineSpecification
    {
        private readonly IDetectSample _detectSample;
        private readonly Logger _logger;

        public NotSampleSpecification(IDetectSample detectSample,
                                      Logger logger)
        {
            _detectSample = detectSample;
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            if (localGame.ExistingFile)
            {
                _logger.Debug("Existing file, skipping sample check");
                return ImportSpecDecision.Accept();
            }

            var sample = _detectSample.IsSample(localGame.Game.GameMetadata, localGame.Path);

            if (sample == DetectSampleResult.Sample)
            {
                return ImportSpecDecision.Reject(ImportRejectionReason.Sample, "Sample");
            }
            else if (sample == DetectSampleResult.Indeterminate)
            {
                return ImportSpecDecision.Reject(ImportRejectionReason.SampleIndeterminate, "Unable to determine if file is a sample");
            }

            return ImportSpecDecision.Accept();
        }
    }
}
