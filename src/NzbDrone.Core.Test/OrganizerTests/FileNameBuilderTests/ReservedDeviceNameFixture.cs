using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]

    public class ReservedDeviceNameFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private GameFile _gameFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                    .CreateNew()
                    .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _gameFile = new GameFile { Quality = new QualityModel(Quality.HDTV720p), ReleaseGroup = "GamarrTest" };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                .Setup(v => v.All())
                .Returns(new List<CustomFormat>());
        }

        [TestCase("Con Game", 2021, "Con_Game (2021)")]
        [TestCase("Com1 Sat", 2021, "Com1_Sat (2021)")]
        public void should_replace_reserved_device_name_in_games_folder(string title, int year, string expected)
        {
            _game.Title = title;
            _game.Year = year;
            _namingConfig.GameFolderFormat = "{Game.Title} ({Release Year})";

            Subject.GetGameFolder(_game).Should().Be($"{expected}");
        }

        [TestCase("Con Game", 2021, "Con_Game (2021)")]
        [TestCase("Com1 Sat", 2021, "Com1_Sat (2021)")]
        public void should_replace_reserved_device_name_in_file_name(string title, int year, string expected)
        {
            _game.Title = title;
            _game.Year = year;
            _namingConfig.StandardGameFormat = "{Game.Title} ({Release Year})";

            Subject.BuildFileName(_game, _gameFile).Should().Be($"{expected}");
        }
    }
}
