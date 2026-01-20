using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.EnsureThat;

namespace NzbDrone.Common.Test.EnsureTests
{
    [TestFixture]
    public class EnsureIntExtensionsFixture
    {
        [Test]
        public void IsLessThan_should_pass_when_less()
        {
            var value = 5;
            Action action = () => Ensure.That(value, () => value).IsLessThan(10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsLessThan_should_throw_when_equal()
        {
            var value = 10;
            Action action = () => Ensure.That(value, () => value).IsLessThan(10);
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsLessThan_should_throw_when_greater()
        {
            var value = 15;
            Action action = () => Ensure.That(value, () => value).IsLessThan(10);
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsLessThanOrEqualTo_should_pass_when_less()
        {
            var value = 5;
            Action action = () => Ensure.That(value, () => value).IsLessThanOrEqualTo(10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsLessThanOrEqualTo_should_pass_when_equal()
        {
            var value = 10;
            Action action = () => Ensure.That(value, () => value).IsLessThanOrEqualTo(10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsLessThanOrEqualTo_should_throw_when_greater()
        {
            var value = 15;
            Action action = () => Ensure.That(value, () => value).IsLessThanOrEqualTo(10);
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsGreaterThan_should_pass_when_greater()
        {
            var value = 15;
            Action action = () => Ensure.That(value, () => value).IsGreaterThan(10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsGreaterThan_should_throw_when_equal()
        {
            var value = 10;
            Action action = () => Ensure.That(value, () => value).IsGreaterThan(10);
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsGreaterThan_should_throw_when_less()
        {
            var value = 5;
            Action action = () => Ensure.That(value, () => value).IsGreaterThan(10);
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsGreaterThanZero_should_pass_when_positive()
        {
            var value = 5;
            Action action = () => Ensure.That(value, () => value).IsGreaterThanZero();
            action.Should().NotThrow();
        }

        [Test]
        public void IsGreaterThanZero_should_throw_when_zero()
        {
            var value = 0;
            Action action = () => Ensure.That(value, () => value).IsGreaterThanZero();
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsGreaterThanZero_should_throw_when_negative()
        {
            var value = -5;
            Action action = () => Ensure.That(value, () => value).IsGreaterThanZero();
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsGreaterOrEqualTo_should_pass_when_greater()
        {
            var value = 15;
            Action action = () => Ensure.That(value, () => value).IsGreaterOrEqualTo(10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsGreaterOrEqualTo_should_pass_when_equal()
        {
            var value = 10;
            Action action = () => Ensure.That(value, () => value).IsGreaterOrEqualTo(10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsGreaterOrEqualTo_should_throw_when_less()
        {
            var value = 5;
            Action action = () => Ensure.That(value, () => value).IsGreaterOrEqualTo(10);
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsInRange_should_pass_when_in_range()
        {
            var value = 5;
            Action action = () => Ensure.That(value, () => value).IsInRange(1, 10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsInRange_should_pass_when_at_min()
        {
            var value = 1;
            Action action = () => Ensure.That(value, () => value).IsInRange(1, 10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsInRange_should_pass_when_at_max()
        {
            var value = 10;
            Action action = () => Ensure.That(value, () => value).IsInRange(1, 10);
            action.Should().NotThrow();
        }

        [Test]
        public void IsInRange_should_throw_when_below_min()
        {
            var value = 0;
            Action action = () => Ensure.That(value, () => value).IsInRange(1, 10);
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void IsInRange_should_throw_when_above_max()
        {
            var value = 11;
            Action action = () => Ensure.That(value, () => value).IsInRange(1, 10);
            action.Should().Throw<ArgumentException>();
        }
    }
}
