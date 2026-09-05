using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private void GivenMetadataPlatforms(params PlatformFamily[] families)
        {
            _fakeGame.Platforms = families.Select(f => new GamePlatform { Family = f }).ToList();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetAllGames())
                  .Returns(new List<Game>());
        }

        [Test]
        public void should_set_platform_from_unambiguous_metadata()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games"
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms(PlatformFamily.NintendoSwitch);

            Subject.AddGame(newGame).Platform.Should().Be(PlatformFamily.NintendoSwitch);
        }

        [Test]
        public void should_leave_platform_unknown_for_a_multiplatform_game()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games"
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms(PlatformFamily.PC, PlatformFamily.NintendoSwitch);

            Subject.AddGame(newGame).Platform.Should().Be(PlatformFamily.Unknown);
        }

        // The three tests above assert the value AddGame returns. These assert
        // what is actually handed to IGameService.AddGame, i.e. what gets
        // stored: a live add of a Switch title came back as 'pc', so "derived
        // correctly" and "stored correctly" have to be checked separately.
        [Test]
        public void should_store_nintendo_switch_for_a_switch_exclusive_title()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games"
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms(PlatformFamily.NintendoSwitch);

            Subject.AddGame(newGame);

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.AddGame(It.Is<Game>(g => g.Platform == PlatformFamily.NintendoSwitch)), Times.Once());
        }

        [Test]
        public void should_not_clobber_a_platform_of_nintendo_switch_on_the_way_to_storage()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games",
                Platform = PlatformFamily.NintendoSwitch
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms(PlatformFamily.PC, PlatformFamily.NintendoSwitch);

            Subject.AddGame(newGame).Platform.Should().Be(PlatformFamily.NintendoSwitch);

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.AddGame(It.Is<Game>(g => g.Platform == PlatformFamily.NintendoSwitch)), Times.Once());
        }

        [Test]
        public void should_store_unknown_rather_than_pc_for_a_multiplatform_title()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games"
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms(PlatformFamily.PC, PlatformFamily.NintendoSwitch, PlatformFamily.Xbox);

            Subject.AddGame(newGame);

            // Unknown means "any" to PlatformSpecification; PC would let PC
            // repacks satisfy a console entry.
            Mocker.GetMock<IGameService>()
                  .Verify(s => s.AddGame(It.Is<Game>(g => g.Platform == PlatformFamily.Unknown)), Times.Once());
        }

        [Test]
        public void should_store_unknown_rather_than_pc_when_metadata_has_no_platforms()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games"
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms();

            Subject.AddGame(newGame);

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.AddGame(It.Is<Game>(g => g.Platform == PlatformFamily.Unknown)), Times.Once());
        }

        [Test]
        public void should_not_override_a_platform_chosen_by_the_caller()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games",
                Platform = PlatformFamily.PC
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms(PlatformFamily.NintendoSwitch);

            Subject.AddGame(newGame).Platform.Should().Be(PlatformFamily.PC);
        }
    }
}
