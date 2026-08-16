using System;
using System.Globalization;
using System.Linq;
using System.Net;
using FluentAssertions;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Sentry;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.InstrumentationTests
{
    [TestFixture]
    public class SentryTargetFixture : TestBase
    {
        private SentryTarget _subject;

        private static LogLevel[] AllLevels = LogLevel.AllLevels.ToArray();
        private static LogLevel[] SentryLevels = LogLevel.AllLevels.Where(x => x >= LogLevel.Error).ToArray();
        private static LogLevel[] OtherLevels = AllLevels.Except(SentryLevels).ToArray();

        // TODO: SQLiteException filtering tests don't work on linux-86 and alpine customer Azure agents due to sqlite library not being loaded up, pass local
        private static Exception[] FilteredExceptions = new Exception[]
        {
            // new SQLiteException(SQLiteErrorCode.Locked, "database is locked"),
            new UnauthorizedAccessException(),
            new AggregateException(new Exception[]
            {
                new UnauthorizedAccessException(),
                new UnauthorizedAccessException()
            })
        };

        private static Exception[] NonFilteredExceptions = new Exception[]
        {
            // new SQLiteException(SQLiteErrorCode.Error, "it's borked"),
            new AggregateException(new Exception[]
            {
                new UnauthorizedAccessException(),
                new NotImplementedException()
            })
        };

        [SetUp]
        public void Setup()
        {
            _subject = new SentryTarget("https://aaaaaaaaaaaaaaaaaaaaaaaaaa@sentry.io/111111", Mocker.GetMock<IAppFolderInfo>().Object);
        }

        private LogEventInfo GivenLogEvent(LogLevel level, Exception ex, string message)
        {
            return LogEventInfo.Create(level, "SentryTest", ex, CultureInfo.InvariantCulture, message);
        }

        [Test]
        [TestCaseSource("AllLevels")]
        public void log_without_error_is_not_sentry_event(LogLevel level)
        {
            _subject.IsSentryMessage(GivenLogEvent(level, null, "test")).Should().BeFalse();
        }

        [Test]
        [TestCaseSource("SentryLevels")]
        public void error_or_worse_with_exception_is_sentry_event(LogLevel level)
        {
            _subject.IsSentryMessage(GivenLogEvent(level, new Exception(), "test")).Should().BeTrue();
        }

        [Test]
        [TestCaseSource("OtherLevels")]
        public void less_than_error_with_exception_is_not_sentry_event(LogLevel level)
        {
            _subject.IsSentryMessage(GivenLogEvent(level, new Exception(), "test")).Should().BeFalse();
        }

        [Test]
        [TestCaseSource("FilteredExceptions")]
        public void should_filter_event_for_filtered_exception_types(Exception ex)
        {
            var log = GivenLogEvent(LogLevel.Error, ex, "test");
            _subject.IsSentryMessage(log).Should().BeFalse();
        }

        [Test]
        [TestCaseSource("NonFilteredExceptions")]
        public void should_not_filter_event_for_filtered_exception_types(Exception ex)
        {
            var log = GivenLogEvent(LogLevel.Error, ex, "test");
            _subject.IsSentryMessage(log).Should().BeTrue();
        }

        [Test]
        [TestCaseSource("FilteredExceptions")]
        public void should_not_filter_event_for_filtered_exception_types_if_filtering_disabled(Exception ex)
        {
            _subject.FilterEvents = false;
            var log = GivenLogEvent(LogLevel.Error, ex, "test");
            _subject.IsSentryMessage(log).Should().BeTrue();
        }

        [Test]
        [TestCaseSource(typeof(SentryTarget), "FilteredExceptionMessages")]
        public void should_filter_event_for_filtered_exception_messages(string message)
        {
            var log = GivenLogEvent(LogLevel.Error, new Exception("aaaaaaa" + message + "bbbbbbb"), "test");
            _subject.IsSentryMessage(log).Should().BeFalse();
        }

        [TestCase("A message that isn't filtered")]
        [TestCase("Error")]
        public void should_not_filter_event_for_exception_messages_that_are_not_filtered(string message)
        {
            var log = GivenLogEvent(LogLevel.Error, new Exception(message), "test");
            _subject.IsSentryMessage(log).Should().BeTrue();
        }

        [TestCaseSource(typeof(SentryTarget), "FilteredExceptionMessages")]
        public void should_filter_event_for_filtered_exception_messages_on_inner_exception(string message)
        {
            var log = GivenLogEvent(LogLevel.Error, new Exception("outer", new Exception("aaaaaaa" + message + "bbbbbbb")), "test");
            _subject.IsSentryMessage(log).Should().BeFalse();
        }

        private static HttpException GivenHttpException(HttpStatusCode statusCode)
        {
            var request = new HttpRequest("https://indexer.example.com/api");

            return new HttpException(request, new HttpResponse(request, new HttpHeader(), string.Empty, statusCode));
        }

        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.PaymentRequired)]
        public void should_filter_event_for_http_exceptions_caused_by_user_configuration(HttpStatusCode statusCode)
        {
            var log = GivenLogEvent(LogLevel.Error, GivenHttpException(statusCode), "test");
            _subject.IsSentryMessage(log).Should().BeFalse();
        }

        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.BadRequest)]
        public void should_not_filter_event_for_other_http_exceptions(HttpStatusCode statusCode)
        {
            var log = GivenLogEvent(LogLevel.Error, GivenHttpException(statusCode), "test");
            _subject.IsSentryMessage(log).Should().BeTrue();
        }

        [Test]
        public void should_filter_event_for_wrapped_http_exception_caused_by_user_configuration()
        {
            var log = GivenLogEvent(LogLevel.Error, new Exception("Failed to fetch releases", GivenHttpException(HttpStatusCode.Forbidden)), "test");
            _subject.IsSentryMessage(log).Should().BeFalse();
        }
    }
}
