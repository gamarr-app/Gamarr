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
    public class OriginalTitleFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private GameFile _gameFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                    .CreateNew()
                    .With(s => s.Title = "My Game")
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

        [Test]
        public void should_not_recursively_include_current_filename()
        {
            _gameFile.RelativePath = "My Game";
            _namingConfig.StandardGameFormat = "{Game Title} {[Original Title]}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be("My Game");
        }

        [Test]
        public void should_include_original_title_if_not_current_file_name()
        {
            _gameFile.SceneName = "my.game.2008";
            _gameFile.RelativePath = "My Game";
            _namingConfig.StandardGameFormat = "{Game Title} {[Original Title]}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be("My Game [my.game.2008]");
        }

        [Test]
        public void should_include_current_filename_if_not_renaming_files()
        {
            _gameFile.SceneName = "my.game.2008";
            _namingConfig.RenameGames = false;

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be("my.game.2008");
        }

        [Test]
        public void should_include_current_filename_if_not_including_multiple_naming_tokens()
        {
            _gameFile.RelativePath = "My Game";
            _namingConfig.StandardGameFormat = "{Original Title}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be("My Game");
        }
    }
}
