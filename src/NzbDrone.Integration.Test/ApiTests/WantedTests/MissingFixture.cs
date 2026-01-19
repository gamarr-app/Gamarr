using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests.WantedTests
{
    [TestFixture]
    public class MissingFixture : IntegrationTest
    {
        [Test]
        [Order(0)]
        public void missing_should_be_empty()
        {
            EnsureNoGame(620, "Portal 2");

            var result = WantedMissing.GetPaged(0, 15, "gameMetadata.year", "desc");

            result.Records.Should().BeEmpty();
        }

        [Test]
        [Order(1)]
        public void missing_should_have_monitored_items()
        {
            EnsureGame(620, "Portal 2", true);

            var result = WantedMissing.GetPaged(0, 15, "gameMetadata.year", "desc");

            result.Records.Should().NotBeEmpty();
        }

        [Test]
        [Order(1)]
        public void missing_should_have_series()
        {
            EnsureGame(620, "Portal 2", true);

            var result = WantedMissing.GetPaged(0, 15, "gameMetadata.year", "desc");

            result.Records.First().Title.Should().Be("Portal 2");
        }

        [Test]
        [Order(1)]
        public void missing_should_not_have_unmonitored_items()
        {
            EnsureGame(620, "Portal 2", false);

            var result = WantedMissing.GetPaged(0, 15, "gameMetadata.year", "desc");

            result.Records.Should().BeEmpty();
        }

        [Test]
        [Order(2)]
        public void missing_should_have_unmonitored_items()
        {
            EnsureGame(620, "Portal 2", false);

            var result = WantedMissing.GetPaged(0, 15, "gameMetadata.year", "desc", "monitored", false);

            result.Records.Should().NotBeEmpty();
        }
    }
}
