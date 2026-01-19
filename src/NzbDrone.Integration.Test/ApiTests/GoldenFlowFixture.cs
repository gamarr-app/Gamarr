using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.IndexerSearch;
using Gamarr.Api.V3.RootFolders;

namespace NzbDrone.Integration.Test.ApiTests
{
    /// <summary>
    /// Golden flow integration tests covering the complete user journey:
    /// 1. Search for a game
    /// 2. Add the game to library
    /// 3. Trigger search for downloads
    /// 4. Move/organize game folders
    /// </summary>
    [TestFixture]
    public class GoldenFlowFixture : IntegrationTest
    {
        private string _testRootFolder;
        private string _secondRootFolder;

        [SetUp]
        public void SetUp()
        {
            _testRootFolder = GetTempDirectory("GoldenFlow", "Games");
            _secondRootFolder = GetTempDirectory("GoldenFlow", "MovedGames");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test games
            var games = Games.All();
            foreach (var game in games.Where(g => g.Path != null && g.Path.Contains("GoldenFlow")))
            {
                Games.Delete(game.Id);
            }

            // Clean up root folders
            var rootFolders = RootFolders.All();
            foreach (var folder in rootFolders.Where(f => f.Path != null && f.Path.Contains("GoldenFlow")))
            {
                RootFolders.Delete(folder.Id);
            }
        }

        [Test]
        [Order(0)]
        public void step1_search_for_game_by_title()
        {
            // Search for a game by title
            var searchResults = Games.Lookup("half-life");

            searchResults.Should().NotBeNull();
            searchResults.Should().NotBeEmpty();
            searchResults.Should().Contain(g => g.Title.Contains("Half-Life"));
        }

        [Test]
        [Order(1)]
        public void step2_search_for_game_by_igdb_id()
        {
            // Search for a specific game by IGDB ID
            var searchResults = Games.Lookup("igdb:21");

            searchResults.Should().NotBeNull();
            searchResults.Should().NotBeEmpty();
            searchResults.Should().Contain(g => g.IgdbId == 21);
            searchResults.First(g => g.IgdbId == 21).Title.Should().Be("Half-Life 2");
        }

        [Test]
        [Order(2)]
        public void step3_add_game_to_library()
        {
            // First ensure the game doesn't exist
            EnsureNoGame(21, "Half-Life 2");

            // Search and get the game
            var searchResults = Games.Lookup("igdb:21");
            var game = searchResults.First(g => g.IgdbId == 21);

            // Set required fields
            game.QualityProfileId = 1;
            game.Path = Path.Combine(_testRootFolder, game.Title);
            game.Monitored = true;

            // Create the directory
            Directory.CreateDirectory(game.Path);

            // Add the game
            var result = Games.Post(game);

            result.Should().NotBeNull();
            result.Id.Should().NotBe(0);
            result.Title.Should().Be("Half-Life 2");
            result.IgdbId.Should().Be(21);
            result.Path.Should().Be(Path.Combine(_testRootFolder, game.Title));

            Commands.WaitAll();
        }

        [Test]
        [Order(3)]
        public void step4_verify_game_in_library()
        {
            // Ensure game exists
            var game = EnsureGame(21, "Half-Life 2");

            // Verify it's in the library
            var allGames = Games.All();
            allGames.Should().Contain(g => g.IgdbId == 21);

            // Verify we can get it by ID
            var retrieved = Games.Get(game.Id);
            retrieved.Should().NotBeNull();
            retrieved.Title.Should().Be("Half-Life 2");
        }

        [Test]
        [Order(4)]
        public void step5_trigger_game_search()
        {
            // Ensure game exists
            var game = EnsureGame(21, "Half-Life 2");

            // Trigger a search for the game (this would search indexers for releases)
            // Note: This will complete quickly without actual indexers configured
            var command = Commands.PostAndWait(new GamesSearchCommand { GameIds = new List<int> { game.Id } });

            command.Should().NotBeNull();
            command.Status.Should().Be(NzbDrone.Core.Messaging.Commands.CommandStatus.Completed);
        }

        [Test]
        [Order(5)]
        public void step6_configure_download_client()
        {
            // Set up a test download client
            var client = EnsureDownloadClient();

            client.Should().NotBeNull();
            client.Id.Should().NotBe(0);
            client.Name.Should().Be("Test UsenetBlackhole");
        }

        [Test]
        [Order(6)]
        public void step7_verify_queue_accessible()
        {
            // Ensure download client exists
            EnsureDownloadClient();

            // Get queue - should be accessible even if empty
            var request = Queue.BuildRequest();
            request.AddParameter("includeUnknownGameItems", true);

            var queue = Queue.Get<Gamarr.Http.PagingResource<Gamarr.Api.V3.Queue.QueueResource>>(request);

            queue.Should().NotBeNull();
            queue.Records.Should().NotBeNull();
        }

