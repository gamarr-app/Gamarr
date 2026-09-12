using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck
{
    [TestFixture]
    public class HealthCheckFixture : CoreTest
    {
        private const string WikiRoot = "https://github.com/gamarr-app/Gamarr/wiki/";

        [TestCase("I blew up because of some weird user mistake", null, WikiRoot + "system#i-blew-up-because-of-some-weird-user-mistake")]
        [TestCase("I blew up because of some weird user mistake", "#my-health-check", WikiRoot + "system#my-health-check")]
        [TestCase("I blew up because of some weird user mistake", "custom-page#my-health-check", WikiRoot + "custom-page#my-health-check")]
        public void should_format_wiki_url(string message, string wikiFragment, string expectedUrl)
        {
            var subject = new NzbDrone.Core.HealthCheck.HealthCheck(typeof(HealthCheckBase), HealthCheckResult.Warning, HealthCheckReason.ServerNotification, message, wikiFragment);

            subject.WikiUrl.Should().Be(expectedUrl);
        }

        [TestCase(HealthCheckReason.RootFolderMissing)]
        [TestCase(HealthCheckReason.IndexerStatusUnavailable)]
        [TestCase(HealthCheckReason.GamesWithoutMetadata)]
        public void should_set_reason(HealthCheckReason reason)
        {
            var subject = new NzbDrone.Core.HealthCheck.HealthCheck(typeof(HealthCheckBase), HealthCheckResult.Error, reason, "I blew up because of some weird user mistake");

            subject.Reason.Should().Be(reason);
        }
    }
}
