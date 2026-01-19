using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class MetadataLinkTypeFixture : CoreTest
    {
        [Test]
        public void should_contain_game_relevant_types()
        {
            var values = Enum.GetValues(typeof(MetadataLinkType)).Cast<MetadataLinkType>().ToList();

            // Game-specific link types
            values.Should().Contain(MetadataLinkType.Igdb);
            values.Should().Contain(MetadataLinkType.Steam);
            values.Should().Contain(MetadataLinkType.Rawg);
        }

        [Test]
        public void should_not_contain_movie_specific_types()
        {
            var names = Enum.GetNames(typeof(MetadataLinkType)).ToList();

            // Movie-specific types should have been removed
            names.Should().NotContain("Imdb");
            names.Should().NotContain("Trakt");
            names.Should().NotContain("TMDb");
        }

        [Test]
        public void igdb_should_be_default_value()
        {
            // IGDB should be the primary/default link type for games
            ((int)MetadataLinkType.Igdb).Should().Be(0);
        }

        [Test]
        public void should_have_unique_values()
        {
            var values = Enum.GetValues(typeof(MetadataLinkType)).Cast<int>().ToList();
            var names = Enum.GetNames(typeof(MetadataLinkType)).ToList();

            values.Should().OnlyHaveUniqueItems();
            names.Should().OnlyHaveUniqueItems();
        }

        [Test]
        public void should_have_exactly_three_types()
        {
            var values = Enum.GetValues(typeof(MetadataLinkType)).Cast<MetadataLinkType>().ToList();

            values.Should().HaveCount(3);
        }

        [TestCase(MetadataLinkType.Igdb, "Igdb")]
        [TestCase(MetadataLinkType.Steam, "Steam")]
        [TestCase(MetadataLinkType.Rawg, "Rawg")]
        public void should_have_correct_string_representation(MetadataLinkType type, string expected)
        {
            type.ToString().Should().Be(expected);
        }

        [TestCase(MetadataLinkType.Igdb, 0)]
        [TestCase(MetadataLinkType.Steam, 1)]
        [TestCase(MetadataLinkType.Rawg, 2)]
        public void should_have_expected_integer_values(MetadataLinkType type, int expected)
        {
            ((int)type).Should().Be(expected);
        }
    }
}
