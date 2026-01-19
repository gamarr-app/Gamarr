using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.GameServiceTests
{
    [TestFixture]
    public class UpdateGameFixture : CoreTest<GameService>
    {
        private Game _fakeGame;
        private Game _existingGame;

        [SetUp]
        public void Setup()
        {
            _fakeGame = Builder<Game>.CreateNew().Build();
            _existingGame = Builder<Game>.CreateNew().Build();

            Mocker.GetMock<IAutoTaggingService>()
                .Setup(s => s.GetTagChanges(It.IsAny<Game>()))
                .Returns(new AutoTaggingChanges());

            Mocker.GetMock<IGameRepository>()
                .Setup(s => s.Update(It.IsAny<Game>()))
                .Returns<Game>(r => r);
        }

        private void GivenExistingGame()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(It.IsAny<int>()))
                  .Returns(_existingGame);
        }

        [Test]
        public void should_update_game_when_it_changes()
        {
            GivenExistingGame();

            Subject.UpdateGame(_fakeGame);

            Mocker.GetMock<IGameRepository>()
                  .Verify(v => v.Update(_fakeGame), Times.Once());
        }

        [Test]
        public void should_add_and_remove_tags()
        {
            GivenExistingGame();

            _fakeGame.Tags = new HashSet<int> { 1, 2 };
            _fakeGame.Monitored = false;

            Mocker.GetMock<IAutoTaggingService>()
                .Setup(s => s.GetTagChanges(_fakeGame))
                .Returns(new AutoTaggingChanges
                {
                    TagsToAdd = new HashSet<int> { 3 },
                    TagsToRemove = new HashSet<int> { 1 }
                });

            var result = Subject.UpdateGame(_fakeGame);

            result.Tags.Should().BeEquivalentTo(new[] { 2, 3 });
        }
    }
}
