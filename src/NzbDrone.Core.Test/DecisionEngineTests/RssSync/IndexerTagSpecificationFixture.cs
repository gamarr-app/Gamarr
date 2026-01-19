using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class IndexerTagSpecificationFixture : CoreTest<IndexerTagSpecification>
    {
        private IndexerTagSpecification _specification;

        private RemoteGame _parseResultMulti;
        private IndexerDefinition _fakeIndexerDefinition;
        private Game _fakeGame;
        private ReleaseInfo _fakeRelease;

        [SetUp]
        public void Setup()
        {
            _fakeIndexerDefinition = new IndexerDefinition
            {
                Tags = new HashSet<int>()
            };

            Mocker
                .GetMock<IIndexerFactory>()
                .Setup(m => m.Get(It.IsAny<int>()))
                .Throws(new ModelNotFoundException(typeof(IndexerDefinition), -1));

            Mocker
                .GetMock<IIndexerFactory>()
                .Setup(m => m.Get(1))
                .Returns(_fakeIndexerDefinition);

            _specification = Mocker.Resolve<IndexerTagSpecification>();

            _fakeGame = Builder<Game>.CreateNew()
                .With(c => c.Monitored = true)
                .With(c => c.Tags = new HashSet<int>())
                .Build();

            _fakeRelease = new ReleaseInfo
            {
                IndexerId = 1
            };

            _parseResultMulti = new RemoteGame
            {
                Game = _fakeGame,
                Release = _fakeRelease
            };
        }

        [Test]
        public void indexer_and_game_without_tags_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int>();
            _fakeGame.Tags = new HashSet<int>();

            _specification.IsSatisfiedBy(_parseResultMulti, new GameSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void indexer_with_tags_game_without_tags_should_return_false()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 123 };
            _fakeGame.Tags = new HashSet<int>();

            _specification.IsSatisfiedBy(_parseResultMulti, new GameSearchCriteria()).Accepted.Should().BeFalse();
        }

        [Test]
        public void indexer_without_tags_game_with_tags_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int>();
            _fakeGame.Tags = new HashSet<int> { 123 };

            _specification.IsSatisfiedBy(_parseResultMulti, new GameSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void indexer_with_tags_game_with_matching_tags_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 123, 456 };
            _fakeGame.Tags = new HashSet<int> { 123, 789 };

            _specification.IsSatisfiedBy(_parseResultMulti, new GameSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void indexer_with_tags_game_with_different_tags_should_return_false()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 456 };
            _fakeGame.Tags = new HashSet<int> { 123, 789 };

            _specification.IsSatisfiedBy(_parseResultMulti, new GameSearchCriteria()).Accepted.Should().BeFalse();
        }

        [Test]
        public void release_without_indexerid_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 456 };
            _fakeGame.Tags = new HashSet<int> { 123, 789 };
            _fakeRelease.IndexerId = 0;

            _specification.IsSatisfiedBy(_parseResultMulti, new GameSearchCriteria { MonitoredEpisodesOnly = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void release_with_invalid_indexerid_should_return_true()
        {
            _fakeIndexerDefinition.Tags = new HashSet<int> { 456 };
            _fakeGame.Tags = new HashSet<int> { 123, 789 };
            _fakeRelease.IndexerId = 2;

            _specification.IsSatisfiedBy(_parseResultMulti, new GameSearchCriteria { MonitoredEpisodesOnly = true }).Accepted.Should().BeTrue();
        }
    }
}
