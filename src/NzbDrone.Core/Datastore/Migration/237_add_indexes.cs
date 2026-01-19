using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(237)]
    public class add_indexes : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index().OnTable("Blocklist").OnColumn("GameId");
            Create.Index().OnTable("Blocklist").OnColumn("Date");

            Create.Index()
                .OnTable("History")
                .OnColumn("GameId").Ascending()
                .OnColumn("Date").Descending();

            Delete.Index().OnTable("History").OnColumn("DownloadId");
            Create.Index()
                .OnTable("History")
                .OnColumn("DownloadId").Ascending()
                .OnColumn("Date").Descending();

            Create.Index().OnTable("Games").OnColumn("GameFileId");
            Create.Index().OnTable("Games").OnColumn("Path");
        }
    }
}
