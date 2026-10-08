using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroCatalogSyncCommand : Command
    {
        public int? CatalogSourceId { get; set; }

        public NoIntroCatalogSyncCommand()
        {
        }

        public NoIntroCatalogSyncCommand(int? catalogSourceId)
        {
            CatalogSourceId = catalogSourceId;
        }

        public override bool SendUpdatesToClient => true;
        public override bool IsTypeExclusive => true;

        public override IEnumerable<string> GetValidationFailures()
        {
            // Omitting the id syncs every source, which is valid; supplying 0 is not.
            if (CatalogSourceId is <= 0)
            {
                yield return "catalogSourceId must be greater than zero when provided";
            }
        }
    }
}
