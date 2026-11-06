using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    public class NewznabSettingFixture : CoreTest
    {
        [TestCase("http://nzbs.org")]
        [TestCase("http:///www.nzbplanet.net")]
        public void requires_apikey(string url)
        {
            var setting = new NewznabSettings()
            {
                ApiKey = "",
                BaseUrl = url
            };

            setting.Validate().IsValid.Should().BeFalse();
            setting.Validate().Errors.Should().Contain(c => c.PropertyName == "ApiKey");
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase(null)]
        public void invalid_url_should_not_apikey(string url)
        {
            var setting = new NewznabSettings
            {
                ApiKey = "",
                BaseUrl = url
            };

            setting.Validate().IsValid.Should().BeFalse();
            setting.Validate().Errors.Should().NotContain(c => c.PropertyName == "ApiKey");
            setting.Validate().Errors.Should().Contain(c => c.PropertyName == "BaseUrl");
        }

        [TestCase("http://nzbs2.org")]
        public void doesnt_requires_apikey(string url)
        {
            var setting = new NewznabSettings()
            {
                ApiKey = "",
                BaseUrl = url
            };

            setting.Validate().IsValid.Should().BeTrue();
        }

        [TestCase("&extended=1", true)]
        [TestCase("&a=b&c=d", true)]
        [TestCase("extended=1", false)]
        [TestCase("&novalue", false)]
        public void additional_parameters_must_be_query_pairs(string parameters, bool valid)
        {
            var setting = new NewznabSettings
            {
                ApiKey = "key",
                BaseUrl = "http://nzbs2.org",
                AdditionalParameters = parameters
            };

            setting.Validate().Errors.Any(c => c.PropertyName == "AdditionalParameters").Should().Be(!valid);
        }

        [Test]
        public void long_additional_parameters_validate_without_regex_timeout()
        {
            var setting = new NewznabSettings
            {
                ApiKey = "key",
                BaseUrl = "http://nzbs2.org",
                AdditionalParameters = string.Concat(Enumerable.Repeat("&a", 50000))
            };

            setting.Validate().Errors.Should().Contain(c => c.PropertyName == "AdditionalParameters");
        }
    }
}
