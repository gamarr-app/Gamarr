using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Test.ExtensionTests
{
    [TestFixture]
    public class NumberExtensionsFixture
    {
        [Test]
        public void SizeSuffix_should_return_bytes_for_small_values()
        {
            var result = 500L.SizeSuffix();

            result.Should().Contain("B");
        }

        [Test]
        public void SizeSuffix_should_return_kb_for_kilobytes()
        {
            var result = (1024L * 10).SizeSuffix();

            result.Should().Contain("KB");
        }

        [Test]
        public void SizeSuffix_should_return_mb_for_megabytes()
        {
            var result = (1024L * 1024L * 10).SizeSuffix();

            result.Should().Contain("MB");
        }

        [Test]
        public void SizeSuffix_should_return_gb_for_gigabytes()
        {
            var result = (1024L * 1024L * 1024L * 10).SizeSuffix();

            result.Should().Contain("GB");
        }

        [Test]
        public void SizeSuffix_should_handle_zero()
        {
            var result = 0L.SizeSuffix();

            result.Should().Be("0 B");
        }

        [Test]
        public void SizeSuffix_should_handle_negative()
        {
            var result = (-1024L).SizeSuffix();

            result.Should().StartWith("-");
        }

        [Test]
        public void Megabytes_int_should_return_bytes()
        {
            var result = 1.Megabytes();

            result.Should().Be(1024L * 1024L);
        }

        [Test]
        public void Megabytes_int_should_multiply_correctly()
        {
            var result = 10.Megabytes();

            result.Should().Be(10L * 1024L * 1024L);
        }

        [Test]
        public void Gigabytes_int_should_return_bytes()
        {
            var result = 1.Gigabytes();

            result.Should().Be(1024L * 1024L * 1024L);
        }

        [Test]
        public void Gigabytes_int_should_multiply_correctly()
        {
            var result = 5.Gigabytes();

            result.Should().Be(5L * 1024L * 1024L * 1024L);
        }

        [Test]
        public void Megabytes_double_should_return_bytes()
        {
            var result = 1.5.Megabytes();

            result.Should().BeGreaterThan(1024L * 1024L);
        }

        [Test]
        public void Gigabytes_double_should_return_bytes()
        {
            var result = 1.5.Gigabytes();

            result.Should().BeGreaterThan(1024L * 1024L * 1024L);
        }
    }
}
