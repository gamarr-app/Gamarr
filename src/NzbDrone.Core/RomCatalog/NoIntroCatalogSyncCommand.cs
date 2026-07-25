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
    }
}
