using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.Steam;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.Steam
{
    [TestFixture]
    [ExternalIntegrationTest]
    public class SteamStoreProxySearchFixture : CoreTest<SteamStoreProxy>
    {
        [SetUp]
        public void Setup()
        {
            UseRealHttp();
        }

        [TestCase("Portal", "Portal")]
        [TestCase("Half-Life", "Half-Life")]
        [TestCase("Counter-Strike", "Counter-Strike")]
        [TestCase("Elden Ring", "ELDEN RING")]
        public void successful_search(string query, string expectedInTitle)
        {
            var result = Subject.SearchGames(query);

            result.Should().NotBeEmpty();
            result[0].Title.Should().Contain(expectedInTitle.Split(' ')[0]);
            result[0].SteamAppId.Should().BeGreaterThan(0);

            ExceptionVerification.IgnoreWarns();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void should_return_empty_for_blank_search(string query)
        {
            var result = Subject.SearchGames(query);
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [TestCase("xyznonexistentgame12345")]
        public void should_return_empty_for_no_results(string query)
        {
            var result = Subject.SearchGames(query);
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_limit_results()
        {
            var result = Subject.SearchGames("game", limit: 5);

            result.Should().HaveCountLessOrEqualTo(5);

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_results_should_have_images()
        {
            var result = Subject.SearchGames("Portal");

            result.Should().NotBeEmpty();
            result[0].Images.Should().NotBeEmpty();

            ExceptionVerification.IgnoreWarns();
        }
    }
}
