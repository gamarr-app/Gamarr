using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GamesTests
{
    [TestFixture]
    public class GameTitleNormalizerFixture : CoreTest
    {
        [TestCase("The Witcher 3", 0, "witcher 3")]
        [TestCase("A Game Title", 0, "game title")]
        [TestCase("An Amazing Game", 0, "amazing game")]
        [TestCase("Cyberpunk 2077", 0, "cyberpunk 2077")]
        public void should_normalize_title(string title, int igdbId, string expected)
        {
            GameTitleNormalizer.Normalize(title, igdbId).Should().Be(expected);
        }

        [TestCase("Game: Subtitle", "game subtitle")]
        [TestCase("Game - Part 2", "game part 2")]
        [TestCase("Game's Title", "games title")]
        public void should_handle_special_characters(string title, string expected)
        {
            GameTitleNormalizer.Normalize(title, 0).Should().Be(expected);
        }

        [TestCase("  Game  Title  ", "game title")]
        [TestCase("Game\t\tTitle", "game title")]
        public void should_handle_extra_whitespace(string title, string expected)
        {
            GameTitleNormalizer.Normalize(title, 0).Should().Be(expected);
        }

        [TestCase("GAME TITLE", "game title")]
        [TestCase("game title", "game title")]
        [TestCase("GaMe TiTlE", "game title")]
        public void should_be_case_insensitive(string title, string expected)
        {
            GameTitleNormalizer.Normalize(title, 0).Should().Be(expected);
        }

        [Test]
        public void should_handle_null_title()
        {
            GameTitleNormalizer.Normalize(null, 0).Should().BeNull();
        }

        [Test]
        public void should_handle_empty_title()
        {
            GameTitleNormalizer.Normalize("", 0).Should().Be("");
        }
    }
}
