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
    public class CleanTitleTheFixture : CoreTest<FileNameBuilder>
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

            _gameFile = new GameFile { Quality = new QualityModel(Quality.Uplay), ReleaseGroup = "GamarrTest" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        [TestCase("The Mist", "Mist, The")]
        [TestCase("A Place to Call Home", "Place to Call Home, A")]
        [TestCase("An Adventure in Space and Time", "Adventure in Space and Time, An")]
        [TestCase("The Flash (2010)", "Flash, The 2010")]
        [TestCase("A League Of Their Own (AU)", "League Of Their Own, A AU")]
        [TestCase("The Fixer (ZH) (2015)", "Fixer, The ZH 2015")]
        [TestCase("The Sixth Sense 2 (Thai)", "Sixth Sense 2, The Thai")]
        [TestCase("The Amazing Race (Latin America)", "Amazing Race, The Latin America")]
        [TestCase("The Rat Pack (A&E)", "Rat Pack, The AandE")]
        [TestCase("The Climax: I (Almost) Got Away With It (2016)", "Climax I Almost Got Away With It, The 2016")]
        [TestCase(null, "")]
        public void should_get_expected_title_back(string title, string expected)
        {
            _game.Title = title;
            _namingConfig.StandardGameFormat = "{Game CleanTitleThe}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be(expected);
        }

        [TestCase("A")]
        [TestCase("Anne")]
        [TestCase("Theodore")]
        [TestCase("3%")]
        public void should_not_change_title(string title)
        {
            _game.Title = title;
            _namingConfig.StandardGameFormat = "{Game CleanTitleThe}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be(title);
        }
    }
}
