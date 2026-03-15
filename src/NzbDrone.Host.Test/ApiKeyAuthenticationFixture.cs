using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using Gamarr.Http.Authentication;

namespace NzbDrone.App.Test
{
    [TestFixture]
    public class ApiKeyAuthenticationFixture
    {
        private const string ValidApiKey = "test-api-key-1234567890";
        private const string InvalidApiKey = "wrong-api-key";

        private ApiKeyAuthenticationHandler _handler;
        private ApiKeyAuthenticationOptions _options;

        [SetUp]
        public void Setup()
        {
            _options = new ApiKeyAuthenticationOptions
            {
                HeaderName = "X-Api-Key",
                QueryName = "apikey"
            };

            var configMock = new Mock<IConfigFileProvider>();
            configMock.Setup(c => c.ApiKey).Returns(ValidApiKey);

            var optionsMonitor = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
            optionsMonitor.Setup(o => o.Get(It.IsAny<string>())).Returns(_options);
            optionsMonitor.Setup(o => o.CurrentValue).Returns(_options);

            var loggerFactory = NullLoggerFactory.Instance;

            _handler = new ApiKeyAuthenticationHandler(
                optionsMonitor.Object,
                loggerFactory,
                new UrlTestEncoder(),
                configMock.Object);
        }

        private async Task<AuthenticateResult> Authenticate(Action<HttpContext> configureContext)
        {
            var scheme = new AuthenticationScheme("API", null, typeof(ApiKeyAuthenticationHandler));
            var context = new DefaultHttpContext();
            configureContext(context);

            await _handler.InitializeAsync(scheme, context);
            return await _handler.AuthenticateAsync();
        }

        [Test]
        public async Task should_authenticate_with_valid_api_key_in_header()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.Headers["X-Api-Key"] = ValidApiKey);

            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
        }

        [Test]
        public async Task should_authenticate_with_valid_api_key_in_query()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.QueryString = new QueryString($"?apikey={ValidApiKey}"));

            result.Succeeded.Should().BeTrue();
        }

        [Test]
        public async Task should_authenticate_with_valid_api_key_in_bearer_token()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.Headers["Authorization"] = $"Bearer {ValidApiKey}");

            result.Succeeded.Should().BeTrue();
        }

        [Test]
        public async Task should_not_authenticate_with_invalid_api_key_in_header()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.Headers["X-Api-Key"] = InvalidApiKey);

            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task should_not_authenticate_with_invalid_api_key_in_query()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.QueryString = new QueryString($"?apikey={InvalidApiKey}"));

            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task should_not_authenticate_with_invalid_bearer_token()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.Headers["Authorization"] = $"Bearer {InvalidApiKey}");

            result.Succeeded.Should().BeFalse();
        }

        [Test]
        public async Task should_return_no_result_when_no_api_key_provided()
        {
            var result = await Authenticate(ctx => { });

            result.Succeeded.Should().BeFalse();
            result.None.Should().BeTrue();
        }

        [Test]
        public async Task should_return_no_result_with_empty_api_key_header()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.Headers["X-Api-Key"] = "");

            result.Succeeded.Should().BeFalse();
            result.None.Should().BeTrue();
        }

        [Test]
        public async Task should_prefer_query_parameter_over_header()
        {
            var result = await Authenticate(ctx =>
            {
                ctx.Request.QueryString = new QueryString($"?apikey={ValidApiKey}");
                ctx.Request.Headers["X-Api-Key"] = InvalidApiKey;
            });

            result.Succeeded.Should().BeTrue();
        }

        [Test]
        public async Task should_have_api_key_claim_on_success()
        {
            var result = await Authenticate(ctx =>
                ctx.Request.Headers["X-Api-Key"] = ValidApiKey);

            result.Succeeded.Should().BeTrue();
            result.Principal.HasClaim("ApiKey", "true").Should().BeTrue();
        }
    }
}
