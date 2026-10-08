using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.GameImport.Manual
{
    public class ManualImportCommand : Command
    {
        public List<ManualImportFile> Files { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public ImportMode ImportMode { get; set; }

        public override IEnumerable<string> GetValidationFailures()
        {
            if (Files == null)
            {
                yield return "Files must be provided";
                yield break;
            }

            for (var i = 0; i < Files.Count; i++)
            {
                if (Files[i] == null)
                {
                    yield return $"Files[{i}] must not be null";
                    continue;
                }

                if (Files[i].Path.IsNullOrWhiteSpace())
                {
                    yield return $"Files[{i}] must have a path";
                }

                if (Files[i].GameId <= 0)
                {
                    yield return $"Files[{i}] must have a gameId";
                }
            }
        }
    }
}
