using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.RAWG;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.RAWG
{
    [TestFixture]
    [IntegrationTest]
    public class RawgProxySearchFixture : CoreTest<RawgProxy>
    {
        private string _apiKey;

        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            // Get API key from environment for integration tests
            _apiKey = Environment.GetEnvironmentVariable("RAWG_API_KEY");

            if (string.IsNullOrEmpty(_apiKey))
            {
                Assert.Ignore("RAWG_API_KEY environment variable not set. Skipping integration tests.");
            }

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.RawgApiKey)
                .Returns(_apiKey);
        }

        [TestCase("Portal", "Portal")]
        [TestCase("Witcher", "The Witcher")]
        [TestCase("Grand Theft Auto", "Grand Theft Auto")]
        public void successful_search(string query, string expectedInTitle)
        {
            var result = Subject.SearchForNewGame(query);

            result.Should().NotBeEmpty();
            result[0].Title.Should().Contain(expectedInTitle.Split(' ')[0]);

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_return_empty_when_api_key_not_configured()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.RawgApiKey)
                .Returns(string.Empty);

            var result = Subject.SearchForNewGame("Portal");
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [TestCase("xyznonexistentgame12345abcdef")]
        public void should_return_empty_for_no_results(string query)
        {
            var result = Subject.SearchForNewGame(query);
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_results_should_have_images()
        {
            var result = Subject.SearchForNewGame("Portal");

            result.Should().NotBeEmpty();
            result[0].GameMetadata.Should().NotBeNull();
            result[0].GameMetadata.Value.Images.Should().NotBeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_results_should_have_platforms()
        {
            var result = Subject.SearchForNewGame("Grand Theft Auto V");

            result.Should().NotBeEmpty();
            result[0].GameMetadata.Should().NotBeNull();
            result[0].GameMetadata.Value.Platforms.Should().NotBeEmpty();

            ExceptionVerification.IgnoreWarns();
        }
    }
}
