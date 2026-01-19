using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class GameTitleFirstCharacterFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
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

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        [TestCase("The Mist", "M", "The Mist")]
        [TestCase("A", "A", "A")]
        [TestCase("30 Rock", "3", "30 Rock")]
        [TestCase("The '80s Greatest", "8", "The '80s Greatest")]
        [TestCase("좀비버스", "좀", "좀비버스")]
        [TestCase("¡Mucha Lucha!", "M", "¡Mucha Lucha!")]
        [TestCase(".hack", "H", "hack")]
        [TestCase("Ütopya", "U", "Ütopya")]
        [TestCase("Æon Flux", "A", "Æon Flux")]
        public void should_get_expected_folder_name_back(string title, string parent, string child)
        {
            _game.Title = title;
            _namingConfig.GameFolderFormat = "{Game TitleFirstCharacter}\\{Game Title}";

            Subject.GetGameFolder(_game).Should().Be(Path.Combine(parent, child));
        }

        [Test]
        public void should_be_able_to_use_lower_case_first_character()
        {
            _game.Title = "Westworld";
            _namingConfig.GameFolderFormat = "{game titlefirstcharacter}\\{game title}";

            Subject.GetGameFolder(_game).Should().Be(Path.Combine("w", "westworld"));
        }
    }
}
