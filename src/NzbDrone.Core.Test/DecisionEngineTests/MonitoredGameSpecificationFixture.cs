using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class MonitoredGameSpecificationFixture : CoreTest<MonitoredGameSpecification>
    {
        private MonitoredGameSpecification _monitoredEpisodeSpecification;

        private RemoteGame _parseResultMulti;
        private RemoteGame _parseResultSingle;
        private Game _fakeSeries;
        private Game _firstEpisode;
        private Game _secondEpisode;

        [SetUp]
        public void Setup()
        {
            _monitoredEpisodeSpecification = Mocker.Resolve<MonitoredGameSpecification>();

            _fakeSeries = Builder<Game>.CreateNew()
                .With(c => c.Monitored = true)
                .Build();

            _firstEpisode = new Game() { Monitored = true };
            _secondEpisode = new Game() { Monitored = true };

            var singleEpisodeList = new List<Game> { _firstEpisode };
            var doubleEpisodeList = new List<Game> { _firstEpisode, _secondEpisode };

            _parseResultMulti = new RemoteGame
            {
                Game = _fakeSeries
            };

            _parseResultSingle = new RemoteGame
            {
                Game = _fakeSeries
            };
        }

        private void WithGameUnmonitored()
        {
            _fakeSeries.Monitored = false;
        }

        [Test]
        public void setup_should_return_monitored_episode_should_return_true()
        {
            _monitoredEpisodeSpecification.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
            _monitoredEpisodeSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void not_monitored_series_should_be_skipped()
        {
            _fakeSeries.Monitored = false;
            _monitoredEpisodeSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void only_episode_not_monitored_should_return_false()
        {
            WithGameUnmonitored();
            _monitoredEpisodeSpecification.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_for_single_episode_search()
        {
            _fakeSeries.Monitored = false;
            _monitoredEpisodeSpecification.IsSatisfiedBy(_parseResultSingle, new GameSearchCriteria { UserInvokedSearch = true }).Accepted.Should().BeTrue();
        }
    }
}