        [Test]
        [Order(7)]
        public void step8_add_root_folder()
        {
            // Add a root folder for organizing games
            var rootFolder = new RootFolderResource
            {
                Path = _secondRootFolder
            };

            var result = RootFolders.Post(rootFolder);

            result.Should().NotBeNull();
            result.Id.Should().NotBe(0);
            result.Path.Should().Be(_secondRootFolder);
        }

        [Test]
        [Order(8)]
        public void step9_move_game_folder()
        {
            // Ensure we have the game
            var game = EnsureGame(21, "Half-Life 2");

            // Store the original path
            var originalPath = game.Path;

            // Move the game to a new location
            var newPath = Path.Combine(_secondRootFolder, game.Title);
            Directory.CreateDirectory(newPath);

            var moveCommand = new MoveGameCommand
            {
                GameId = game.Id,
                SourcePath = originalPath,
                DestinationPath = newPath
            };

            // Execute the move command
            var command = Commands.PostAndWait(moveCommand);

            command.Should().NotBeNull();
            command.Status.Should().Be(NzbDrone.Core.Messaging.Commands.CommandStatus.Completed);

            // Verify the game path was updated
            var movedGame = Games.Get(game.Id);
            movedGame.Path.Should().Be(newPath);
        }

        [Test]
        [Order(9)]
        public void step10_update_game_monitored_status()
        {
            // Ensure game exists and is monitored
            var game = EnsureGame(21, "Half-Life 2", true);

            // Verify it's monitored
            game.Monitored.Should().BeTrue();

            // Update to unmonitored
            game.Monitored = false;
            var result = Games.Put(game);

            result.Monitored.Should().BeFalse();

            // Verify the change persisted
            var updated = Games.Get(game.Id);
            updated.Monitored.Should().BeFalse();
        }

        [Test]
        [Order(10)]
        public void step11_delete_game()
        {
            // Ensure game exists
            var game = EnsureGame(21, "Half-Life 2");

            // Verify it exists
            Games.Get(game.Id).Should().NotBeNull();

            // Delete the game
            Games.Delete(game.Id);

            // Verify it's gone
            Games.All().Should().NotContain(g => g.IgdbId == 21);
        }

        [Test]
        public void complete_golden_flow_end_to_end()
        {
            // This test runs the entire golden flow in a single test
            // for scenarios where ordered tests might not run correctly

            // 1. Search for a game
            var searchResults = Games.Lookup("igdb:38");
            searchResults.Should().NotBeEmpty();
            var gameToAdd = searchResults.First(g => g.IgdbId == 38);
            gameToAdd.Title.Should().Be("Portal");

            // 2. Add the game
            EnsureNoGame(38, "Portal");
            gameToAdd.QualityProfileId = 1;
            gameToAdd.Path = Path.Combine(_testRootFolder, gameToAdd.Title);
            gameToAdd.Monitored = true;
            Directory.CreateDirectory(gameToAdd.Path);

            var addedGame = Games.Post(gameToAdd);
            addedGame.Should().NotBeNull();
            addedGame.Id.Should().NotBe(0);
            Commands.WaitAll();

            // 3. Verify game is in library
            var libraryGames = Games.All();
            libraryGames.Should().Contain(g => g.IgdbId == 38);

            // 4. Trigger search for downloads
            var searchCommand = Commands.PostAndWait(new GamesSearchCommand { GameIds = new List<int> { addedGame.Id } });
            searchCommand.Status.Should().Be(NzbDrone.Core.Messaging.Commands.CommandStatus.Completed);

            // 5. Set up root folder for moving
            var moveFolder = new RootFolderResource { Path = _secondRootFolder };
            var rootFolder = RootFolders.Post(moveFolder);
            rootFolder.Should().NotBeNull();

            // 6. Move the game
            var newPath = Path.Combine(_secondRootFolder, addedGame.Title);
            Directory.CreateDirectory(newPath);

            var moveCommand = Commands.PostAndWait(new MoveGameCommand
            {
                GameId = addedGame.Id,
                SourcePath = addedGame.Path,
                DestinationPath = newPath
            });
            moveCommand.Status.Should().Be(NzbDrone.Core.Messaging.Commands.CommandStatus.Completed);

            // 7. Verify move succeeded
            var movedGame = Games.Get(addedGame.Id);
            movedGame.Path.Should().Be(newPath);

            // 8. Clean up
            Games.Delete(addedGame.Id);
            RootFolders.Delete(rootFolder.Id);
            Games.All().Should().NotContain(g => g.IgdbId == 38);
        }
    }
}
