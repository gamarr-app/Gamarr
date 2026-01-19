#pragma warning disable CS0618
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportList
{
    [TestFixture]
    public class ImportListSyncServiceFixture : CoreTest<ImportListSyncService>
    {
        private ImportListFetchResult _importListFetch;
        private List<ImportListGame> _list1Games;
        private List<ImportListGame> _list2Games;

        private List<Game> _existingGames;
        private List<IImportList> _importLists;
        private ImportListSyncCommand _commandAll;
        private ImportListSyncCommand _commandSingle;

        [SetUp]
        public void Setup()
        {
            _importLists = new List<IImportList>();

            _list1Games = Builder<ImportListGame>.CreateListOfSize(5)
                .Build().ToList();

            _existingGames = Builder<Game>.CreateListOfSize(3)
                .TheFirst(1)
                .With(s => s.IgdbId = 6)
                .TheNext(1)
                .With(s => s.IgdbId = 7)
                .TheNext(1)
                .With(s => s.IgdbId = 8)
                .Build().ToList();

            _list2Games = Builder<ImportListGame>.CreateListOfSize(3)
                .TheFirst(1)
                .With(s => s.IgdbId = 6)
                .TheNext(1)
                .With(s => s.IgdbId = 7)
                .TheNext(1)
                .With(s => s.IgdbId = 8)
                .Build().ToList();

            _importListFetch = new ImportListFetchResult
            {
                Games = _list1Games,
                AnyFailure = false,
                SyncedLists = 1
            };

            _commandAll = new ImportListSyncCommand
            {
            };

            _commandSingle = new ImportListSyncCommand
            {
                DefinitionId = 1
            };

            Mocker.GetMock<IImportListFactory>()
                  .Setup(v => v.Enabled(It.IsAny<bool>()))
                  .Returns(_importLists);

            Mocker.GetMock<IImportListExclusionService>()
                  .Setup(v => v.All())
                  .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.GameExists(It.IsAny<Game>()))
                  .Returns(false);

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.AllGameIgdbIds())
                  .Returns(new List<int>());

            Mocker.GetMock<IFetchAndParseImportList>()
                  .Setup(v => v.Fetch())
                  .Returns(_importListFetch);
        }

        private void GivenListFailure()
        {
            _importListFetch.AnyFailure = true;
        }

        private void GivenNoListSync()
        {
            _importListFetch.SyncedLists = 0;
        }

        private void GivenCleanLevel(string cleanLevel)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(v => v.ListSyncLevel)
                  .Returns(cleanLevel);
        }

        private void GivenList(int id, bool enabledAuto)
        {
            var importListDefinition = new ImportListDefinition { Id = id, EnableAuto = enabledAuto };

            Mocker.GetMock<IImportListFactory>()
                  .Setup(v => v.Get(id))
                  .Returns(importListDefinition);

            CreateListResult(id, enabledAuto);
        }

        private Mock<IImportList> CreateListResult(int id, bool enabledAuto)
        {
            var importListDefinition = new ImportListDefinition { Id = id, EnableAuto = enabledAuto };

            var mockImportList = new Mock<IImportList>();
            mockImportList.SetupGet(s => s.Definition).Returns(importListDefinition);
            mockImportList.SetupGet(s => s.Enabled).Returns(true);
            mockImportList.SetupGet(s => s.EnableAuto).Returns(enabledAuto);

            _importLists.Add(mockImportList.Object);

            return mockImportList;
        }

        [Test]
        public void should_not_clean_library_if_config_value_disable()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel("disabled");

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.GetAllGames(), Times.Never());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(new List<Game>(), true), Times.Never());
        }

        [Test]
        public void should_not_clean_library_or_process_games_if_no_synced_lists()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel("logOnly");
            GivenNoListSync();

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.GetAllGames(), Times.Never());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(new List<Game>(), true), Times.Never());

            Mocker.GetMock<IImportListExclusionService>()
                  .Verify(v => v.All(), Times.Never);
        }

        [Test]
        public void should_log_only_on_clean_library_if_config_value_logonly()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel("logOnly");

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.GetAllGames())
                  .Returns(_existingGames);

            Mocker.GetMock<IImportListGameService>()
                .Setup(v => v.GetAllListGames())
                .Returns(_list1Games);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.GetAllGames(), Times.Once());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.DeleteGame(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(new List<Game>(), true), Times.Once());
        }

        [Test]
        public void should_unmonitor_on_clean_library_if_config_value_keepAndUnmonitor()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel("keepAndUnmonitor");

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.GetAllGames())
                  .Returns(_existingGames);

            Mocker.GetMock<IImportListGameService>()
                .Setup(v => v.GetAllListGames())
                .Returns(_list1Games);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.GetAllGames(), Times.Once());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.DeleteGame(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(It.Is<List<Game>>(s => s.Count == 3 && s.All(m => !m.Monitored)), true), Times.Once());
        }

        [Test]
        public void should_not_clean_on_clean_library_if_igdb_match()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            _importListFetch.Games[0].IgdbId = 6;

            GivenList(1, true);
            GivenCleanLevel("keepAndUnmonitor");

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.GetAllGames())
                  .Returns(_existingGames);

            Mocker.GetMock<IImportListGameService>()
                .Setup(v => v.GetAllListGames())
                .Returns(_list1Games);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(It.Is<List<Game>>(s => s.Count == 2 && s.All(m => !m.Monitored)), true), Times.Once());
        }

        [Test]
        public void should_delete_games_not_files_on_clean_library_if_config_value_logonly()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel("removeAndKeep");

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.GetAllGames())
                  .Returns(_existingGames);

            Mocker.GetMock<IImportListGameService>()
                .Setup(v => v.GetAllListGames())
                .Returns(_list1Games);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.GetAllGames(), Times.Once());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.DeleteGame(It.IsAny<int>(), false, It.IsAny<bool>()), Times.Exactly(3));

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.DeleteGame(It.IsAny<int>(), true, It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(new List<Game>(), true), Times.Once());
        }

        [Test]
        public void should_delete_games_and_files_on_clean_library_if_config_value_logonly()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel("removeAndDelete");

            Mocker.GetMock<IGameService>()
                  .Setup(v => v.GetAllGames())
                  .Returns(_existingGames);

            Mocker.GetMock<IImportListGameService>()
                .Setup(v => v.GetAllListGames())
                .Returns(_list1Games);

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.GetAllGames(), Times.Once());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.DeleteGame(It.IsAny<int>(), false, It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.DeleteGame(It.IsAny<int>(), true, It.IsAny<bool>()), Times.Exactly(3));

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(new List<Game>(), true), Times.Once());
        }

        [Test]
        public void should_not_clean_if_list_failures()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenListFailure();

            GivenList(1, true);
            GivenCleanLevel("disabled");

            Subject.Execute(_commandAll);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(new List<Game>(), true), Times.Never());
        }

        [Test]
        public void should_add_new_games_from_single_list_to_library()
        {
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            GivenList(1, true);
            GivenCleanLevel("disabled");

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(s => s.Count == 5), true), Times.Once());
        }

        [Test]
        public void should_add_new_games_from_multiple_list_to_library()
        {
            _list2Games.ForEach(m => m.ListId = 2);
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            _importListFetch.Games.AddRange(_list2Games);

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel("disabled");

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(s => s.Count == 8), true), Times.Once());
        }

        [Test]
        public void should_add_new_games_from_enabled_lists_to_library()
        {
            _list2Games.ForEach(m => m.ListId = 2);
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            _importListFetch.Games.AddRange(_list2Games);

            GivenList(1, true);
            GivenList(2, false);

            GivenCleanLevel("disabled");

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(s => s.Count == 5), true), Times.Once());
        }

        [Test]
        public void should_not_add_duplicate_games_from_separate_lists()
        {
            _list2Games.ForEach(m => m.ListId = 2);
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            _importListFetch.Games.AddRange(_list2Games);
            _importListFetch.Games[0].IgdbId = 4;

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel("disabled");

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(s => s.Count == 7), true), Times.Once());
        }

        [Test]
        public void should_not_add_game_from_on_exclusion_list()
        {
            _list2Games.ForEach(m => m.ListId = 2);
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            _importListFetch.Games.AddRange(_list2Games);

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel("disabled");

            Mocker.GetMock<IImportListExclusionService>()
                  .Setup(v => v.All())
                  .Returns(new List<ImportListExclusion> { new ImportListExclusion { IgdbId = _existingGames[0].IgdbId } });

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(s => s.Count == 7 && s.All(m => m.IgdbId != _existingGames[0].IgdbId)), true), Times.Once());
        }

        [Test]
        public void should_not_add_game_that_exists_in_library()
        {
            _list2Games.ForEach(m => m.ListId = 2);
            _importListFetch.Games.ForEach(m => m.ListId = 1);
            _importListFetch.Games.AddRange(_list2Games);

            GivenList(1, true);
            GivenList(2, true);

            GivenCleanLevel("disabled");

            Mocker.GetMock<IGameService>()
                 .Setup(v => v.AllGameIgdbIds())
                 .Returns(new List<int> { _existingGames[0].IgdbId });

            Subject.Execute(_commandAll);

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(s => s.Count == 7 && s.All(m => m.IgdbId != _existingGames[0].IgdbId)), true), Times.Once());
        }
    }
}
