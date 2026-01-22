using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(2)]
    public class AddIgdbSlug : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("GameMetadata")
                .AddColumn("IgdbSlug").AsString().Nullable();
        }
    }
}
