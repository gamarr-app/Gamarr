using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.Search;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests.Search
{
    [TestFixture]
    public class GameSpecificationFixture : TestBase<GameSpecification>
    {
        private Game _game1;
        private Game _game2;
        private RemoteGame _remoteEpisode = new RemoteGame();
        private SearchCriteriaBase _searchCriteria = new GameSearchCriteria();

        [SetUp]
        public void Setup()
        {
            _game1 = Builder<Game>.CreateNew().With(s => s.Id = 1).Build();
            _game2 = Builder<Game>.CreateNew().With(s => s.Id = 2).Build();

            _remoteEpisode.Game = _game1;
        }

        [Test]
        public void should_return_false_if_series_doesnt_match()
        {
            _searchCriteria.Game = _game2;

            Subject.IsSatisfiedBy(_remoteEpisode, _searchCriteria).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_series_ids_match()
        {
            _searchCriteria.Game = _game1;

            Subject.IsSatisfiedBy(_remoteEpisode, _searchCriteria).Accepted.Should().BeTrue();
        }
    }
}
