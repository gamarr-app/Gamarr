using NzbDrone.Core.Download;

namespace NzbDrone.Core.Validation
{
    public class DownloadClientExistsValidator
    {
        private readonly IDownloadClientFactory _downloadClientFactory;

        public DownloadClientExistsValidator(IDownloadClientFactory downloadClientFactory)
        {
            _downloadClientFactory = downloadClientFactory;
        }

        public bool Validate(int downloadClientId)
        {
            if (downloadClientId == 0)
            {
                return true;
            }

            return _downloadClientFactory.Exists(downloadClientId);
        }
    }
}
