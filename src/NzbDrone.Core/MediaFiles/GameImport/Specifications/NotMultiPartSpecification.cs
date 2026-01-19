using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Specifications
{
    public class NotMultiPartSpecification : IImportDecisionEngineSpecification
    {
        private static readonly Regex[] GameMultiPartRegex = new[]
        {
            new Regex(@"(?<!^)(?<identifier>[ _.-]*(?:cd|dvd|p(?:(?:ar)?t)?|dis[ck])[ _.-]*[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?<!^)(?<identifier>[ _.-]*(?:cd|dvd|p(?:(?:ar)?t)?|dis[ck])[ _.-]*[a-d]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };

        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public NotMultiPartSpecification(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            if (GameMultiPartRegex.Any(v => v.IsMatch(localGame.Path)))
            {
                var filesInDirectory = _diskProvider.GetFiles(localGame.Path.GetParentPath(), false).ToList();

                foreach (var regex in GameMultiPartRegex)
                {
                    if (filesInDirectory.Count(file => regex.Replace(file, "") == regex.Replace(localGame.Path, "")) > 1)
                    {
                        _logger.Debug("Rejected Multi-Part File: {0}", localGame.Path);

                        return ImportSpecDecision.Reject(ImportRejectionReason.MultiPartGame, "File is suspected multi-part file, Gamarr doesn't support this");
                    }
                }
            }

            return ImportSpecDecision.Accept();
        }
    }
}
