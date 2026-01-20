using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class HttpResponseFixture
    {
        private HttpRequest _request;
        private HttpHeader _headers;

        [SetUp]
        public void Setup()
        {
            _request = new HttpRequest("http://example.com");
            _headers = new HttpHeader();
        }

        [Test]
        public void constructor_with_byte_array_should_set_properties()
        {
            var data = new byte[] { 1, 2, 3 };
            var response = new HttpResponse(_request, _headers, data, HttpStatusCode.OK);

            response.Request.Should().Be(_request);
            response.Headers.Should().BeSameAs(_headers);
            response.ResponseData.Should().BeEquivalentTo(data);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public void constructor_with_string_should_set_properties()
        {
            var content = "test content";
            var response = new HttpResponse(_request, _headers, content, HttpStatusCode.OK);

            response.Request.Should().Be(_request);
            response.Content.Should().Be(content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public void content_should_decode_response_data()
        {
            var content = "test content";
            var data = System.Text.Encoding.UTF8.GetBytes(content);
            var response = new HttpResponse(_request, _headers, data, HttpStatusCode.OK);

            response.Content.Should().Be(content);
        }

        [Test]
        public void HasHttpError_should_return_true_for_400_plus_status()
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>(), HttpStatusCode.BadRequest);

            response.HasHttpError.Should().BeTrue();
        }

        [Test]
        public void HasHttpError_should_return_false_for_200_status()
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>(), HttpStatusCode.OK);

            response.HasHttpError.Should().BeFalse();
        }

        [Test]
        public void HasHttpServerError_should_return_true_for_500_plus_status()
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>(), HttpStatusCode.InternalServerError);

            response.HasHttpServerError.Should().BeTrue();
        }

        [Test]
        public void HasHttpServerError_should_return_false_for_400_status()
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>(), HttpStatusCode.BadRequest);

            response.HasHttpServerError.Should().BeFalse();
        }

        [Test]
        [TestCase(HttpStatusCode.Moved)]
        [TestCase(HttpStatusCode.MovedPermanently)]
        [TestCase(HttpStatusCode.Found)]
        [TestCase(HttpStatusCode.TemporaryRedirect)]
        [TestCase(HttpStatusCode.RedirectMethod)]
        [TestCase(HttpStatusCode.SeeOther)]
        [TestCase(HttpStatusCode.PermanentRedirect)]
        public void HasHttpRedirect_should_return_true_for_redirect_status(HttpStatusCode statusCode)
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>(), statusCode);

            response.HasHttpRedirect.Should().BeTrue();
        }

        [Test]
        public void HasHttpRedirect_should_return_false_for_ok_status()
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>(), HttpStatusCode.OK);

            response.HasHttpRedirect.Should().BeFalse();
        }

        [Test]
        public void GetCookieHeaders_should_return_empty_array_when_no_cookies()
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>());

            response.GetCookieHeaders().Should().BeEmpty();
        }

        [Test]
        public void GetCookieHeaders_should_return_set_cookie_values()
        {
            _headers.Add("Set-Cookie", "cookie1=value1");
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>());

            var cookies = response.GetCookieHeaders();

            cookies.Should().Contain("cookie1=value1");
        }

        [Test]
        public void GetCookies_should_parse_cookie_headers()
        {
            _headers.Add("Set-Cookie", "cookie1=value1");
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>());

            var cookies = response.GetCookies();

            cookies.Should().ContainKey("cookie1");
            cookies["cookie1"].Should().Be("value1");
        }

        [Test]
        public void GetCookies_should_return_empty_dict_when_no_cookies()
        {
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>());

            var cookies = response.GetCookies();

            cookies.Should().BeEmpty();
        }

        [Test]
        public void ToString_should_contain_request_info()
        {
            var response = new HttpResponse(_request, _headers, new byte[10], HttpStatusCode.OK);

            var result = response.ToString();

            result.Should().Contain("Res:");
            result.Should().Contain("example.com");
            result.Should().Contain("10 bytes");
        }

        [Test]
        public void ToString_should_include_content_for_error_with_non_html_content()
        {
            _headers.ContentType = "application/json";
            var response = new HttpResponse(_request, _headers, "error content", HttpStatusCode.BadRequest);

            var result = response.ToString();

            result.Should().Contain("error content");
        }

        [Test]
        public void constructor_should_set_version()
        {
            var version = new Version(1, 1);
            var response = new HttpResponse(_request, _headers, Array.Empty<byte>(), HttpStatusCode.OK, version);

            response.Version.Should().Be(version);
        }
    }
}
