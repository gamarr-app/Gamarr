using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class GameSearchServiceFixture : CoreTest<GameSearchService>
    {
        private Game _deleted;
        private Game _survivor;

        [SetUp]
        public void SetUp()
        {
            var games = Builder<Game>.CreateListOfSize(2)
                .All()
                .With(g => g.Monitored = true)
                .BuildList()
                .ToList();

            _deleted = games[0];
            _survivor = games[1];

            Mocker.GetMock<IGameService>()
                .Setup(s => s.GetGames(It.IsAny<IEnumerable<int>>()))
                .Returns(games);

            // Both still present unless a test deletes one.
            Mocker.GetMock<IGameService>()
                .Setup(s => s.FindGame(It.IsAny<int>()))
                .Returns((int id) => games.Find(g => g.Id == id));

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
            Subject.Execute(new GamesSearchCommand
            {
                GameIds = new List<int> { _deleted.Id, _survivor.Id },
                Trigger = CommandTrigger.Manual
            });
        }

        // The game list is loaded when the command starts, so a search long
        // enough to matter is a search the user can delete a game during.
        [Test]
        public void should_skip_a_game_deleted_after_the_command_started()
        {
            Mocker.GetMock<IGameService>()
                .Setup(s => s.FindGame(_deleted.Id))
                .Returns((Game)null);

            ExecuteSearch();

            Mocker.GetMock<ISearchForReleases>()
                .Verify(s => s.GameSearch(_deleted.Id, It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<ISearchForReleases>()
                .Verify(s => s.GameSearch(_survivor.Id, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());
        }

        // The existence check cannot close the window completely — deletion can
        // land between it and the lookup inside the search itself.
        [Test]
        public void should_not_log_an_error_when_the_game_vanishes_mid_search()
        {
            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.GameSearch(_deleted.Id, It.IsAny<bool>(), It.IsAny<bool>()))
                .Throws(new ModelNotFoundException(typeof(Game), _deleted.Id));

            ExecuteSearch();

            Mocker.GetMock<ISearchForReleases>()
                .Verify(s => s.GameSearch(_survivor.Id, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());
        }

        // Anything else is still a real failure worth reporting, and must not
        // stop the games after it in the list.
        [Test]
        public void should_log_an_error_and_continue_on_any_other_failure()
        {
            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.GameSearch(_deleted.Id, It.IsAny<bool>(), It.IsAny<bool>()))
                .Throws(new System.InvalidOperationException("indexer exploded"));

            ExecuteSearch();

            Mocker.GetMock<ISearchForReleases>()
                .Verify(s => s.GameSearch(_survivor.Id, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
