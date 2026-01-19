using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Notifications.Xbmc;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests.Xbmc
{
    [TestFixture]
    public class GetGamePathFixture : CoreTest<XbmcService>
    {
        private const string IMDB_ID = "tt67890";
        private XbmcSettings _settings;
        private Game _game;
        private List<XbmcGame> _xbmcGames;

        [SetUp]
        public void Setup()
        {
            _settings = Builder<XbmcSettings>.CreateNew()
                                             .Build();

            _xbmcGames = Builder<XbmcGame>.CreateListOfSize(3)
                                            .All()
                                            .With(s => s.ImdbNumber = "tt00000")
                                            .TheFirst(1)
                                            .With(s => s.ImdbNumber = IMDB_ID)
                                            .Build()
                                            .ToList();

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetGames(_settings))
                  .Returns(_xbmcGames);
        }

        private void GivenMatchingImdbId()
        {
            _game = new Game
            {
                ImdbId = IMDB_ID,
                Title = "Game"
            };
        }

        private void GivenMatchingTitle()
        {
            _game = new Game
            {
                ImdbId = "tt01000",
                Title = _xbmcGames.First().Label
            };
        }

        private void GivenMatchingGame()
        {
            _game = new Game
            {
                ImdbId = "tt01000",
                Title = "Does not exist"
            };
        }

        [Test]
        public void should_return_null_when_game_is_not_found()
        {
            GivenMatchingGame();

            Subject.GetGamePath(_settings, _game).Should().BeNull();
        }

        [Test]
        public void should_return_path_when_tvdbId_matches()
        {
            GivenMatchingImdbId();

            Subject.GetGamePath(_settings, _game).Should().Be(_xbmcGames.First().File);
        }
    }
}
