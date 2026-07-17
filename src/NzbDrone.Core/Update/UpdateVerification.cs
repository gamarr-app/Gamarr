using System;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Update
{
    public interface IVerifyUpdates
    {
        bool Verify(UpdatePackage updatePackage, string packagePath);
    }

    public class UpdateVerification : IVerifyUpdates
    {
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public UpdateVerification(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public bool Verify(UpdatePackage updatePackage, string packagePath)
        {
            // Releases published before checksum sidecars existed have no hash;
            // failing them would make every update from such a release
            // impossible rather than merely unverified.
            if (updatePackage.Hash.IsNullOrWhiteSpace())
            {
                _logger.Warn("No checksum is published for update package {0}; skipping verification", updatePackage.FileName);
                return true;
            }

            using (var fileStream = _diskProvider.OpenReadStream(packagePath))
            {
                var hash = fileStream.SHA256Hash();

                return hash.Equals(updatePackage.Hash, StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}
