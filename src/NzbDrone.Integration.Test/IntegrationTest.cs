using System;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Datastore;

namespace NzbDrone.Integration.Test
{
    [Parallelizable(ParallelScope.Fixtures)]
    public abstract class IntegrationTest : IntegrationTestBase
    {
        protected static int StaticPort = 6767;

        protected NzbDroneRunner _runner;

        public override string GameRootFolder => GetTempDirectory("GameRootFolder");

        protected int Port { get; private set; }

        protected PostgresOptions PostgresOptions { get; set; } = new ();

        protected override string RootUrl => $"http://localhost:{Port}/";

        protected override string ApiKey => _runner.ApiKey;

        /// <summary>
        /// Mock metadata is enabled by default so tests work without IGDB/Steam network access.
        /// Override to false for tests that specifically need real API responses.
        /// </summary>
        protected virtual bool UseMockMetadata => true;

        /// <summary>
        /// Override this property to specify a custom path to mock data files.
        /// If not specified, the system will auto-detect the mock data path.
        /// </summary>
        protected virtual string MockDataPath => null;

        protected override void StartTestTarget()
        {
            Port = Interlocked.Increment(ref StaticPort);

            PostgresOptions = PostgresDatabase.GetTestOptions();

            if (PostgresOptions?.Host != null)
            {
                CreatePostgresDb(PostgresOptions);
            }

            _runner = new NzbDroneRunner(LogManager.GetCurrentClassLogger(), PostgresOptions, Port);
            _runner.Kill();

            // Configure mock metadata mode
            _runner.UseMockMetadata = UseMockMetadata;
            _runner.MockDataPath = MockDataPath ?? FindMockDataPath();

            _runner.Start();
        }

        /// <summary>
        /// Attempts to find the mock data path relative to the test directory.
        /// </summary>
        private string FindMockDataPath()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            var possiblePaths = new[]
            {
                Path.Combine(testDir, "Files", "MockData"),
                Path.Combine(testDir, "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "src", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "..", "src", "NzbDrone.Core.Test", "Files", "MockData"),
            };

            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        protected override void InitializeTestTarget()
        {
            // Make sure tasks have been initialized so the config put below doesn't cause errors
            WaitForCompletion(() => Tasks.All().SelectList(x => x.TaskName).Contains("RssSync"), 30000);

            var indexer = Indexers.Schema().FirstOrDefault(i => i.Implementation == nameof(Newznab));

            if (indexer == null)
            {
                throw new NullReferenceException("Expected valid indexer schema, found null");
            }

            indexer.EnableRss = false;
            indexer.EnableInteractiveSearch = false;
            indexer.EnableAutomaticSearch = false;
            indexer.ConfigContract = nameof(NewznabSettings);
            indexer.Implementation = nameof(Newznab);
            indexer.Name = "NewznabTest";
            indexer.Protocol = Core.Indexers.DownloadProtocol.Usenet;

            // Change Console Log Level to Debug so we get more details.
            var config = HostConfig.Get(1);
            config.ConsoleLogLevel = "Debug";
            HostConfig.Put(config);
        }

        protected override void StopTestTarget()
        {
            _runner.Kill();
            if (PostgresOptions?.Host != null)
            {
                DropPostgresDb(PostgresOptions);
            }
        }

        private static void CreatePostgresDb(PostgresOptions options)
        {
            PostgresDatabase.Create(options, MigrationType.Main);
            PostgresDatabase.Create(options, MigrationType.Log);
        }

        private static void DropPostgresDb(PostgresOptions options)
        {
            PostgresDatabase.Drop(options, MigrationType.Main);
            PostgresDatabase.Drop(options, MigrationType.Log);
        }
    }
}
