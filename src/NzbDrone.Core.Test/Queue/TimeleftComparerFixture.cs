using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Queue
{
    [TestFixture]
    public class TimeleftComparerFixture : CoreTest
    {
        private TimeleftComparer _comparer;

        [SetUp]
        public void Setup()
        {
            _comparer = new TimeleftComparer();
        }

        [Test]
        public void should_return_zero_when_both_null()
        {
            _comparer.Compare(null, null).Should().Be(0);
        }

        [Test]
        public void should_return_positive_when_first_is_null()
        {
            _comparer.Compare(null, TimeSpan.FromMinutes(5)).Should().Be(1);
        }

        [Test]
        public void should_return_negative_when_second_is_null()
        {
            _comparer.Compare(TimeSpan.FromMinutes(5), null).Should().Be(-1);
        }

        [Test]
        public void should_return_negative_when_first_is_less()
        {
            _comparer.Compare(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)).Should().BeLessThan(0);
        }

        [Test]
        public void should_return_positive_when_first_is_greater()
        {
            _comparer.Compare(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5)).Should().BeGreaterThan(0);
        }

        [Test]
        public void should_return_zero_when_equal()
        {
            _comparer.Compare(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)).Should().Be(0);
        }
    }
}
