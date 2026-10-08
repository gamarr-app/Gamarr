using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncCommand : Command
    {
        public int? DefinitionId { get; set; }

        public ImportListSyncCommand()
        {
        }

        public ImportListSyncCommand(int? definition)
        {
            DefinitionId = definition;
        }

        public override bool SendUpdatesToClient => true;

        public override bool IsTypeExclusive => true;

        public override bool UpdateScheduledTask => !DefinitionId.HasValue;

        public override IEnumerable<string> GetValidationFailures()
        {
            // Omitting the id syncs every list, which is valid; supplying 0 is not.
            if (DefinitionId is <= 0)
            {
                yield return "definitionId must be greater than zero when provided";
            }
        }
    }
}
