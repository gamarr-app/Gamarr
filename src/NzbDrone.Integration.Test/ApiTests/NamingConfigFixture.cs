using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class NamingConfigFixture : IntegrationTest
    {
        [Test]
        public void should_be_able_to_get()
        {
            NamingConfig.GetSingle().Should().NotBeNull();
        }

        [Test]
        public void should_be_able_to_get_by_id()
        {
            var config = NamingConfig.GetSingle();
            NamingConfig.Get(config.Id).Should().NotBeNull();
            NamingConfig.Get(config.Id).Id.Should().Be(config.Id);
        }

        [TestCase("{Game Title} {Release Year}")]
        [TestCase("{Game Title} {(Release Year)}")]
        [TestCase("{Game Title} {[Release Year]}")]
        [TestCase("{Game Title} {{Release Year}}")]
        [TestCase("{Game Title}{ Release Year }")]
        [TestCase("{Game-Title}{-Release-Year-}")]
        [TestCase("{Game_Title}{_Release_Year_}")]
        [TestCase("{Game.Title}{.Release.Year.}")]
        public void should_be_able_to_update(string standardGameFormat)
        {
            var config = NamingConfig.GetSingle();
            config.RenameGames = false;
            config.StandardGameFormat = standardGameFormat;

            var result = NamingConfig.Put(config);
            result.RenameGames.Should().BeFalse();
            result.StandardGameFormat.Should().Be(config.StandardGameFormat);
        }

        [Test]
        public void should_get_bad_request_if_standard_format_is_empty()
        {
            var config = NamingConfig.GetSingle();
            config.RenameGames = true;
            config.StandardGameFormat = "";

            var errors = NamingConfig.InvalidPut(config);
            errors.Should().NotBeNull();
        }

        [Test]
        public void should_get_bad_request_if_standard_format_doesnt_contain_title()
        {
            var config = NamingConfig.GetSingle();
            config.RenameGames = true;
            config.StandardGameFormat = "{quality}";

            var errors = NamingConfig.InvalidPut(config);
            errors.Should().NotBeNull();
        }

        [Test]
        public void should_not_require_format_when_rename_episodes_is_false()
        {
            var config = NamingConfig.GetSingle();
            config.RenameGames = false;
            config.StandardGameFormat = "";

            var errors = NamingConfig.InvalidPut(config);
            errors.Should().NotBeNull();
        }

        [Test]
        public void should_require_format_when_rename_episodes_is_true()
        {
            var config = NamingConfig.GetSingle();
            config.RenameGames = true;
            config.StandardGameFormat = "";

            var errors = NamingConfig.InvalidPut(config);
            errors.Should().NotBeNull();
        }

        [Test]
        public void should_get_bad_request_if_game_folder_format_does_not_contain_game_title()
        {
            var config = NamingConfig.GetSingle();
            config.RenameGames = true;
            config.GameFolderFormat = "This and That";

            var errors = NamingConfig.InvalidPut(config);
            errors.Should().NotBeNull();
        }
    }
}
