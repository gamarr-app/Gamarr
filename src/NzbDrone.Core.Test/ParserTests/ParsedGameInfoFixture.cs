using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ParsedGameInfoFixture
    {
        [Test]
        public void constructor_should_initialize_lists()
        {
            var parsed = new ParsedGameInfo();

            parsed.GameTitles.Should().NotBeNull();
            parsed.GameTitles.Should().BeEmpty();
            parsed.Languages.Should().NotBeNull();
            parsed.Languages.Should().BeEmpty();
        }

        [Test]
        public void PrimaryGameTitle_should_return_first_title()
        {
            var parsed = new ParsedGameInfo
            {
                GameTitles = new List<string> { "First Game", "Second Game" }
            };

            parsed.PrimaryGameTitle.Should().Be("First Game");
        }

        [Test]
        public void PrimaryGameTitle_should_return_null_when_no_titles()
        {
            var parsed = new ParsedGameInfo();

            parsed.PrimaryGameTitle.Should().BeNull();
        }

        [Test]
        public void GameTitle_should_return_same_as_PrimaryGameTitle()
        {
            var parsed = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Test Game" }
            };

            parsed.GameTitle.Should().Be(parsed.PrimaryGameTitle);
        }

        [Test]
        public void should_set_all_properties()
        {
            var parsed = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Game Title" },
                OriginalTitle = "Original Title",
                ReleaseTitle = "Release Title",
                SimpleReleaseTitle = "Simple Title",
                Quality = new QualityModel(Quality.Unknown),
                Languages = new List<Language> { Language.English },
                ReleaseGroup = "GROUP",
                ReleaseHash = "hash123",
                Edition = "Deluxe Edition",
                Year = 2023,
                IgdbId = 12345,
                HardcodedSubs = "English"
            };

            parsed.OriginalTitle.Should().Be("Original Title");
            parsed.ReleaseTitle.Should().Be("Release Title");
            parsed.SimpleReleaseTitle.Should().Be("Simple Title");
            parsed.Quality.Should().NotBeNull();
            parsed.Languages.Should().HaveCount(1);
            parsed.ReleaseGroup.Should().Be("GROUP");
            parsed.ReleaseHash.Should().Be("hash123");
            parsed.Edition.Should().Be("Deluxe Edition");
            parsed.Year.Should().Be(2023);
            parsed.IgdbId.Should().Be(12345);
            parsed.HardcodedSubs.Should().Be("English");
        }

        [Test]
        public void ToString_should_contain_title_year_and_quality()
        {
            var parsed = new ParsedGameInfo
            {
                GameTitles = new List<string> { "Test Game" },
                Year = 2023,
                Quality = new QualityModel(Quality.Unknown)
            };

            var result = parsed.ToString();

            result.Should().Contain("Test Game");
            result.Should().Contain("2023");
        }

        [Test]
        public void should_be_able_to_add_languages()
        {
            var parsed = new ParsedGameInfo();
            parsed.Languages.Add(Language.English);
            parsed.Languages.Add(Language.French);

            parsed.Languages.Should().HaveCount(2);
        }

        [Test]
        public void should_be_able_to_add_game_titles()
        {
            var parsed = new ParsedGameInfo();
            parsed.GameTitles.Add("Game 1");
            parsed.GameTitles.Add("Game 2");

            parsed.GameTitles.Should().HaveCount(2);
            parsed.PrimaryGameTitle.Should().Be("Game 1");
        }
    }
}
