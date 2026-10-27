using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.JobTests
{
    [TestFixture]
    public class SchedulerFixture : TestBase<Scheduler>
    {
        private static readonly FieldInfo CancellationTokenSourceField =
            typeof(Scheduler).GetField("_cancellationTokenSource", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo ExecutingField =
            typeof(Scheduler).GetField("_executing", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void Setup()
        {
            GivenCancellationTokenSource(null);
            GivenNotExecuting();
            Scheduler.TimerEnabled = false;

            GivenPendingTasks(new List<ScheduledTask>());
        }

        [TearDown]
        public void TearDown()
        {
            Scheduler.TimerEnabled = false;
            GivenCancellationTokenSource(null);
            GivenNotExecuting();
        }

        private static void GivenCancellationTokenSource(CancellationTokenSource source)
        {
            CancellationTokenSourceField.SetValue(null, source);
        }

        private static void GivenNotExecuting()
        {
            ExecutingField.SetValue(null, 0);
        }

        private void GivenPendingTasks(List<ScheduledTask> tasks)
        {
            Mocker.GetMock<ITaskManager>()
                  .Setup(s => s.GetPending())
                  .Returns(tasks);
        }

        [Test]
        public void should_not_throw_when_token_source_has_not_been_assigned()
        {
            // ApplicationStartedEvent has not been handled, so the static token
            // source is still null. Dereferencing it in the finally used to kill
            // the timer callback outright.
            Assert.DoesNotThrow(() => Subject.ExecuteCommands());
        }

        [Test]
        public void should_push_pending_tasks_when_token_source_has_not_been_assigned()
        {
            GivenPendingTasks(new List<ScheduledTask>
            {
                new ScheduledTask { TypeName = "TestTask", Priority = CommandPriority.Low }
            });

            Subject.ExecuteCommands();

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push("TestTask", It.IsAny<DateTime>(), It.IsAny<DateTime>(), CommandPriority.Low, CommandTrigger.Scheduled), Times.Once());
        }

        [Test]
        public void should_re_enable_timer_when_token_source_has_not_been_assigned()
        {
            // The whole point of the fix: a scheduling pass must always leave the
            // timer armed, otherwise every scheduled task stops silently until
            // the application is restarted.
            Subject.ExecuteCommands();

            Scheduler.TimerEnabled.Should().BeTrue();
        }

        [Test]
        public void should_not_mask_the_real_exception_when_token_source_has_not_been_assigned()
        {
            Mocker.GetMock<ITaskManager>()
                  .Setup(s => s.GetPending())
                  .Throws(new InvalidOperationException("task manager blew up"));

            var exception = Assert.Throws<InvalidOperationException>(() => Subject.ExecuteCommands());

            exception.Message.Should().Be("task manager blew up");
        }

        [Test]
        public void should_re_enable_timer_when_the_pass_throws()
        {
            Mocker.GetMock<ITaskManager>()
                  .Setup(s => s.GetPending())
                  .Throws(new InvalidOperationException("task manager blew up"));

            Assert.Throws<InvalidOperationException>(() => Subject.ExecuteCommands());

            Scheduler.TimerEnabled.Should().BeTrue();
        }

        [Test]
        public void should_not_re_enable_timer_when_cancellation_has_been_requested()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            GivenCancellationTokenSource(tokenSource);

            Subject.ExecuteCommands();

            Scheduler.TimerEnabled.Should().BeFalse();
        }

        [Test]
        public void should_not_run_overlapping_passes()
        {
            var calls = 0;

            Mocker.GetMock<ITaskManager>()
                  .Setup(s => s.GetPending())
                  .Returns(() =>
                  {
                      calls++;

                      // Stands in for a second timer tick landing while this pass
                      // is still in flight. Two passes toggling Timer.Enabled at
                      // once can race inside the (not thread-safe) setter.
                      Subject.ExecuteCommands();

                      return new List<ScheduledTask>();
                  });

            Subject.ExecuteCommands();

            calls.Should().Be(1);
        }

        [Test]
        public void should_not_throw_when_shutdown_is_requested_before_application_started()
        {
            // Mirror image of the same bug: the shutdown handler dereferenced the
            // same static field, which is null until ApplicationStartedEvent runs.
            Assert.DoesNotThrow(() => Subject.Handle(new ApplicationShutdownRequested()));
        }

        [Test]
        public void should_cancel_the_token_source_on_shutdown()
        {
            var tokenSource = new CancellationTokenSource();

            GivenCancellationTokenSource(tokenSource);

            Subject.Handle(new ApplicationShutdownRequested());

            tokenSource.IsCancellationRequested.Should().BeTrue();
        }
    }
}
