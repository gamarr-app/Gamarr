using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(6)]
    public class add_game_components : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Component slots within a game (#149 phase 1): base / updates /
            // DLC. DLC components sync from metadata DlcReferences; update
            // components are created by imports. Files link via ComponentId.
            Create.TableForModel("GameComponents")
                 .WithColumn("GameId").AsInt32().NotNullable().Indexed()
                 .WithColumn("ComponentType").AsInt32().NotNullable()
                 .WithColumn("Key").AsString().NotNullable()
                 .WithColumn("Title").AsString().Nullable()
                 .WithColumn("Monitored").AsBoolean().NotNullable().WithDefaultValue(true)
                 .WithColumn("ExternalId").AsInt32().NotNullable().WithDefaultValue(0)
                 .WithColumn("Added").AsDateTime().Nullable();

            Alter.Table("GameFiles")
                 .AddColumn("ComponentId").AsInt32().NotNullable().WithDefaultValue(0);
        }
    }
}
