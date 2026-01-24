using System.Net;
using FluentAssertions;
using Gamarr.Http.Exceptions;
using NUnit.Framework;

namespace NzbDrone.Api.Test.Exceptions
{
    [TestFixture]
    public class InvalidApiKeyExceptionFixture
    {
        [Test]
        public void should_have_unauthorized_status_code()
        {
            var exception = new InvalidApiKeyException();

            exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public void should_have_unauthorized_status_code_with_message()
        {
            var exception = new InvalidApiKeyException("bad key");

            exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Test]
        public void should_include_message_in_content()
        {
            var exception = new InvalidApiKeyException("bad key");

            exception.Content.Should().Be("bad key");
        }

        [Test]
        public void should_have_null_content_without_message()
        {
            var exception = new InvalidApiKeyException();

            exception.Content.Should().BeNull();
        }

        [Test]
        public void should_format_exception_message_with_status_and_content()
        {
            var exception = new InvalidApiKeyException("bad key");

            exception.Message.Should().Be("Unauthorized: bad key");
        }
    }
}
