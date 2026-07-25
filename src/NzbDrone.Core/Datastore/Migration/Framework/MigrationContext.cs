using System;

namespace NzbDrone.Core.Datastore.Migration.Framework
{
    public class MigrationContext
    {
        // Thread-static: migrations run synchronously on the thread that set
        // the context, and parallel test fixtures each create their own test
        // database — a shared static let one fixture null the context out
        // from under another mid-migration.
        [ThreadStatic]
        private static MigrationContext _current;

        public static MigrationContext Current
        {
            get => _current;
            set => _current = value;
        }

        public MigrationType MigrationType { get; private set; }
        public long? DesiredVersion { get; set; }
        public Action<NzbDroneMigrationBase> BeforeMigration { get; set; }

        public MigrationContext(MigrationType migrationType, long? desiredVersion = null)
        {
            MigrationType = migrationType;
            DesiredVersion = desiredVersion;
        }
    }
}
