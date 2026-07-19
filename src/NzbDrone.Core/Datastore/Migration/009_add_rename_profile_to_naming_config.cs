using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(9)]
    public class add_rename_profile_to_naming_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NamingConfig")
                 .AddColumn("RenameProfile")
                 .AsInt32()
                 .NotNullable()
                 .WithDefaultValue((int)RenameProfile.Gamarr);
        }
    }
}
