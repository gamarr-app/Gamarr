using Moq;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Lifecycle
{
    [TestFixture]
    public class LifecycleServiceFixture : CoreTest<LifecycleService>
    {
        [Test]
        public void restart_should_flag_restart_pending_before_publishing()
        {
            Subject.Restart();

            Mocker.GetMock<IRuntimeInfo>()
                  .VerifySet(c => c.RestartPending = true, Times.Once());
        }

        [Test]
        public void restart_should_publish_shutdown_requested_with_restarting_set()
        {
            Subject.Restart();

            Mocker.GetMock<IEventAggregator>()
                  .Verify(c => c.PublishEvent(It.Is<ApplicationShutdownRequested>(e => e.Restarting)), Times.Once());
        }

        [Test]
        public void shutdown_should_publish_shutdown_requested_without_restarting()
        {
            Subject.Shutdown();

            Mocker.GetMock<IEventAggregator>()
                  .Verify(c => c.PublishEvent(It.Is<ApplicationShutdownRequested>(e => !e.Restarting)), Times.Once());
        }

        [Test]
        public void shutdown_should_not_flag_restart_pending()
        {
            Subject.Shutdown();

            Mocker.GetMock<IRuntimeInfo>()
                  .VerifySet(c => c.RestartPending = true, Times.Never());
        }

        [Test]
        public void shutdown_should_not_stop_the_windows_service_itself()
        {
            // The host now owns stopping/restarting: LifecycleService only raises the
            // event, so a Windows service must not be stopped out from under the host.
            Mocker.GetMock<IRuntimeInfo>()
                  .SetupGet(c => c.IsWindowsService)
                  .Returns(true);

            Subject.Shutdown();

            Mocker.GetMock<IEventAggregator>()
                  .Verify(c => c.PublishEvent(It.IsAny<ApplicationShutdownRequested>()), Times.Once());
        }
    }
}
