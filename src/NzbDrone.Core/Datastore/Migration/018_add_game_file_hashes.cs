using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(18)]
    public class add_game_file_hashes : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // No-Intro hash triplets per imported game file (#205). The
            // components endpoint used to hash every file of the game on every
            // GET; it now reads these rows, and a background command computes
            // them at import/rescan. Size + LastModifiedUtcTicks are the
            // invalidation key, so a replaced file is re-hashed rather than
            // matched against a stale triplet.
            Create.TableForModel("GameFileHashes")
                  .WithColumn("GameFileId").AsInt32().NotNullable().Unique()
                  .WithColumn("Size").AsInt64().NotNullable()
                  .WithColumn("LastModifiedUtcTicks").AsInt64().NotNullable()
                  .WithColumn("Md5").AsString().Nullable()
                  .WithColumn("Sha1").AsString().Nullable()
                  .WithColumn("Crc32").AsString().Nullable()
                  .WithColumn("HashedAt").AsDateTime().NotNullable();
        }
    }
}
