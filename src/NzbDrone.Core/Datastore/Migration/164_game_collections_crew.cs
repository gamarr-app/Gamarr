using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(164)]
    public class game_collections_crew : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Games").AddColumn("Collection").AsString().Nullable();
            Delete.Column("Actors").FromTable("Games");

            Create.TableForModel("Credits").WithColumn("GameId").AsInt32()
                                  .WithColumn("CreditIgdbId").AsString().Unique()
                                  .WithColumn("PersonIgdbId").AsInt32()
                                  .WithColumn("Name").AsString()
                                  .WithColumn("Images").AsString()
                                  .WithColumn("Character").AsString().Nullable()
                                  .WithColumn("Order").AsInt32()
                                  .WithColumn("Job").AsString().Nullable()
                                  .WithColumn("Department").AsString().Nullable()
                                  .WithColumn("Type").AsInt32();

            Create.Index().OnTable("Credits").OnColumn("GameId");

            Delete.FromTable("Notifications").Row(new { Implementation = "NotifyMyAndroid" });
            Delete.FromTable("Notifications").Row(new { Implementation = "Pushalot" });
        }
    }
}
