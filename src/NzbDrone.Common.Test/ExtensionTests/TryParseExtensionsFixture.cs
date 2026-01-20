using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Test.ExtensionTests
{
    [TestFixture]
    public class TryParseExtensionsFixture
    {
        [Test]
        public void ParseInt32_should_parse_valid_int()
        {
            var result = "123".ParseInt32();

            result.Should().Be(123);
        }

        [Test]
        public void ParseInt32_should_return_null_for_invalid()
        {
            var result = "abc".ParseInt32();

            result.Should().BeNull();
        }

        [Test]
        public void ParseInt32_should_return_null_for_empty()
        {
            var result = "".ParseInt32();

            result.Should().BeNull();
        }

        [Test]
        public void ParseInt32_should_parse_negative()
        {
            var result = "-42".ParseInt32();

            result.Should().Be(-42);
        }

        [Test]
        public void ParseInt64_should_parse_valid_long()
        {
            var result = "1234567890123".ParseInt64();

            result.Should().Be(1234567890123L);
        }

        [Test]
        public void ParseInt64_should_return_null_for_invalid()
        {
            var result = "abc".ParseInt64();

            result.Should().BeNull();
        }

        [Test]
        public void ParseInt64_should_return_null_for_empty()
        {
            var result = "".ParseInt64();

            result.Should().BeNull();
        }

        [Test]
        public void ParseInt64_should_parse_negative()
        {
            var result = "-9876543210".ParseInt64();

            result.Should().Be(-9876543210L);
        }

        [Test]
        public void ParseDouble_should_parse_with_period()
        {
            var result = "3.14".ParseDouble();

            result.Should().BeApproximately(3.14, 0.001);
        }

        [Test]
        public void ParseDouble_should_parse_with_comma()
        {
            var result = "3,14".ParseDouble();

            result.Should().BeApproximately(3.14, 0.001);
        }

        [Test]
        public void ParseDouble_should_return_null_for_invalid()
        {
            var result = "abc".ParseDouble();

            result.Should().BeNull();
        }

        [Test]
        public void ParseDouble_should_return_null_for_empty()
        {
            var result = "".ParseDouble();

            result.Should().BeNull();
        }

        [Test]
        public void ParseDouble_should_parse_negative()
        {
            var result = "-2.5".ParseDouble();

            result.Should().BeApproximately(-2.5, 0.001);
        }

        [Test]
        public void ParseDouble_should_parse_integer()
        {
            var result = "42".ParseDouble();

            result.Should().BeApproximately(42.0, 0.001);
        }
    }
}
