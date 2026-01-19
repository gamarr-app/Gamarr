using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Notifications.Xbmc;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests.Xbmc
{
    [TestFixture]
    public class UpdateGameFixture : CoreTest<XbmcService>
    {
        private const string IMDB_ID = "tt67890";
        private XbmcSettings _settings;
        private List<XbmcGame> _xbmcGames;

        [SetUp]
        public void Setup()
        {
            _settings = Builder<XbmcSettings>.CreateNew()
                                             .Build();

            _xbmcGames = Builder<XbmcGame>.CreateListOfSize(3)
                                            .TheFirst(1)
                                            .With(s => s.ImdbNumber = IMDB_ID)
                                            .Build()
                                            .ToList();

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetGames(_settings))
                  .Returns(_xbmcGames);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetActivePlayers(_settings))
                  .Returns(new List<ActivePlayer>());
        }

        [Test]
        public void should_update_using_game_path()
        {
            var game = Builder<Game>.CreateNew()
                                      .With(s => s.ImdbId = IMDB_ID)
                                      .Build();

            Subject.Update(_settings, game);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Verify(v => v.UpdateLibrary(_settings, It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_update_all_paths_when_game_path_not_found()
        {
            var fakeGame = Builder<Game>.CreateNew()
                                          .With(s => s.ImdbId = "tt01000")
                                          .With(s => s.Title = "Not A Real Game")
                                          .Build();

            Subject.Update(_settings, fakeGame);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Verify(v => v.UpdateLibrary(_settings, null), Times.Once());
        }
    }
}
