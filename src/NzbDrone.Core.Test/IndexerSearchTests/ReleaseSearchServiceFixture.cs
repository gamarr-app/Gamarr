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
using NzbDrone.Test.Common;

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

        private void GivenDecisions(params DownloadDecision[] decisions)
        {
            Mocker.GetMock<IMakeDownloadDecision>()
                .Setup(s => s.GetSearchDecision(It.IsAny<List<Parser.Model.ReleaseInfo>>(), It.IsAny<SearchCriteriaBase>()))
                .Returns(decisions.ToList());
        }

        private DownloadDecision GivenUnparseableDecision(string guid, string title, long size)
        {
            var remoteGame = new RemoteGame
            {
                Release = new Parser.Model.ReleaseInfo { Guid = guid, Title = title, Size = size },
                ParsedGameInfo = new ParsedGameInfo()
            };

            return new DownloadDecision(remoteGame, new DownloadRejection(DownloadRejectionReason.UnableToParse, "Unable to parse release"));
        }

        private DownloadDecision GivenAcceptedDecision(string guid, string title, long size)
        {
            var remoteGame = new RemoteGame
            {
                Game = _game,
                Release = new Parser.Model.ReleaseInfo { Guid = guid, Title = title, Size = size },
                ParsedGameInfo = new ParsedGameInfo()
            };

            return new DownloadDecision(remoteGame);
        }

        // A feed that reports a blank <guid> used to give every release the same de-dupe
        // identity, so one unparseable release took out all of its clean siblings.
        [Test]
        public async Task should_not_drop_parseable_releases_sharing_a_blank_guid_with_an_unparseable_one()
        {
            WatchForSearchCriteria();

            GivenDecisions(
                GivenUnparseableDecision(string.Empty, "Some.Garbled.Release", 100),
                GivenAcceptedDecision(string.Empty, "Clean Game Release GROUP", 200),
                GivenAcceptedDecision(string.Empty, "Clean Game Release OTHERGROUP", 300));

            var result = await Subject.GameSearch(_game, true, false);

            result.Should().HaveCount(3);
            result.Count(d => d.Approved).Should().Be(2);
        }

        // Same title, different releases: two repacks of one game differ in size, and both
        // have to survive even when the unparseable one carries the same title.
        [Test]
        public async Task should_not_drop_same_title_releases_that_differ_in_size()
        {
            WatchForSearchCriteria();

            GivenDecisions(
                GivenUnparseableDecision(string.Empty, "Clean Game Release", 100),
                GivenAcceptedDecision(string.Empty, "Clean Game Release", 200));

            var result = await Subject.GameSearch(_game, true, false);

            result.Should().HaveCount(2);
        }

        // The real duplicate case must keep working: the same release returned twice is
        // identical in guid, title and size, and is still collapsed to one row.
        [Test]
        public async Task should_still_de_dupe_identical_reports()
        {
            WatchForSearchCriteria();

            GivenDecisions(
                GivenAcceptedDecision("abc", "Clean Game Release", 200),
                GivenAcceptedDecision("abc", "Clean Game Release", 200));

            var result = await Subject.GameSearch(_game, true, false);

            result.Should().HaveCount(1);
        }
    }
}
