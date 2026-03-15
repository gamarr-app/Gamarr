using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using NLog.Targets;
using NUnit.Framework;

namespace NzbDrone.Test.Common
{
    public class ExceptionVerification : Target
    {
        private static readonly AsyncLocal<TestLogContext> _context = new AsyncLocal<TestLogContext>();

        private static TestLogContext CurrentContext
        {
            get
            {
                _context.Value ??= new TestLogContext();
                return _context.Value;
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var ctx = CurrentContext;
            lock (ctx.Logs)
            {
                if (logEvent.Level >= LogLevel.Warn)
                {
                    ctx.Logs.Add(logEvent);
                    ctx.WaitEvent.Set();
                }
            }
        }

        public static void Reset()
        {
            _context.Value = new TestLogContext();
        }

        public static void AssertNoUnexpectedLogs()
        {
            ExpectedFatals(0);
            ExpectedErrors(0);
            ExpectedWarns(0);
        }

        private static string GetLogsString(IEnumerable<LogEventInfo> logs)
        {
            var errors = "";
            foreach (var log in logs)
            {
                var exception = "";
                if (log.Exception != null)
                {
                    exception = string.Format("[{0}: {1}]", log.Exception.GetType(), log.Exception.Message);
                }

                errors += Environment.NewLine + string.Format("[{0}] {1}: {2} {3}", log.Level, log.LoggerName, log.FormattedMessage, exception);
            }

            return errors;
        }

        public static void WaitForErrors(int count, int msec)
        {
            var ctx = CurrentContext;

            while (true)
            {
                lock (ctx.Logs)
                {
                    var levelLogs = ctx.Logs.Where(l => l.Level == LogLevel.Error).ToList();

                    if (levelLogs.Count >= count)
                    {
                        break;
                    }

                    ctx.WaitEvent.Reset();
                }

                if (!ctx.WaitEvent.Wait(msec))
                {
                    break;
                }
            }

            Expected(LogLevel.Error, count);
        }

        public static void ExpectedErrors(int count)
        {
            Expected(LogLevel.Error, count);
        }

        public static void ExpectedFatals(int count)
        {
            Expected(LogLevel.Fatal, count);
        }

        public static void ExpectedWarns(int count)
        {
            Expected(LogLevel.Warn, count);
        }

        public static void IgnoreWarns()
        {
            Ignore(LogLevel.Warn);
        }

        public static void IgnoreErrors()
        {
            Ignore(LogLevel.Error);
        }

        public static void MarkInconclusive(Type exception)
        {
            var ctx = CurrentContext;
            lock (ctx.Logs)
            {
                var inconclusiveLogs = ctx.Logs.Where(l => l.Exception != null && l.Exception.GetType() == exception).ToList();

                if (inconclusiveLogs.Any())
                {
                    inconclusiveLogs.ForEach(c => ctx.Logs.Remove(c));
                    Assert.Inconclusive(GetLogsString(inconclusiveLogs));
                }
            }
        }

        public static void MarkInconclusive(string text)
        {
            var ctx = CurrentContext;
            lock (ctx.Logs)
            {
                var inconclusiveLogs = ctx.Logs.Where(l => l.FormattedMessage.ToLower().Contains(text.ToLower())).ToList();

                if (inconclusiveLogs.Any())
                {
                    inconclusiveLogs.ForEach(c => ctx.Logs.Remove(c));
                    Assert.Inconclusive(GetLogsString(inconclusiveLogs));
                }
            }
        }

        private static void Expected(LogLevel level, int count)
        {
            var ctx = CurrentContext;
            lock (ctx.Logs)
            {
                var levelLogs = ctx.Logs.Where(l => l.Level == level).ToList();

                if (levelLogs.Count != count)
                {
                    var message = string.Format("{0} {1}(s) were expected but {2} were logged.\n\r{3}",
                        count,
                        level,
                        levelLogs.Count,
                        GetLogsString(levelLogs));

                    message = "\n\r****************************************************************************************\n\r"
                        + message +
                        "\n\r****************************************************************************************";

                    Assert.Fail(message);
                }

                levelLogs.ForEach(c => ctx.Logs.Remove(c));
            }
        }

        private static void Ignore(LogLevel level)
        {
            var ctx = CurrentContext;
            lock (ctx.Logs)
            {
                var levelLogs = ctx.Logs.Where(l => l.Level == level).ToList();
                levelLogs.ForEach(c => ctx.Logs.Remove(c));
            }
        }

        private class TestLogContext
        {
            public List<LogEventInfo> Logs { get; } = new List<LogEventInfo>();
            public ManualResetEventSlim WaitEvent { get; } = new ManualResetEventSlim();
        }
    }
}
