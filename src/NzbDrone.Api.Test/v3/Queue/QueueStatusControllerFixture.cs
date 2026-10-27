using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Queue;
using NzbDrone.Test.Common;
using Gamarr.Api.V3.Queue;

namespace NzbDrone.Api.Test.v3.Queue
{
    [TestFixture]
    public class QueueStatusControllerFixture : TestBase<QueueStatusController>
    {
        private static readonly FieldInfo DebounceField =
            typeof(QueueStatusController).GetField("_broadcastDebounce", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo PausedField =
            typeof(Debouncer).GetField("_paused", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Returns(new List<NzbDrone.Core.Queue.Queue>());

            Mocker.GetMock<IPendingReleaseService>()
                  .Setup(s => s.GetPendingQueue())
                  .Returns(new List<NzbDrone.Core.Queue.Queue>());
        }

        private int GetPausedCount()
        {
            var debouncer = DebounceField.GetValue(Subject);

            return (int)PausedField.GetValue(debouncer);
        }

        [Test]
        public void should_leave_debounce_unpaused_after_a_successful_call()
        {
            Subject.GetQueueStatus();

            GetPausedCount().Should().Be(0);
        }

        [Test]
        public void should_leave_debounce_unpaused_when_queue_service_throws()
        {
            // The whole point of the fix: Pause()/Resume() had no try/finally, so
            // a throw between them left the debounce paused forever - every
            // future QueueUpdatedEvent/PendingReleasesUpdatedEvent would then be
            // silently swallowed and the queue panel would stop updating until
            // Gamarr was restarted.
            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Throws(new InvalidOperationException("queue service blew up"));

            Assert.Throws<InvalidOperationException>(() => Subject.GetQueueStatus());

            GetPausedCount().Should().Be(0);
        }

        [Test]
        public void should_not_mask_the_real_exception_when_queue_service_throws()
        {
            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Throws(new InvalidOperationException("queue service blew up"));

            var exception = Assert.Throws<InvalidOperationException>(() => Subject.GetQueueStatus());

            exception.Message.Should().Be("queue service blew up");
        }
    }
}
