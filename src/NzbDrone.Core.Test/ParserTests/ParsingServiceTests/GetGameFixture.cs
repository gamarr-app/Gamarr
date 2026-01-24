using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class GetGameFixture : CoreTest<ParsingService>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                .With(m => m.Title = "Cyberpunk 2077")
                .With(m => m.GameMetadata.Value.CleanTitle = "cyberpunk2077")
                .With(m => m.Year = 2020)
                .Build();
        }

        [Test]
        public void should_use_passed_in_title_when_it_cannot_be_parsed()
        {
            const string title = "xK9#mZ2@";

            Subject.GetGame(title);

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.FindByTitle(title), Times.Once());
        }

        [Test]
        public void should_use_parsed_game_title()
        {
            const string title = "30.Game.2015.720p.hdtv";

            Subject.GetGame(title);

            Mocker.GetMock<IGameService>()
                .Verify(s => s.FindByTitle(Parser.Parser.ParseGameTitle(title, false).GameTitles, It.IsAny<int>(), It.IsAny<List<string>>(), null), Times.Once());
        }

        [Test]
        public void should_return_game_when_found_by_title()
        {
            const string title = "Cyberpunk.2077.v2.1-CODEX";

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitleCandidates(It.IsAny<List<string>>(), out It.Ref<List<string>>.IsAny))
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(It.IsAny<List<string>>(), It.IsAny<int?>(), It.IsAny<List<string>>(), It.IsAny<List<Game>>()))
                  .Returns(_game);

            var result = Subject.GetGame(title);

            result.Should().Be(_game);
        }

        [Test]
        public void should_return_null_when_no_game_found()
        {
            const string title = "Nonexistent.Game.2099.v1.0-GROUP";

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitleCandidates(It.IsAny<List<string>>(), out It.Ref<List<string>>.IsAny))
                  .Returns(new List<Game>());

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(It.IsAny<List<string>>(), It.IsAny<int?>(), It.IsAny<List<string>>(), It.IsAny<List<Game>>()))
                  .Returns((Game)null);

            var result = Subject.GetGame(title);

            result.Should().BeNull();
        }

        [Test]
        public void should_return_game_found_by_raw_title_when_parse_fails()
        {
            const string title = "xK9#mZ2@";

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.FindByTitle(title))
                  .Returns(_game);

            var result = Subject.GetGame(title);

            result.Should().Be(_game);
        }
    }
}
