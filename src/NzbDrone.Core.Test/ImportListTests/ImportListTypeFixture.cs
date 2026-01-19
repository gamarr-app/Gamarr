using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests
{
    [TestFixture]
    public class ImportListTypeFixture : CoreTest
    {
        [Test]
        public void should_contain_game_relevant_types()
        {
            var values = Enum.GetValues(typeof(ImportListType)).Cast<ImportListType>().ToList();

            // Game-specific list types
            values.Should().Contain(ImportListType.IGDB);
            values.Should().Contain(ImportListType.Steam);
            values.Should().Contain(ImportListType.Plex);
        }

        [Test]
        public void should_not_contain_movie_specific_types()
        {
            var names = Enum.GetNames(typeof(ImportListType)).ToList();

            // Movie-specific types should have been removed
            names.Should().NotContain("Trakt");
            names.Should().NotContain("Simkl");
            names.Should().NotContain("TMDb");
        }

        [Test]
        public void should_have_program_type()
        {
            var values = Enum.GetValues(typeof(ImportListType)).Cast<ImportListType>().ToList();

            values.Should().Contain(ImportListType.Program);
        }

        [Test]
        public void should_have_other_and_advanced_types()
        {
            var values = Enum.GetValues(typeof(ImportListType)).Cast<ImportListType>().ToList();

            values.Should().Contain(ImportListType.Other);
            values.Should().Contain(ImportListType.Advanced);
        }

        [Test]
        public void should_have_unique_values()
        {
            var values = Enum.GetValues(typeof(ImportListType)).Cast<int>().ToList();
            var names = Enum.GetNames(typeof(ImportListType)).ToList();

            values.Should().OnlyHaveUniqueItems();
            names.Should().OnlyHaveUniqueItems();
        }

        [TestCase(ImportListType.Program, "Program")]
        [TestCase(ImportListType.IGDB, "IGDB")]
        [TestCase(ImportListType.Steam, "Steam")]
        [TestCase(ImportListType.Plex, "Plex")]
        [TestCase(ImportListType.Other, "Other")]
        [TestCase(ImportListType.Advanced, "Advanced")]
        public void should_have_correct_string_representation(ImportListType type, string expected)
        {
            type.ToString().Should().Be(expected);
        }
    }
}
