using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(8)]
    public class add_nointro_catalog : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("NoIntroCatalogSources")
                  .WithColumn("Name").AsString().NotNullable()
                  .WithColumn("SourceUrl").AsString().NotNullable()
                  .WithColumn("PinnedRevision").AsString().Nullable()
                  .WithColumn("CatalogVersion").AsString().Nullable()
                  .WithColumn("LastSuccessfulSync").AsDateTime().Nullable()
                  .WithColumn("LastAttemptedSync").AsDateTime().Nullable()
                  .WithColumn("LastSyncError").AsString().Nullable();

            Create.TableForModel("NoIntroCatalogEntries")
                  .WithColumn("CatalogSourceId").AsInt32().NotNullable().Indexed()
                  .WithColumn("SystemKey").AsString().NotNullable().Indexed()
                  .WithColumn("CanonicalName").AsString().NotNullable()
                  .WithColumn("CanonicalFileName").AsString().NotNullable()
                  .WithColumn("PlatformFamily").AsInt32().NotNullable();

            Create.TableForModel("NoIntroCatalogHashes")
                  .WithColumn("CatalogEntryId").AsInt32().NotNullable().Indexed()
                  .WithColumn("HashType").AsString().NotNullable()
                  .WithColumn("HashValue").AsString().NotNullable().Indexed()
                  .WithColumn("IsPrimary").AsBoolean().NotNullable().WithDefaultValue(false)
                  .WithColumn("IsBadDump").AsBoolean().NotNullable().WithDefaultValue(false);

            Create.TableForModel("NoIntroSystemMappings")
                  .WithColumn("SystemKey").AsString().NotNullable().Indexed()
                  .WithColumn("DisplayName").AsString().NotNullable()
                  .WithColumn("PlatformFamily").AsInt32().NotNullable()
                  .WithColumn("RootRelativePathPattern").AsString().Nullable();

            Create.TableForModel("NoIntroVerificationSets")
                  .WithColumn("CatalogSourceId").AsInt32().NotNullable().Indexed()
                  .WithColumn("SystemKey").AsString().NotNullable().Indexed()
                  .WithColumn("RootPath").AsString().NotNullable()
                  .WithColumn("Enabled").AsBoolean().NotNullable().WithDefaultValue(true);

            Create.TableForModel("NoIntroVerificationSnapshots")
                  .WithColumn("VerificationSetId").AsInt32().NotNullable().Indexed()
                  .WithColumn("CatalogSourceId").AsInt32().NotNullable().Indexed()
                  .WithColumn("CatalogRevision").AsString().Nullable()
                  .WithColumn("StartedAt").AsDateTime().NotNullable()
                  .WithColumn("CompletedAt").AsDateTime().Nullable();

            Create.TableForModel("NoIntroVerificationResults")
                  .WithColumn("SnapshotId").AsInt32().NotNullable().Indexed()
                  .WithColumn("VerificationSetId").AsInt32().NotNullable().Indexed()
                  .WithColumn("CatalogEntryId").AsInt32().Nullable().Indexed()
                  .WithColumn("RelativePath").AsString().NotNullable()
                  .WithColumn("ArchivePath").AsString().Nullable()
                  .WithColumn("MemberPath").AsString().Nullable()
                  .WithColumn("ActualFileName").AsString().NotNullable()
                  .WithColumn("ExpectedFileName").AsString().Nullable()
                  .WithColumn("HashType").AsString().Nullable()
                  .WithColumn("HashValue").AsString().Nullable().Indexed()
                  .WithColumn("VerificationStatus").AsInt32().NotNullable()
                  .WithColumn("IsDuplicate").AsBoolean().NotNullable().WithDefaultValue(false)
                  .WithColumn("IsMissing").AsBoolean().NotNullable().WithDefaultValue(false)
                  .WithColumn("VerifiedAt").AsDateTime().NotNullable();
        }
    }
}
