using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(179)]
    public class game_translation_indexes : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Index("IX_GameTranslations_Language").OnTable("GameTranslations").OnColumn("Language");
            Create.Index("IX_GameTranslations_GameId").OnTable("GameTranslations").OnColumn("GameId");
        }
    }
}
