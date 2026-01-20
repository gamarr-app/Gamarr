using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    public class IndexerStatusServiceFixture : CoreTest<IndexerStatusService>
    {
        private DateTime _epoch;

        [SetUp]
        public void SetUp()
        {
            _epoch = DateTime.UtcNow;

            Mocker.GetMock<IRuntimeInfo>()
                .SetupGet(v => v.StartTime)
                .Returns(_epoch - TimeSpan.FromHours(1));
        }

        private void WithStatus(IndexerStatus status)
        {
            Mocker.GetMock<IIndexerStatusRepository>()
                .Setup(v => v.FindByProviderId(1))
                .Returns(status);

            Mocker.GetMock<IIndexerStatusRepository>()
                .Setup(v => v.All())
                .Returns(new[] { status });
        }

        private void VerifyUpdate()
        {
            Mocker.GetMock<IIndexerStatusRepository>()
                .Verify(v => v.Upsert(It.IsAny<IndexerStatus>()), Times.Once());
        }

        private void VerifyNoUpdate()
        {
            Mocker.GetMock<IIndexerStatusRepository>()
                .Verify(v => v.Upsert(It.IsAny<IndexerStatus>()), Times.Never());
        }

        [Test]
        public void should_cancel_backoff_on_success()
        {
            WithStatus(new IndexerStatus { EscalationLevel = 2 });

            Subject.RecordSuccess(1);

            VerifyUpdate();

            var status = Subject.GetBlockedProviders().FirstOrDefault();
            status.Should().BeNull();
        }

        [Test]
        public void should_not_store_update_if_already_okay()
        {
            WithStatus(new IndexerStatus { EscalationLevel = 0 });

            Subject.RecordSuccess(1);

            VerifyNoUpdate();
        }

        [Test]
        public void should_not_record_failure_for_unknown_provider()
        {
            Subject.RecordFailure(0);

            Mocker.GetMock<IIndexerStatusRepository>()
                .Verify(v => v.FindByProviderId(1), Times.Never);

            VerifyNoUpdate();
        }

        [Test]
        public void should_return_last_rss_sync_release_info()
        {
            var releaseInfo = new Parser.Model.ReleaseInfo { Title = "Test Release" };
            WithStatus(new IndexerStatus { ProviderId = 1, LastRssSyncReleaseInfo = releaseInfo });

            Subject.GetLastRssSyncReleaseInfo(1).Should().Be(releaseInfo);
        }

        [Test]
        public void should_return_null_when_no_last_rss_sync()
        {
            WithStatus(new IndexerStatus { ProviderId = 1, LastRssSyncReleaseInfo = null });

            Subject.GetLastRssSyncReleaseInfo(1).Should().BeNull();
        }

        [Test]
        public void should_update_rss_sync_status()
        {
            WithStatus(new IndexerStatus { ProviderId = 1 });
            var releaseInfo = new Parser.Model.ReleaseInfo { Title = "Test Release" };

            Subject.UpdateRssSyncStatus(1, releaseInfo);

            VerifyUpdate();
        }

        [Test]
        public void should_return_cookies()
        {
            var cookies = new System.Collections.Generic.Dictionary<string, string> { { "session", "abc123" } };
            WithStatus(new IndexerStatus { ProviderId = 1, Cookies = cookies });

            Subject.GetIndexerCookies(1).Should().BeEquivalentTo(cookies);
        }

        [Test]
        public void should_update_cookies()
        {
            WithStatus(new IndexerStatus { ProviderId = 1 });
            var cookies = new System.Collections.Generic.Dictionary<string, string> { { "session", "abc123" } };
            var expiration = DateTime.UtcNow.AddDays(7);

            Subject.UpdateCookies(1, cookies, expiration);

            VerifyUpdate();
        }

        [Test]
        public void should_return_default_expiration_when_not_set()
        {
            WithStatus(new IndexerStatus { ProviderId = 1, CookiesExpirationDate = null });

            var result = Subject.GetIndexerCookiesExpirationDate(1);

            // Should be approximately 12 days from now
            result.Should().BeAfter(DateTime.Now.AddDays(11));
            result.Should().BeBefore(DateTime.Now.AddDays(13));
        }

        [Test]
        public void should_return_cookies_expiration_date()
        {
            var expiration = DateTime.UtcNow.AddDays(7);
            WithStatus(new IndexerStatus { ProviderId = 1, CookiesExpirationDate = expiration });

            Subject.GetIndexerCookiesExpirationDate(1).Should().BeCloseTo(expiration, TimeSpan.FromSeconds(1));
        }
    }
}
