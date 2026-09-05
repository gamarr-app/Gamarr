using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Organizer
{
    /// <summary>
    /// Every expected name here is a verbatim copy of a file in a real ownfoil
    /// library, extension removed. That string comparison is the only check worth
    /// anything: ownfoil identifies a title by the cnmt metadata inside the NSP, so
    /// a wrongly named file still shows up in ownfoil looking perfectly fine.
    /// </summary>
    [TestFixture]
    public class SwitchOwnfoilRenameProfileFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;
            _namingConfig.RenameProfile = RenameProfile.SwitchOwnfoil;

            _game = new Game
            {
                Title = "Kirby and the Forgotten Land",
                Year = 2022
            };

            Mocker.GetMock<INamingConfigService>()
                  .Setup(x => x.GetConfig())
                  .Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                  .Setup(x => x.Get(It.IsAny<Quality>()))
                  .Returns<Quality>(quality => Quality.DefaultQualityDefinitions.Single(x => x.Quality == quality));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(x => x.All())
                  .Returns(new List<CustomFormat>());

            Mocker.GetMock<IGameTranslationService>()
                  .Setup(x => x.GetAllTranslationsForGameMetadata(It.IsAny<int>()))
                  .Returns(new List<GameTranslation>());
        }

        private GameFile GivenFile(string originalFilePath, GameVersion gameVersion = null)
        {
            return new GameFile
            {
                Quality = new QualityModel(Quality.Retail),
                OriginalFilePath = originalFilePath,
                GameVersion = gameVersion
            };
        }

        // Names taken verbatim from /Volumes/Media/Switch. A file already following
        // the convention has to come back out of the builder unchanged, otherwise
        // enabling the profile would churn an entire library.
        [TestCase("Super Mario Galaxy™ [010099C022B96000][v0][Base].nsp",
                  "Super Mario Galaxy™ [010099C022B96000][v0][Base]")]
        [TestCase("Super Mario Galaxy™ [010099C022B96800][v327680][1.3.1][UPD].nsp",
                  "Super Mario Galaxy™ [010099C022B96800][v327680][1.3.1][UPD]")]
        [TestCase("Kirby and the Forgotten Land [01004D300C5AE000][v0][Base].nsp",
                  "Kirby and the Forgotten Land [01004D300C5AE000][v0][Base]")]
        [TestCase("Kirby and the Forgotten Land [01004D300C5AE800][v65536][1.1.0][UPD].nsp",
                  "Kirby and the Forgotten Land [01004D300C5AE800][v65536][1.1.0][UPD]")]
        [TestCase("Game Boy™ – Nintendo Switch Online [0100C62011050800][v1441792][4.0.0][UPD][US].nsp",
                  "Game Boy™ – Nintendo Switch Online [0100C62011050800][v1441792][4.0.0][UPD][US]")]
        [TestCase("Game Boy™ Advance – Nintendo Classics [010012F017576800][v1048576][3.3.0][UPD][US].nsp",
                  "Game Boy™ Advance – Nintendo Classics [010012F017576800][v1048576][3.3.0][UPD][US]")]
        [TestCase("Super Mario Galaxy™ 2 [0100FD8022DAA800][v327680][1.3.1][UPD].nsp",
                  "Super Mario Galaxy™ 2 [0100FD8022DAA800][v327680][1.3.1][UPD]")]
        [TestCase("SEGA Genesis™ – Nintendo Switch Online [0100B3C014BDA000][v0][Base].nsp",
                  "SEGA Genesis™ – Nintendo Switch Online [0100B3C014BDA000][v0][Base]")]
        [TestCase("Virtual Boy™ – Nintendo Classics [0100BFC01D976800][v131072][1.2.0][UPD].nsp",
                  "Virtual Boy™ – Nintendo Classics [0100BFC01D976800][v131072][1.2.0][UPD]")]
        [TestCase("Minecraft [0100D71004694800][v10092544][1.26.33][UPD].nsp",
                  "Minecraft [0100D71004694800][v10092544][1.26.33][UPD]")]
        public void should_reproduce_an_existing_ownfoil_library_name_exactly(string libraryFileName, string expected)
        {
            Subject.BuildFileName(_game, GivenFile(libraryFileName))
                   .Should().Be(expected);
        }

        [Test]
        public void should_name_a_base_game_from_a_grab_that_has_no_type_tag()
        {
            // Prowlarr normalises the extension separator to whitespace, so this is
            // what a real Switch grab looks like by the time it reaches naming.
            Subject.BuildFileName(_game, GivenFile("Kirby and the Forgotten Land [01004D300C5AE000][v0] nsp"))
                   .Should().Be("Kirby and the Forgotten Land [01004D300C5AE000][v0][Base]");
        }

        [Test]
        public void should_name_an_update_with_semver_from_a_grab_that_has_no_type_tag()
        {
            Subject.BuildFileName(_game, GivenFile("Kirby and the Forgotten Land [01004D300C5AE800][v65536][1.1.0] nsp"))
                   .Should().Be("Kirby and the Forgotten Land [01004D300C5AE800][v65536][1.1.0][UPD]");
        }

        [Test]
        public void should_keep_the_region_group_on_an_update()
        {
            var game = new Game { Title = "Game Boy™ – Nintendo Switch Online", Year = 2021 };

            Subject.BuildFileName(game, GivenFile("Game Boy™ – Nintendo Switch Online [0100C62011050800][v1441792][4.0.0][US] nsp"))
                   .Should().Be("Game Boy™ – Nintendo Switch Online [0100C62011050800][v1441792][4.0.0][UPD][US]");
        }

        [Test]
        public void should_add_the_type_tag_to_a_library_file_dumped_before_the_tag_existed()
        {
            var game = new Game { Title = "The Legend of Zelda Breath of the Wild", Year = 2017 };

            Subject.BuildFileName(game, GivenFile("The Legend of Zelda Breath of the Wild [01007EF00011E000][v0].nsp"))
                   .Should().Be("The Legend of Zelda Breath of the Wild [01007EF00011E000][v0][Base]");

            Subject.BuildFileName(game, GivenFile("The Legend of Zelda Breath of the Wild [01007EF00011E800][v1114112][1.9.0].nsp"))
                   .Should().Be("The Legend of Zelda Breath of the Wild [01007EF00011E800][v1114112][1.9.0][UPD]");

            var pikachu = new Game { Title = "Pokemon Let's Go Pikachu", Year = 2018 };

            Subject.BuildFileName(pikachu, GivenFile("Pokemon Let's Go, Pikachu! [010003F003A34000][v0].nsp"))
                   .Should().Be("Pokemon Let's Go, Pikachu! [010003F003A34000][v0][Base]");

            Subject.BuildFileName(pikachu, GivenFile("Pokemon Let's Go, Pikachu! [010003F003A34800][v131072].nsp"))
                   .Should().Be("Pokemon Let's Go, Pikachu! [010003F003A34800][v131072][UPD]");
        }

        [Test]
        public void should_leave_dlc_without_a_type_tag()
        {
            // A title id that ends in neither 000 nor 800 is DLC, and the real library
            // writes no type tag on those - so this round-trips unchanged.
            var game = new Game { Title = "Pokémon™ Sword", Year = 2019 };

            Subject.BuildFileName(game, GivenFile("Pokémon™ Sword - The Isle of Armor [0100ABF008969001][v0].nsp"))
                   .Should().Be("Pokémon™ Sword - The Isle of Armor [0100ABF008969001][v0]");
        }

        [Test]
        public void should_default_a_missing_version_group_to_v0()
        {
            Subject.BuildFileName(_game, GivenFile("Kirby and the Forgotten Land [01004D300C5AE000] nsp"))
                   .Should().Be("Kirby and the Forgotten Land [01004D300C5AE000][v0][Base]");
        }

        [Test]
        public void should_take_a_missing_semver_from_the_parsed_release_version()
        {
            // Same file the library holds as "... [v65536][1.1.0][UPD].nsp", but grabbed
            // under a name that never carried the display version.
            Subject.BuildFileName(_game, GivenFile("Kirby and the Forgotten Land [01004D300C5AE800][v65536] nsp", new GameVersion(1, 1, 0)))
                   .Should().Be("Kirby and the Forgotten Land [01004D300C5AE800][v65536][1.1.0][UPD]");
        }

        [Test]
        public void should_not_use_a_bare_integer_release_version_as_the_semver()
        {
            // "v327680" parses as a version but is the nsp version integer, not a
            // display version - emitting it as the semver would invent a name shape
            // no dumper writes.
            Subject.BuildFileName(_game, GivenFile("Kirby and the Forgotten Land [01004D300C5AE800][v65536] nsp", new GameVersion(327680)))
                   .Should().Be("Kirby and the Forgotten Land [01004D300C5AE800][v65536][UPD]");
        }

        [Test]
        public void should_fall_back_to_the_game_title_when_the_name_is_only_tags()
        {
            Subject.BuildFileName(_game, GivenFile("[01004D300C5AE000][v0].nsp"))
                   .Should().Be("Kirby and the Forgotten Land [01004D300C5AE000][v0][Base]");
        }

        [Test]
        public void should_read_the_title_id_from_the_scene_name_when_the_file_name_lacks_one()
        {
            var gameFile = new GameFile
            {
                Quality = new QualityModel(Quality.Retail),
                OriginalFilePath = "Kirby and the Forgotten Land.nsp",
                SceneName = "Kirby and the Forgotten Land [01004D300C5AE000][v0] NSW-VENOM"
            };

            Subject.BuildFileName(_game, gameFile)
                   .Should().Be("Kirby and the Forgotten Land [01004D300C5AE000][v0][Base]");
        }

        [Test]
        public void should_fall_back_to_normal_naming_without_a_title_id()
        {
            // A title id is the one field of the layout that cannot be derived, so a
            // name without one is not a Switch dump name at all.
            var gameFile = GivenFile("Kirby and the Forgotten Land NSW-VENOM.nsp");
            gameFile.Quality = new QualityModel(Quality.Uplay);

            Subject.BuildFileName(_game, gameFile)
                   .Should().Be("Kirby and the Forgotten Land (2022) Uplay");
        }

        [Test]
        public void should_use_the_bare_game_title_as_the_folder()
        {
            var game = new Game { Title = "Super Mario Galaxy", Year = 2025 };

            Subject.GetGameFolder(game)
                   .Should().Be("Super Mario Galaxy");
        }

        [Test]
        public void should_not_change_naming_for_the_other_profiles()
        {
            _namingConfig.RenameProfile = RenameProfile.Gamarr;

            var gameFile = GivenFile("Kirby and the Forgotten Land [01004D300C5AE000][v0][Base].nsp");
            gameFile.Quality = new QualityModel(Quality.Uplay);

            Subject.BuildFileName(_game, gameFile)
                   .Should().Be("Kirby and the Forgotten Land (2022) Uplay");
        }
    }
}
