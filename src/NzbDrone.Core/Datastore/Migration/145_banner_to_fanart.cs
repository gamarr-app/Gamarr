using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(145)]
    public class banner_to_fanart : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"Games\" SET \"Images\" = replace(\"Images\", \'\"coverType\": \"banner\"\', \'\"coverType\": \"fanart\"\')");

            // Remove Link for images to specific GameFiles, Images are now related to the Game object only
            Execute.Sql("UPDATE \"MetadataFiles\" SET \"GameFileId\" = null WHERE \"Type\" = 2");
        }
    }
}
