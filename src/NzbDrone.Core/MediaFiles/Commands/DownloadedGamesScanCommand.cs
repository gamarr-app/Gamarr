using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class DownloadedGamesScanCommand : Command
    {
        public override bool SendUpdatesToClient => SendUpdates;

        public bool SendUpdates { get; set; }

        // Properties used by third-party apps, do not modify.
        public string Path { get; set; }
        public string DownloadClientId { get; set; }
        public ImportMode ImportMode { get; set; }
        public override bool RequiresDiskAccess => true;
        public override bool IsLongRunning => true;

        public override IEnumerable<string> GetValidationFailures()
        {
            if (Path.IsNullOrWhiteSpace())
            {
                yield return "A path must be provided";
            }
        }
    }
}
