using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(7)]
    public class component_quality_profile : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Per-component quality profile (#149): 0 means inherit the
            // game's profile; a concrete id overrides it for releases matched
            // to this slot.
            Alter.Table("GameComponents")
                 .AddColumn("QualityProfileId").AsInt32().NotNullable().WithDefaultValue(0);
        }
    }
}
