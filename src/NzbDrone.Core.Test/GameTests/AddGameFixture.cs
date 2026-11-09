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
using NzbDrone.Core.RootFolders;
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

        private void GivenPlatformDefaultRootFolder(PlatformFamily platform, string path)
        {
            Mocker.GetMock<IPlatformRootFolderService>()
                  .Setup(s => s.GetDefaultRootFolderPath(platform))
                  .Returns(path);
        }

        [Test]
        public void should_use_the_platform_default_root_folder_when_none_was_supplied()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                Platform = PlatformFamily.NintendoSwitch
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms();
            GivenPlatformDefaultRootFolder(PlatformFamily.NintendoSwitch, @"C:\Test\Switch");

            var game = Subject.AddGame(newGame);

            game.RootFolderPath.Should().Be(@"C:\Test\Switch");
            game.Path.Should().Be(Path.Combine(@"C:\Test\Switch", _fakeGame.Title));
        }

        // The platform is derived from metadata before the root folder is
        // resolved, so a console exclusive added by IGDB id alone (import
        // lists, API clients) still lands in the console's folder.
        [Test]
        public void should_use_the_default_for_the_platform_derived_from_metadata()
        {
            var newGame = new Game
            {
                IgdbId = 1
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms(PlatformFamily.NintendoSwitch);
            GivenPlatformDefaultRootFolder(PlatformFamily.NintendoSwitch, @"C:\Test\Switch");
            GivenPlatformDefaultRootFolder(PlatformFamily.Unknown, @"C:\Test\Games");

            Subject.AddGame(newGame).RootFolderPath.Should().Be(@"C:\Test\Switch");
        }

        [Test]
        public void should_not_override_a_root_folder_chosen_by_the_caller()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                RootFolderPath = @"C:\Test\Games",
                Platform = PlatformFamily.NintendoSwitch
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms();
            GivenPlatformDefaultRootFolder(PlatformFamily.NintendoSwitch, @"C:\Test\Switch");

            Subject.AddGame(newGame).RootFolderPath.Should().Be(@"C:\Test\Games");

            Mocker.GetMock<IPlatformRootFolderService>()
                  .Verify(s => s.GetDefaultRootFolderPath(It.IsAny<PlatformFamily>()), Times.Never());
        }

        // GetDefaultRootFolderPath only returns null when the instance has no
        // root folders at all. The failure has to name the missing mapping:
        // "'Root Folder Path' must not be empty" sends the caller looking for a
        // bug in a request body that was fine.
        [Test]
        public void should_fail_naming_the_missing_mapping_when_nothing_can_be_resolved()
        {
            var newGame = new Game
            {
                IgdbId = 1,
                Platform = PlatformFamily.NintendoSwitch
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms();

            var exception = Assert.Throws<ValidationException>(() => Subject.AddGame(newGame));

            exception.Message.Should().Contain("No root folder was supplied and no default is configured for platform NintendoSwitch");
            exception.Message.Should().Contain("platform root folder mapping");
            exception.Message.Should().NotContain("must not be empty");
        }

        // ...and it fails before Path.Combine gets a null root folder, which
        // used to surface as an ArgumentNullException from deep inside the add.
        [Test]
        public void should_not_throw_argument_null_when_nothing_can_be_resolved()
        {
            var newGame = new Game
            {
                IgdbId = 1
            };

            GivenValidGame(newGame.IgdbId);
            GivenValidPath();
            GivenMetadataPlatforms();

            Assert.Throws<ValidationException>(() => Subject.AddGame(newGame));

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.AddGame(It.IsAny<Game>()), Times.Never());
        }

        // Bulk add: Games.GameMetadataId is unique, UpsertMany matches rows by
        // IgdbId, so two entries for one title used to share a metadata row and
        // the insert threw "UNIQUE constraint failed: Games.GameMetadataId".
        private void GivenBatchMocks(params int[] igdbIds)
        {
            GivenValidPath();

            foreach (var id in igdbIds)
            {
                var igdbId = id;

                Mocker.GetMock<IProvideGameInfo>()
                      .Setup(s => s.GetGameInfoByIgdbId(igdbId))
                      .Returns(() => new GameMetadata { IgdbId = igdbId, Title = "Game " + igdbId, Platforms = new List<GamePlatform>() });
            }

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.UpsertMany(It.IsAny<List<GameMetadata>>()))
                  .Callback<List<GameMetadata>>(list =>
                  {
                      foreach (var m in list)
                      {
                          m.Id = m.IgdbId * 10;
                      }
                  })
                  .Returns(true);

            var nextCloneId = 9000;

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.Insert(It.IsAny<GameMetadata>()))
                  .Returns<GameMetadata>(m =>
                  {
                      m.Id = nextCloneId++;
                      return m;
                  });

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AddGames(It.IsAny<List<Game>>()))
                  .Returns<List<Game>>(g => g);
        }

        private static Game BatchGame(int igdbId, PlatformFamily platform, string folder)
        {
            return new Game
            {
                IgdbId = igdbId,
                Platform = platform,
                Path = @"C:\Test\Games\" + folder
            };
        }

        [Test]
        public void should_drop_same_game_same_platform_duplicates_in_a_batch()
        {
            GivenBatchMocks(1);

            var result = Subject.AddGames(new List<Game>
            {
                BatchGame(1, PlatformFamily.PC, "A"),
                BatchGame(1, PlatformFamily.PC, "B")
            }, true);

            result.Should().HaveCount(1);
            result[0].Path.Should().Be(@"C:\Test\Games\A");
            result[0].GameMetadataId.Should().Be(10);
        }

        [Test]
        public void should_drop_same_game_with_unknown_platform_duplicates_in_a_batch()
        {
            GivenBatchMocks(1);

            var result = Subject.AddGames(new List<Game>
            {
                BatchGame(1, PlatformFamily.Unknown, "A"),
                BatchGame(1, PlatformFamily.PC, "B")
            }, true);

            result.Should().HaveCount(1);
        }

        [Test]
        public void should_throw_validation_for_a_same_game_batch_when_not_ignoring_errors()
        {
            GivenBatchMocks(1);

            Assert.Throws<ValidationException>(() => Subject.AddGames(new List<Game>
            {
                BatchGame(1, PlatformFamily.PC, "A"),
                BatchGame(1, PlatformFamily.PC, "B")
            }));

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.AddGames(It.IsAny<List<Game>>()), Times.Never());
        }

        [Test]
        public void should_give_a_cross_platform_batch_entry_its_own_metadata_row()
        {
            GivenBatchMocks(1);

            var result = Subject.AddGames(new List<Game>
            {
                BatchGame(1, PlatformFamily.PC, "A"),
                BatchGame(1, PlatformFamily.NintendoSwitch, "B")
            });

            result.Should().HaveCount(2);
            result.Select(g => g.GameMetadataId).Should().OnlyHaveUniqueItems();
            result[0].GameMetadataId.Should().Be(10);
            result[1].GameMetadataId.Should().Be(9000);

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(s => s.UpsertMany(It.Is<List<GameMetadata>>(l => l.Count == 1)), Times.Once());
        }

        [Test]
        public void should_clone_metadata_when_an_existing_game_already_uses_the_row()
        {
            GivenBatchMocks(1);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.ExistsByMetadataId(10))
                  .Returns(true);

            var result = Subject.AddGames(new List<Game>
            {
                BatchGame(1, PlatformFamily.NintendoSwitch, "A")
            });

            result.Should().HaveCount(1);
            result[0].GameMetadataId.Should().Be(9000);
        }

        [Test]
        public void should_skip_a_platform_agnostic_entry_when_an_existing_game_uses_the_row()
        {
            GivenBatchMocks(1, 2);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.ExistsByMetadataId(10))
                  .Returns(true);

            var result = Subject.AddGames(new List<Game>
            {
                BatchGame(1, PlatformFamily.Unknown, "A"),
                BatchGame(2, PlatformFamily.PC, "B")
            }, true);

            result.Should().HaveCount(1);
            result[0].IgdbId.Should().Be(2);
        }
    }
}
