using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.GameImport.Manual;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Messaging.Commands
{
    /// <summary>
    /// A command POSTed by an API client with its required fields missing used to be
    /// queued happily and then throw on the executor thread, where the client never
    /// saw it and Sentry logged it as a crash. These assert the inputs are rejected
    /// up front instead; CommandController turns any failure into a 400.
    /// </summary>
    [TestFixture]
    public class CommandValidationFixture : TestBase
    {
        [Test]
        public void base_command_should_have_no_failures()
        {
            new RssSyncCommand().GetValidationFailures().Should().BeEmpty();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void downloaded_games_scan_should_require_a_path(string path)
        {
            new DownloadedGamesScanCommand { Path = path }
                .GetValidationFailures().Should().ContainSingle().Which.Should().Contain("path");
        }

        [Test]
        public void downloaded_games_scan_with_a_path_should_be_valid()
        {
            new DownloadedGamesScanCommand { Path = @"C:\Downloads".AsOsAgnostic() }
                .GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void manual_import_should_require_files()
        {
            new ManualImportCommand().GetValidationFailures().Should().ContainSingle();
        }

        [Test]
        public void manual_import_should_require_a_path_and_game_on_each_file()
        {
            var failures = new ManualImportCommand
            {
                Files = new List<ManualImportFile> { new ManualImportFile() }
            }.GetValidationFailures().ToList();

            failures.Should().HaveCount(2);
        }

        [Test]
        public void manual_import_should_reject_a_null_file()
        {
            new ManualImportCommand { Files = new List<ManualImportFile> { null } }
                .GetValidationFailures().Should().ContainSingle();
        }

        [Test]
        public void manual_import_with_complete_files_should_be_valid()
        {
            new ManualImportCommand
            {
                Files = new List<ManualImportFile>
                {
                    new ManualImportFile { GameId = 1, Path = @"C:\Downloads\game".AsOsAgnostic() }
                }
            }.GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void manual_import_with_no_files_should_be_valid()
        {
            // An empty list is a no-op, not an error; only a null one would throw.
            new ManualImportCommand { Files = new List<ManualImportFile>() }
                .GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void rename_files_should_require_a_game_and_files()
        {
            new RenameFilesCommand().GetValidationFailures().Should().HaveCount(2);
            new RenameFilesCommand(1, new List<int>()).GetValidationFailures().Should().ContainSingle();
            new RenameFilesCommand(1, new List<int> { 1 }).GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void rename_game_should_require_game_ids()
        {
            new RenameGameCommand().GetValidationFailures().Should().ContainSingle();
            new RenameGameCommand { GameIds = new List<int>() }.GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void games_search_should_require_game_ids()
        {
            new GamesSearchCommand().GetValidationFailures().Should().ContainSingle();
            new GamesSearchCommand { GameIds = new List<int> { 1 } }.GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void movies_search_should_require_movie_ids()
        {
            new MoviesSearchCommand().GetValidationFailures().Should().ContainSingle();
            new MoviesSearchCommand { MovieIds = new List<int> { 1 } }.GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void game_component_search_should_require_a_component()
        {
            new GameComponentSearchCommand().GetValidationFailures().Should().ContainSingle();
            new GameComponentSearchCommand { ComponentId = 1 }.GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void move_game_should_require_a_game()
        {
            new MoveGameCommand().GetValidationFailures().Should().ContainSingle();
            new MoveGameCommand { GameId = 1 }.GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void bulk_move_game_should_require_games_and_a_destination()
        {
            new BulkMoveGameCommand().GetValidationFailures().Should().HaveCount(2);

            new BulkMoveGameCommand
            {
                Games = new List<BulkMoveGame>(),
                DestinationRootFolder = @"C:\Games".AsOsAgnostic()
            }.GetValidationFailures().Should().BeEmpty();
        }

        [Test]
        public void refresh_game_should_reject_an_explicit_null_but_accept_the_default()
        {
            // The ctor defaults GameIds to an empty list, which is the "refresh everything"
            // form the scheduled task uses; only an explicit null in the body is invalid.
            new RefreshGameCommand().GetValidationFailures().Should().BeEmpty();
            new RefreshGameCommand(null).GetValidationFailures().Should().ContainSingle();
        }

        [Test]
        public void refresh_game_update_scheduled_task_should_not_throw_on_a_null_list()
        {
            new RefreshGameCommand(null).UpdateScheduledTask.Should().BeTrue();
        }

        [Test]
        public void refresh_collections_should_reject_an_explicit_null_but_accept_the_default()
        {
            new RefreshCollectionsCommand().GetValidationFailures().Should().BeEmpty();
            new RefreshCollectionsCommand(null).GetValidationFailures().Should().ContainSingle();
        }

        [Test]
        public void refresh_collections_update_scheduled_task_should_not_throw_on_a_null_list()
        {
            new RefreshCollectionsCommand(null).UpdateScheduledTask.Should().BeTrue();
        }

        [Test]
        public void rescan_game_should_accept_no_id_but_reject_zero()
        {
            new RescanGameCommand().GetValidationFailures().Should().BeEmpty();
            new RescanGameCommand(1).GetValidationFailures().Should().BeEmpty();
            new RescanGameCommand(0).GetValidationFailures().Should().ContainSingle();
        }

        [Test]
        public void nointro_catalog_sync_should_accept_no_id_but_reject_zero()
        {
            new NoIntroCatalogSyncCommand().GetValidationFailures().Should().BeEmpty();
            new NoIntroCatalogSyncCommand(1).GetValidationFailures().Should().BeEmpty();
            new NoIntroCatalogSyncCommand(0).GetValidationFailures().Should().ContainSingle();
        }

        [Test]
        public void import_list_sync_should_accept_no_id_but_reject_zero()
        {
            new ImportListSyncCommand().GetValidationFailures().Should().BeEmpty();
            new ImportListSyncCommand(1).GetValidationFailures().Should().BeEmpty();
            new ImportListSyncCommand(0).GetValidationFailures().Should().ContainSingle();
        }

        [Test]
        public void every_command_should_have_a_handler()
        {
            // CommandController resolves command types by reflection over every Command
            // implementation, so one with no IExecute<> handler is accepted and then fails
            // to resolve on the executor thread. RenameGameFolderCommand was exactly that.
            var assembly = typeof(Command).Assembly;

            var commands = assembly.GetTypes()
                .Where(t => typeof(Command).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();

            commands.Should().NotBeEmpty();

            var handlerContracts = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExecute<>))
                .Select(i => i.GetGenericArguments()[0])
                .ToHashSet();

            var unhandled = commands.Where(c => !handlerContracts.Contains(c)).Select(c => c.Name).ToList();

            unhandled.Should().BeEmpty("every command reachable from the API needs a handler");
        }
    }
}
