using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(142)]
    public class game_extras : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Table("ExtraFiles");
            Delete.Table("SubtitleFiles");
            Delete.Table("MetadataFiles");

            Create.TableForModel("ExtraFiles")
                .WithColumn("GameId").AsInt32().NotNullable()
                .WithColumn("GameFileId").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("Extension").AsString().NotNullable()
                .WithColumn("Added").AsDateTime().NotNullable()
                .WithColumn("LastUpdated").AsDateTime().NotNullable();

            Create.TableForModel("SubtitleFiles")
                .WithColumn("GameId").AsInt32().NotNullable()
                .WithColumn("GameFileId").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("Extension").AsString().NotNullable()
                .WithColumn("Added").AsDateTime().NotNullable()
                .WithColumn("LastUpdated").AsDateTime().NotNullable()
                .WithColumn("Language").AsInt32().NotNullable();

            Create.TableForModel("MetadataFiles")
                .WithColumn("GameId").AsInt32().NotNullable()
                .WithColumn("Consumer").AsString().NotNullable()
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("LastUpdated").AsDateTime().NotNullable()
                .WithColumn("GameFileId").AsInt32().Nullable()
                .WithColumn("Hash").AsString().Nullable()
                .WithColumn("Added").AsDateTime().Nullable()
                .WithColumn("Extension").AsString().NotNullable();
        }
    }
}
