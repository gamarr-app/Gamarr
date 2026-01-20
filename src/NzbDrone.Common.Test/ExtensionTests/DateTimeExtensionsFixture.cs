using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Test.ExtensionTests
{
    [TestFixture]
    public class DateTimeExtensionsFixture
    {
        [Test]
        public void InNextDays_should_return_true_for_tomorrow()
        {
            var tomorrow = DateTime.UtcNow.AddDays(1);

            tomorrow.InNextDays(7).Should().BeTrue();
        }

        [Test]
        public void InNextDays_should_return_false_for_yesterday()
        {
            var yesterday = DateTime.UtcNow.AddDays(-1);

            yesterday.InNextDays(7).Should().BeFalse();
        }

        [Test]
        public void InLastDays_should_return_true_for_yesterday()
        {
            var yesterday = DateTime.UtcNow.AddDays(-1);

            yesterday.InLastDays(7).Should().BeTrue();
        }

        [Test]
        public void InLastDays_should_return_false_for_tomorrow()
        {
            var tomorrow = DateTime.UtcNow.AddDays(1);

            tomorrow.InLastDays(7).Should().BeFalse();
        }

        [Test]
        public void InNext_should_return_true_within_timespan()
        {
            var future = DateTime.UtcNow.AddHours(1);

            future.InNext(TimeSpan.FromHours(2)).Should().BeTrue();
        }

        [Test]
        public void InNext_should_return_false_beyond_timespan()
        {
            var future = DateTime.UtcNow.AddHours(3);

            future.InNext(TimeSpan.FromHours(2)).Should().BeFalse();
        }

        [Test]
        public void InLast_should_return_true_within_timespan()
        {
            var past = DateTime.UtcNow.AddHours(-1);

            past.InLast(TimeSpan.FromHours(2)).Should().BeTrue();
        }

        [Test]
        public void InLast_should_return_false_beyond_timespan()
        {
            var past = DateTime.UtcNow.AddHours(-3);

            past.InLast(TimeSpan.FromHours(2)).Should().BeFalse();
        }

        [Test]
        public void Before_should_return_true_when_before()
        {
            var earlier = DateTime.UtcNow;
            var later = DateTime.UtcNow.AddDays(1);

            earlier.Before(later).Should().BeTrue();
        }

        [Test]
        public void Before_should_return_false_when_after()
        {
            var earlier = DateTime.UtcNow;
            var later = DateTime.UtcNow.AddDays(1);

            later.Before(earlier).Should().BeFalse();
        }

        [Test]
        public void Before_should_return_true_when_equal()
        {
            var now = DateTime.UtcNow;

            now.Before(now).Should().BeTrue();
        }

        [Test]
        public void After_should_return_true_when_after()
        {
            var earlier = DateTime.UtcNow;
            var later = DateTime.UtcNow.AddDays(1);

            later.After(earlier).Should().BeTrue();
        }

        [Test]
        public void After_should_return_false_when_before()
        {
            var earlier = DateTime.UtcNow;
            var later = DateTime.UtcNow.AddDays(1);

            earlier.After(later).Should().BeFalse();
        }

        [Test]
        public void After_should_return_true_when_equal()
        {
            var now = DateTime.UtcNow;

            now.After(now).Should().BeTrue();
        }

        [Test]
        public void Between_should_return_true_when_in_range()
        {
            var start = DateTime.UtcNow.AddDays(-1);
            var middle = DateTime.UtcNow;
            var end = DateTime.UtcNow.AddDays(1);

            middle.Between(start, end).Should().BeTrue();
        }

        [Test]
        public void Between_should_return_false_when_before_range()
        {
            var start = DateTime.UtcNow;
            var end = DateTime.UtcNow.AddDays(1);
            var before = DateTime.UtcNow.AddDays(-1);

            before.Between(start, end).Should().BeFalse();
        }

        [Test]
        public void Between_should_return_false_when_after_range()
        {
            var start = DateTime.UtcNow.AddDays(-1);
            var end = DateTime.UtcNow;
            var after = DateTime.UtcNow.AddDays(1);

            after.Between(start, end).Should().BeFalse();
        }

        [Test]
        public void Epoch_should_be_correct()
        {
            DateTimeExtensions.Epoch.Year.Should().Be(1970);
            DateTimeExtensions.Epoch.Month.Should().Be(1);
            DateTimeExtensions.Epoch.Day.Should().Be(1);
            DateTimeExtensions.Epoch.Kind.Should().Be(DateTimeKind.Utc);
        }
    }
}
