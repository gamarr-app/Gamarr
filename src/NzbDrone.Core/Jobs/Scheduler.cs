using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using Timer = System.Timers.Timer;

namespace NzbDrone.Core.Jobs
{
    public class Scheduler :
        IHandle<ApplicationStartedEvent>,
        IHandle<ApplicationShutdownRequested>
    {
        private readonly ITaskManager _taskManager;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        private static readonly Timer Timer = new Timer();

        // System.Timers.Timer is explicitly not thread-safe: its Enabled setter
        // disposes and nulls an internal System.Threading.Timer, so two threads
        // toggling it can dereference that field after it has been nulled. Every
        // touch of Timer goes through this lock.
        private static readonly object TimerLock = new object();

        private static CancellationTokenSource _cancellationTokenSource;
        private static ElapsedEventHandler _elapsedHandler;

        // Only one scheduling pass may be in flight. The timer keeps ticking
        // while a pass runs on the thread pool, so without this two passes could
        // overlap and fight over Timer.Enabled.
        private static int _executing;

        public Scheduler(ITaskManager taskManager, IManageCommandQueue commandQueueManager, Logger logger)
        {
            _taskManager = taskManager;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        internal static bool TimerEnabled
        {
            get
            {
                lock (TimerLock)
                {
                    return Timer.Enabled;
                }
            }

            set
            {
                lock (TimerLock)
                {
                    Timer.Enabled = value;
                }
            }
        }

        internal void ExecuteCommands()
        {
            if (Interlocked.CompareExchange(ref _executing, 1, 0) != 0)
            {
                _logger.Trace("Scheduling pass already in progress, skipping this tick");
                return;
            }

            try
            {
                TimerEnabled = false;

                var tasks = _taskManager.GetPending().ToList();

                _logger.Trace("Pending Tasks: {0}", tasks.Count);

                foreach (var task in tasks)
                {
                    _commandQueueManager.Push(task.TypeName, task.LastExecution, task.LastStartTime, task.Priority, CommandTrigger.Scheduled);
                }
            }
            finally
            {
                // Nothing in here may throw. An exception escaping this block
                // replaces the real failure from the try and, worse, leaves the
                // timer disabled for the life of the process - every scheduled
                // task then silently stops until Gamarr is restarted.
                try
                {
                    // A null source means ApplicationStartedEvent has not been
                    // handled (or its write is not yet visible to this thread).
                    // Nobody has asked us to stop, so treat it as "not cancelled"
                    // and keep the scheduler running.
                    var cancellationTokenSource = _cancellationTokenSource;

                    if (cancellationTokenSource is null || !cancellationTokenSource.IsCancellationRequested)
                    {
                        TimerEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to reschedule the task timer, scheduled tasks may be stopped");
                }
                finally
                {
                    Interlocked.Exchange(ref _executing, 0);
                }
            }
        }

        public void Handle(ApplicationStartedEvent message)
        {
            lock (TimerLock)
            {
                _cancellationTokenSource = new CancellationTokenSource();

                Timer.Interval = 1000 * 30;

                // The timer is static but this handler runs once per Scheduler
                // instance, and an in-process restart publishes the event again.
                // Replace the subscription instead of stacking a second one, so
                // a tick cannot start two overlapping passes (and cannot call
                // back into an instance from a disposed container).
                if (_elapsedHandler != null)
                {
                    Timer.Elapsed -= _elapsedHandler;
                }

                _elapsedHandler = OnTimerElapsed;
                Timer.Elapsed += _elapsedHandler;

                Timer.Start();
            }
        }

        public void Handle(ApplicationShutdownRequested message)
        {
            _logger.Info("Shutting down scheduler");

            // Shutdown can be requested before ApplicationStartedEvent has been
            // handled, which left this field null and threw out of the handler.
            _cancellationTokenSource?.Cancel(true);

            lock (TimerLock)
            {
                Timer.Stop();
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs args)
        {
            var cancellationTokenSource = _cancellationTokenSource;

            Task.Factory.StartNew(ExecuteCommands, cancellationTokenSource?.Token ?? CancellationToken.None)
                .LogExceptions();
        }
    }
}
