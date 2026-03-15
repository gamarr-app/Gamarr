using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
        public class ReleaseSearchServiceFixture : CoreTest<ReleaseSearchService>
    {
        private Mock<IIndexer> _mockIndexer;
        private Game _game;

        [SetUp]
        public void SetUp()
        {
            _mockIndexer = Mocker.GetMock<IIndexer>();
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition { Id = 1 });
            _mockIndexer.SetupGet(s => s.SupportsSearch).Returns(true);

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.AutomaticSearchEnabled(true))
                  .Returns(new List<IIndexer> { _mockIndexer.Object });

            Mocker.GetMock<IMakeDownloadDecision>()
                .Setup(s => s.GetSearchDecision(It.IsAny<List<Parser.Model.ReleaseInfo>>(), It.IsAny<SearchCriteriaBase>()))
                .Returns(new List<DownloadDecision>());

            _game = Builder<Game>.CreateNew()
                .With(v => v.Monitored = true)
                .Build();

            Mocker.GetMock<IGameService>()
                .Setup(v => v.GetGame(_game.Id))
                .Returns(_game);

            Mocker.GetMock<IGameTranslationService>()
                .Setup(s => s.GetAllTranslationsForGameMetadata(It.IsAny<int>()))
                .Returns(new List<GameTranslation>());
        }

        private List<SearchCriteriaBase> WatchForSearchCriteria()
        {
            var result = new List<SearchCriteriaBase>();

            _mockIndexer.Setup(v => v.Fetch(It.IsAny<GameSearchCriteria>()))
                .Callback<GameSearchCriteria>(s => result.Add(s))
                .Returns(Task.FromResult<IList<Parser.Model.ReleaseInfo>>(new List<Parser.Model.ReleaseInfo>()));

            return result;
        }

        [Test]
        public async Task Tags_IndexerTags_GameNoTags_IndexerNotIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1,
                Tags = new HashSet<int> { 3 }
            });

            var allCriteria = WatchForSearchCriteria();

            await Subject.GameSearch(_game, true, false);

            var criteria = allCriteria.OfType<GameSearchCriteria>().ToList();

            criteria.Count.Should().Be(0);
        }

        [Test]
        public async Task Tags_IndexerNoTags_GameTags_IndexerIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1
            });

            _game = Builder<Game>.CreateNew()
                .With(v => v.Monitored = true)
                .With(v => v.Tags = new HashSet<int> { 3 })
                .Build();

            Mocker.GetMock<IGameService>()
                .Setup(v => v.GetGame(_game.Id))
                .Returns(_game);

            var allCriteria = WatchForSearchCriteria();

            await Subject.GameSearch(_game, true, false);

            var criteria = allCriteria.OfType<GameSearchCriteria>().ToList();

            criteria.Count.Should().Be(1);
        }

        [Test]
        public async Task Tags_IndexerAndGameTagsMatch_IndexerIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1,
                Tags = new HashSet<int> { 1, 2, 3 }
            });

            _game = Builder<Game>.CreateNew()
                .With(v => v.Monitored = true)
                .With(v => v.Tags = new HashSet<int> { 3, 4, 5 })
                .Build();

            Mocker.GetMock<IGameService>()
                .Setup(v => v.GetGame(_game.Id))
                .Returns(_game);

            var allCriteria = WatchForSearchCriteria();

            await Subject.GameSearch(_game, true, false);

            var criteria = allCriteria.OfType<GameSearchCriteria>().ToList();

            criteria.Count.Should().Be(1);
        }

        [Test]
        public async Task Tags_IndexerAndGameTagsMismatch_IndexerNotIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1,
                Tags = new HashSet<int> { 1, 2, 3 }
            });

            _game = Builder<Game>.CreateNew()
                .With(v => v.Monitored = true)
                .With(v => v.Tags = new HashSet<int> { 4, 5, 6 })
                .Build();

            Mocker.GetMock<IGameService>()
                .Setup(v => v.GetGame(_game.Id))
                .Returns(_game);

            var allCriteria = WatchForSearchCriteria();

            await Subject.GameSearch(_game, true, false);

            var criteria = allCriteria.OfType<GameSearchCriteria>().ToList();

            criteria.Count.Should().Be(0);
        }

        [Test]
        public async Task should_search_indexer_and_return_decisions()
        {
            var release = new ReleaseInfo { Title = "Test Game", Guid = "guid-1" };

            _mockIndexer.Setup(v => v.Fetch(It.IsAny<GameSearchCriteria>()))
                .Returns(Task.FromResult<IList<ReleaseInfo>>(new List<ReleaseInfo> { release }));

            Mocker.GetMock<IMakeDownloadDecision>()
                .Setup(s => s.GetSearchDecision(It.IsAny<List<ReleaseInfo>>(), It.IsAny<SearchCriteriaBase>()))
                .Returns(new List<DownloadDecision>
                {
                    new DownloadDecision(new RemoteGame { Release = release })
                });

            var result = await Subject.GameSearch(_game, true, false);

            result.Should().HaveCount(1);
        }

        [Test]
        public async Task should_deduplicate_results_by_guid()
        {
            var release = new ReleaseInfo { Title = "Test Game", Guid = "same-guid" };

            _mockIndexer.Setup(v => v.Fetch(It.IsAny<GameSearchCriteria>()))
                .Returns(Task.FromResult<IList<ReleaseInfo>>(new List<ReleaseInfo> { release }));

            Mocker.GetMock<IMakeDownloadDecision>()
                .Setup(s => s.GetSearchDecision(It.IsAny<List<ReleaseInfo>>(), It.IsAny<SearchCriteriaBase>()))
                .Returns(new List<DownloadDecision>
                {
                    new DownloadDecision(new RemoteGame { Release = new ReleaseInfo { Guid = "same-guid" } }),
                    new DownloadDecision(new RemoteGame { Release = new ReleaseInfo { Guid = "same-guid" } })
                });

            var result = await Subject.GameSearch(_game, true, false);

            result.Should().HaveCount(1);
        }

        [Test]
        public async Task should_update_last_search_time()
        {
            WatchForSearchCriteria();

            await Subject.GameSearch(_game, true, false);

            Mocker.GetMock<IGameService>()
                .Verify(s => s.UpdateLastSearchTime(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public async Task should_use_interactive_indexers_for_interactive_search()
        {
            Mocker.GetMock<IIndexerFactory>()
                .Setup(s => s.InteractiveSearchEnabled(It.IsAny<bool>()))
                .Returns(new List<IIndexer> { _mockIndexer.Object });

            WatchForSearchCriteria();

            await Subject.GameSearch(_game, true, true);

            Mocker.GetMock<IIndexerFactory>()
                .Verify(s => s.InteractiveSearchEnabled(It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public async Task should_handle_indexer_error_gracefully()
        {
            _mockIndexer.Setup(v => v.Fetch(It.IsAny<GameSearchCriteria>()))
                .Throws(new Exception("Indexer down"));

            var result = await Subject.GameSearch(_game, true, false);

            result.Should().BeEmpty();

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public async Task should_lookup_game_by_id()
        {
            WatchForSearchCriteria();

            await Subject.GameSearch(_game.Id, true, false);

            Mocker.GetMock<IGameService>()
                .Verify(s => s.GetGame(_game.Id), Times.Once());
        }
    }
}
