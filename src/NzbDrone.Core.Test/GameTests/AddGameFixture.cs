using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class AddGameFixture : CoreTest<AddGameService>
    {
        private GameMetadata _fakeGame;

        [SetUp]
        public void Setup()
        {
            _fakeGame = Builder<GameMetadata>
                .CreateNew()
                .With(x => x.CollectionTitle = null)
                .With(x => x.CollectionIgdbId = 0)
                .Build();
        }

        private void GivenValidGame(int igdbId)
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoByIgdbId(igdbId))
                  .Returns(_fakeGame);
        }

        private void GivenValidPath()
        {
            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetGameFolder(It.IsAny<Game>(), null))
                  .Returns<Game, NamingConfig>((c, n) => c.Title);

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult());
        }

        [Test]
        public void should_be_able_to_add_a_game_without_passing_in_title()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games"
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();

            var series = Subject.AddGame(newGame);

            series.Title.Should().Be(_fakeGame.Title);
        }

        [Test]
        public void should_have_proper_path()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games"
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();

            var series = Subject.AddGame(newGame);

            series.Path.Should().Be(Path.Combine(newGame.RootFolderPath, _fakeGame.Title));
        }

        [Test]
        public void should_throw_if_game_validation_fails()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                Path = @"C:\Test\Game\Title1"
            };

            GivenValidGame(newGame.IgdbId);

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                                                {
                                                    new ValidationFailure("Path", "Test validation failure")
                                                }));

            Assert.Throws<ValidationException>(() => Subject.AddGame(newGame));
        }

        [Test]
        public void should_throw_if_game_cannot_be_found()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                Path = @"C:\Test\Game\Title1"
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoByIgdbId(newGame.IgdbId))
                  .Throws(new GameNotFoundException(newGame.IgdbId));

            Mocker.GetMock<IAddGameValidator>()
                  .Setup(s => s.Validate(It.IsAny<Game>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                                                {
                                                    new ValidationFailure("Path", "Test validation failure")
                                                }));

            Assert.Throws<ValidationException>(() => Subject.AddGame(newGame));

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
