using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(106)]
    public class add_igdb_stuff : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Games")
                  .AddColumn("IgdbId").AsInt32().WithDefaultValue(0);
            Alter.Table("Games")
                .AddColumn("Website").AsString().Nullable();
            Alter.Table("Games")
                .AlterColumn("ImdbId").AsString().Nullable();
            Alter.Table("Games")
                .AddColumn("AlternativeTitles").AsString().Nullable();
        }
    }
}
