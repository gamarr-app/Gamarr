using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.GameTests.GameServiceTests
{
    [TestFixture]
    public class UpdateMultipleGamesFixture : CoreTest<GameService>
    {
        private List<Game> _games;

        [SetUp]
        public void Setup()
        {
            _games = Builder<Game>.CreateListOfSize(5)
                .All()
                .With(s => s.QualityProfileId = 1)
                .With(s => s.Monitored)
                .With(s => s.Path = @"C:\Test\name".AsOsAgnostic())
                .With(s => s.RootFolderPath = "")
                .Build().ToList();

            Mocker.GetMock<IAutoTaggingService>()
                .Setup(s => s.GetTagChanges(It.IsAny<Game>()))
                .Returns(new AutoTaggingChanges());
        }

        [Test]
        public void should_call_repo_updateMany()
        {
            Subject.UpdateGame(_games, false);

            Mocker.GetMock<IGameRepository>().Verify(v => v.UpdateMany(_games), Times.Once());
        }

        [Test]
        public void should_update_path_when_rootFolderPath_is_supplied()
        {
            var newRoot = @"C:\Test\TV2".AsOsAgnostic();
            _games.ForEach(s => s.RootFolderPath = newRoot);

            Mocker.GetMock<IBuildGamePaths>()
                .Setup(s => s.BuildPath(It.IsAny<Game>(), false))
                .Returns<Game, bool>((s, u) => Path.Combine(s.RootFolderPath, s.Title));

            Subject.UpdateGame(_games, false).ForEach(s => s.Path.Should().StartWith(newRoot));
        }

        [Test]
        public void should_not_update_path_when_rootFolderPath_is_empty()
        {
            Subject.UpdateGame(_games, false).ForEach(s =>
            {
                var expectedPath = _games.Single(ser => ser.Id == s.Id).Path;
                s.Path.Should().Be(expectedPath);
            });
        }

        [Test]
        public void should_be_able_to_update_many_games()
        {
            var games = Builder<Game>.CreateListOfSize(50)
                                        .All()
                                        .With(s => s.Path = (@"C:\Test\Games\" + s.Path).AsOsAgnostic())
                                        .Build()
                                        .ToList();

            var newRoot = @"C:\Test\Games2".AsOsAgnostic();
            games.ForEach(s => s.RootFolderPath = newRoot);

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetGameFolder(It.IsAny<Game>(), (NamingConfig)null))
                  .Returns<Game, NamingConfig>((s, n) => s.Title);

            Subject.UpdateGame(games, false);
        }

        [Test]
        public void should_add_and_remove_tags()
        {
            _games[0].Tags = new HashSet<int> { 1, 2 };

            Mocker.GetMock<IAutoTaggingService>()
                .Setup(s => s.GetTagChanges(_games[0]))
                .Returns(new AutoTaggingChanges
                {
                    TagsToAdd = new HashSet<int> { 3 },
                    TagsToRemove = new HashSet<int> { 1 }
                });

            var result = Subject.UpdateGame(_games, false);

            result[0].Tags.Should().BeEquivalentTo(new[] { 2, 3 });
        }
    }
}
