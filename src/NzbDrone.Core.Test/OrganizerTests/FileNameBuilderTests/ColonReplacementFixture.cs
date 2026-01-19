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
    public class ColonReplacementFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private GameFile _gameFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                    .CreateNew()
                    .With(s => s.Title = "CSI: Vegas")
                    .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _gameFile = new GameFile { Quality = new QualityModel(Quality.Uplay), ReleaseGroup = "GamarrTest" };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_replace_colon_followed_by_space_with_space_dash_space_by_default()
        {
            _namingConfig.StandardGameFormat = "{Game Title}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be("CSI - Vegas");
        }

        [TestCase("CSI: Vegas", ColonReplacementFormat.Smart, "CSI - Vegas")]
        [TestCase("CSI: Vegas", ColonReplacementFormat.Dash, "CSI- Vegas")]
        [TestCase("CSI: Vegas", ColonReplacementFormat.Delete, "CSI Vegas")]
        [TestCase("CSI: Vegas", ColonReplacementFormat.SpaceDash, "CSI - Vegas")]
        [TestCase("CSI: Vegas", ColonReplacementFormat.SpaceDashSpace, "CSI - Vegas")]
        public void should_replace_colon_followed_by_space_with_expected_result(string gameName, ColonReplacementFormat replacementFormat, string expected)
        {
            _game.Title = gameName;
            _namingConfig.StandardGameFormat = "{Game Title}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildFileName(_game, _gameFile)
                .Should().Be(expected);
        }

        [TestCase("Game:Title", ColonReplacementFormat.Smart, "Game-Title")]
        [TestCase("Game:Title", ColonReplacementFormat.Dash, "Game-Title")]
        [TestCase("Game:Title", ColonReplacementFormat.Delete, "GameTitle")]
        [TestCase("Game:Title", ColonReplacementFormat.SpaceDash, "Game -Title")]
        [TestCase("Game:Title", ColonReplacementFormat.SpaceDashSpace, "Game - Title")]
        public void should_replace_colon_with_expected_result(string gameName, ColonReplacementFormat replacementFormat, string expected)
        {
            _game.Title = gameName;
            _namingConfig.StandardGameFormat = "{Game Title}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildFileName(_game, _gameFile)
                .Should().Be(expected);
        }
    }
}
