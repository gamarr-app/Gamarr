using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Queue
{
    [TestFixture]
    public class QueueServiceFixture : CoreTest<QueueService>
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void should_return_empty_queue_when_no_downloads()
        {
            Subject.GetQueue().Should().BeEmpty();
        }

        [Test]
        public void should_return_null_when_finding_non_existent_item()
        {
            Subject.Find(999).Should().BeNull();
        }

        [Test]
        public void should_publish_queue_updated_event_on_refresh()
        {
            var trackedDownloads = new List<TrackedDownload>();
            var refreshEvent = new TrackedDownloadRefreshedEvent(trackedDownloads);

            Subject.Handle(refreshEvent);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent(It.IsAny<QueueUpdatedEvent>()), Times.Once());
        }
    }
}
