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
        private const int IGDB_ID = 12345;
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
                                            .With(s => s.IgdbId = 0)
                                            .TheFirst(1)
                                            .With(s => s.IgdbId = IGDB_ID)
                                            .Build()
                                            .ToList();

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetGames(_settings))
                  .Returns(_xbmcGames);
        }

        private void GivenMatchingIgdbId()
        {
            _game = new Game
            {
                GameMetadataId = 1,
                Title = "Game"
            };
            _game.GameMetadata = new GameMetadata { IgdbId = IGDB_ID };
        }

        private void GivenMatchingTitle()
        {
            _game = new Game
            {
                GameMetadataId = 1,
                Title = _xbmcGames.First().Label
            };
            _game.GameMetadata = new GameMetadata { IgdbId = 99999 };
        }

        private void GivenNoMatchingGame()
        {
            _game = new Game
            {
                GameMetadataId = 1,
                Title = "Does not exist"
            };
            _game.GameMetadata = new GameMetadata { IgdbId = 99999 };
        }

        [Test]
        public void should_return_null_when_game_is_not_found()
        {
            GivenNoMatchingGame();

            Subject.GetGamePath(_settings, _game).Should().BeNull();
        }

        [Test]
        public void should_return_path_when_igdbId_matches()
        {
            GivenMatchingIgdbId();

            Subject.GetGamePath(_settings, _game).Should().Be(_xbmcGames.First().File);
        }
    }
}
