using System.Net;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Test.Common;
using Gamarr.Api.V3.Indexers;

namespace NzbDrone.Api.Test.v3.Indexers
{
    [TestFixture]
    public class ReleaseControllerFixture : TestBase<ReleaseController>
    {
        private static ReleaseResource UncachedRelease()
        {
            return new ReleaseResource
            {
                IndexerId = 3,
                Guid = "a-guid-that-was-never-in-the-cache",
                GameId = 1
            };
        }

        [Test]
        public void should_reject_a_release_that_is_not_in_the_search_cache_with_not_found()
        {
            var ex = Assert.ThrowsAsync<NzbDroneClientException>(() => Subject.DownloadRelease(UncachedRelease()));

            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public void should_name_the_push_endpoint_as_the_remedy_for_releases_found_outside_gamarr()
        {
            var ex = Assert.ThrowsAsync<NzbDroneClientException>(() => Subject.DownloadRelease(UncachedRelease()));

            ex.Message.Should().Contain("/api/v3/release/push");
        }

        [Test]
        public void should_not_blame_the_cache_timeout_as_the_only_cause()
        {
            var ex = Assert.ThrowsAsync<NzbDroneClientException>(() => Subject.DownloadRelease(UncachedRelease()));

            // The old message ("try searching again") named a stale cache as the cause, which is
            // wrong for a release that was never in the cache to begin with. Re-searching is still
            // offered, but only as one of the two possibilities.
            ex.Message.Should().NotBe("Couldn't find requested release in cache, try searching again");
            ex.Message.Should().Contain("search again");
        }
    }
}
