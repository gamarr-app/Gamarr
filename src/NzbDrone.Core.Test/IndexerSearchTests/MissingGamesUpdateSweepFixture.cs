using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    /// <summary>
    /// "Search missing" never looked at a game's updates: a game stops being
    /// missing the moment its base file arrives, and an update is never
    /// missing because an update slot only exists after an update imported
    /// (#210). These cover the sweep that closes that hole.
    /// </summary>
    [TestFixture]
    public class MissingGamesUpdateSweepFixture : CoreTest<GameSearchService>
    {
        private List<Game> _games;
        private List<GameFile> _files;

        [SetUp]
        public void SetUp()
        {
            _games = new List<Game>
            {
                new Game { Id = 1, Monitored = true, MonitorUpdates = true, GameMetadata = new GameMetadata { Title = "One" } }
            };

            _files = new List<GameFile>
            {
                new GameFile { Id = 1, GameId = 1, RelativePath = string.Empty }
            };

            Mocker.GetMock<IGameService>()
                .Setup(s => s.GamesWithoutFiles(It.IsAny<PagingSpec<Game>>()))
                .Returns((PagingSpec<Game> spec) =>
                {
                    spec.Records = new List<Game>();
                    return spec;
                });

            Mocker.GetMock<IGameService>()
                .Setup(s => s.GetAllGames())
                .Returns(() => _games);

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByGames(It.IsAny<IEnumerable<int>>()))
                .Returns(() => _files);

            Mocker.GetMock<IQueueService>()
                .Setup(s => s.GetQueue())
                .Returns(new List<NzbDrone.Core.Queue.Queue>());

            Mocker.GetMock<IGameComponentService>()
                .Setup(s => s.GetMonitoredMissingDlc())
                .Returns(new List<GameComponent>());

            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.GameSearch(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(new List<DownloadDecision>()));

            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(),
                                                                new List<DownloadDecision>(),
                                                                new List<DownloadDecision>())));
        }

        private void ExecuteSearch()
        {
            Subject.Execute(new MissingGamesSearchCommand { Trigger = CommandTrigger.Scheduled });
        }

        private void VerifySwept(int gameId, Times times)
        {
            Mocker.GetMock<IGameUpdateSearchService>()
                .Verify(s => s.SearchForUpdatesAndMissingDlc(gameId, It.IsAny<bool>()), times);
        }

        [Test]
        public void should_sweep_a_game_with_a_base_file_and_no_update()
        {
            ExecuteSearch();

            VerifySwept(1, Times.Once());
        }

        // A game that already carries an update only wants a *newer* one, and a
        // newer one is a new upload — which RSS already sees.
        [Test]
        public void should_not_sweep_a_game_that_already_has_an_update()
        {
            _files.Add(new GameFile { Id = 2, GameId = 1, RelativePath = "Updates/v1.0.2" });

            ExecuteSearch();

            VerifySwept(1, Times.Never());
        }

        [Test]
        public void should_not_sweep_a_game_with_no_base_file()
        {
            _files.Clear();

            ExecuteSearch();

            VerifySwept(1, Times.Never());
        }

        [Test]
        public void should_not_sweep_an_unmonitored_game()
        {
            _games[0].Monitored = false;

            ExecuteSearch();

            VerifySwept(1, Times.Never());
        }

        [Test]
        public void should_not_sweep_a_game_whose_updates_are_not_monitored()
        {
            _games[0].MonitorUpdates = false;

            ExecuteSearch();

            VerifySwept(1, Times.Never());
        }

        [Test]
        public void should_not_sweep_a_game_that_is_already_in_the_queue()
        {
            Mocker.GetMock<IQueueService>()
                .Setup(s => s.GetQueue())
                .Returns(new List<NzbDrone.Core.Queue.Queue> { new NzbDrone.Core.Queue.Queue { Game = _games[0] } });

            ExecuteSearch();

            VerifySwept(1, Times.Never());
        }

        // A library where no game has an update yet is every game in it, and
        // one pass must not put all of them on the indexers at once.
        [Test]
        public void should_cap_the_sweep_and_take_the_least_recently_searched_first()
        {
            _games.Clear();
            _files.Clear();

            for (var id = 1; id <= 60; id++)
            {
                _games.Add(new Game
                {
                    Id = id,
                    Monitored = true,
                    MonitorUpdates = true,
                    LastSearchTime = DateTime.UtcNow.AddDays(-id),
                    GameMetadata = new GameMetadata { Title = $"Game {id}" }
                });

                _files.Add(new GameFile { Id = id, GameId = id, RelativePath = string.Empty });
            }

            ExecuteSearch();

            Mocker.GetMock<IGameUpdateSearchService>()
                .Verify(s => s.SearchForUpdatesAndMissingDlc(It.IsAny<int>(), It.IsAny<bool>()), Times.Exactly(50));

            // Highest id searched longest ago, so it goes first and id 1 - the
            // most recently searched - is the one left for the next pass.
            VerifySwept(60, Times.Once());
            VerifySwept(1, Times.Never());
        }

        [Test]
        public void should_not_sweep_a_game_that_is_still_backed_off()
        {
            Mocker.GetMock<IGameSearchBackoff>()
                .Setup(s => s.ShouldSkip(It.IsAny<Game>()))
                .Returns(true);

            ExecuteSearch();

            VerifySwept(1, Times.Never());
        }
    }
}
