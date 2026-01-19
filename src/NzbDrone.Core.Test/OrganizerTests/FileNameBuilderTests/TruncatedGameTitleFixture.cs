using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]

    public class TruncatedGameTitleFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                    .CreateNew()
                    .With(s => s.Title = "Game Title")
                    .Build();

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

        [TestCase("{Game Title:16}", "The Fantastic...")]
        [TestCase("{Game TitleThe:17}", "Fantastic Life...")]
        [TestCase("{Game CleanTitle:-13}", "...Mr. Sisko")]
        public void should_truncate_series_title(string format, string expected)
        {
            _game.Title = "The Fantastic Life of Mr. Sisko";
            _namingConfig.GameFolderFormat = format;

            var result = Subject.GetGameFolder(_game, _namingConfig);
            result.Should().Be(expected);
        }
    }
}
