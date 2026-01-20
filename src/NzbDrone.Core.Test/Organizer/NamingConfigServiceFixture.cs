using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Organizer
{
    [TestFixture]
    public class NamingConfigServiceFixture : CoreTest<NamingConfigService>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = new NamingConfig
            {
                Id = 1,
                RenameGames = true,
                ReplaceIllegalCharacters = true,
                ColonReplacementFormat = ColonReplacementFormat.Dash,
                GameFolderFormat = "{Game Title} ({Release Year})",
                StandardGameFormat = "{Game.Title}.{Quality.Full}",
                IncludeQuality = true
            };

            Mocker.GetMock<INamingConfigRepository>()
                  .Setup(s => s.Single())
                  .Returns(_namingConfig);
        }

        [Test]
        public void should_return_naming_config()
        {
            Subject.GetConfig().Should().Be(_namingConfig);
        }

        [Test]
        public void should_save_naming_config()
        {
            Subject.Save(_namingConfig);

            Mocker.GetMock<INamingConfigRepository>()
                  .Verify(s => s.Update(_namingConfig), Times.Once());
        }

        [Test]
        public void should_get_game_folder_format()
        {
            Subject.GetConfig().GameFolderFormat.Should().Be("{Game Title} ({Release Year})");
        }

        [Test]
        public void should_get_standard_game_format()
        {
            Subject.GetConfig().StandardGameFormat.Should().Be("{Game.Title}.{Quality.Full}");
        }

        [Test]
        public void should_get_rename_games_setting()
        {
            Subject.GetConfig().RenameGames.Should().BeTrue();
        }

        [Test]
        public void should_get_replace_illegal_characters_setting()
        {
            Subject.GetConfig().ReplaceIllegalCharacters.Should().BeTrue();
        }

        [Test]
        public void should_get_colon_replacement_format()
        {
            Subject.GetConfig().ColonReplacementFormat.Should().Be(ColonReplacementFormat.Dash);
        }
    }
}
