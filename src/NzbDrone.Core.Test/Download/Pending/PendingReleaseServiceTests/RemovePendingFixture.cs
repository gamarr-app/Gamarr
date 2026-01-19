using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Crypto;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.Pending.PendingReleaseServiceTests
{
    [TestFixture]
    public class RemovePendingFixture : CoreTest<PendingReleaseService>
    {
        private List<PendingRelease> _pending;
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _pending = new List<PendingRelease>();

            _game = Builder<Game>.CreateNew()
                                       .Build();

            Mocker.GetMock<IPendingReleaseRepository>()
                 .Setup(s => s.AllByGameId(It.IsAny<int>()))
                 .Returns(_pending);

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(_pending);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(It.IsAny<int>()))
                  .Returns(_game);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGames(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns(_game);
        }

        private void AddPending(int id, string title, int year)
        {
            _pending.Add(new PendingRelease
            {
                Id = id,
                Title = "Game.Title.2020.720p-Gamarr",
                ParsedGameInfo = new ParsedGameInfo { GameTitles = new List<string> { title }, Year = year },
                Release = Builder<ReleaseInfo>.CreateNew().Build(),
                GameId = _game.Id
            });
        }

        [Test]
        public void should_remove_same_release()
        {
            AddPending(id: 1, title: "Game", year: 2001);

            var queueId = HashConverter.GetHashInt31(string.Format("pending-{0}-game{1}", 1, _game.Id));

            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1);
        }

        private void AssertRemoved(params int[] ids)
        {
            Mocker.GetMock<IPendingReleaseRepository>().Verify(c => c.DeleteMany(It.Is<IEnumerable<int>>(s => s.SequenceEqual(ids))));
        }
    }
}
