using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Test.ExtensionTests
{
    [TestFixture]
    public class Base64ExtensionsFixture
    {
        [Test]
        public void ToBase64_bytes_should_encode_properly()
        {
            var bytes = new byte[] { 1, 2, 3 };

            var result = bytes.ToBase64();

            result.Should().Be("AQID");
        }

        [Test]
        public void ToBase64_bytes_should_handle_empty_array()
        {
            var bytes = System.Array.Empty<byte>();

            var result = bytes.ToBase64();

            result.Should().BeEmpty();
        }

        [Test]
        public void ToBase64_long_should_encode_properly()
        {
            var value = 12345L;

            var result = value.ToBase64();

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void ToBase64_long_zero_should_encode()
        {
            var value = 0L;

            var result = value.ToBase64();

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void ToBase64_long_max_value_should_encode()
        {
            var value = long.MaxValue;

            var result = value.ToBase64();

            result.Should().NotBeNullOrEmpty();
        }
    }
}
