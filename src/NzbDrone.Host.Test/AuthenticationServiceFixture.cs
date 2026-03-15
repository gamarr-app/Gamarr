using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;
using Gamarr.Http.Authentication;

namespace NzbDrone.App.Test
{
    [TestFixture]
    public class AuthenticationServiceFixture : TestBase<AuthenticationService>
    {
        private const string ValidUsername = "admin";
        private const string ValidPassword = "password123";

        private User _validUser;

        [SetUp]
        public void Setup()
        {
            _validUser = new User
            {
                Id = 1,
                Username = ValidUsername,
                Password = "hashed",
                Identifier = System.Guid.NewGuid()
            };

            Mocker.GetMock<IConfigFileProvider>()
                .Setup(c => c.AuthenticationMethod)
                .Returns(AuthenticationType.Forms);
        }

        private HttpRequest CreateMockRequest()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            return context.Request;
        }

        [Test]
        public void login_should_return_user_with_valid_credentials()
        {
            Mocker.GetMock<IUserService>()
                .Setup(s => s.FindUser(ValidUsername, ValidPassword))
                .Returns(_validUser);

            var result = Subject.Login(CreateMockRequest(), ValidUsername, ValidPassword);

            result.Should().NotBeNull();
            result.Username.Should().Be(ValidUsername);
        }

        [Test]
        public void login_should_return_null_with_invalid_credentials()
        {
            Mocker.GetMock<IUserService>()
                .Setup(s => s.FindUser(ValidUsername, "wrongpassword"))
                .Returns((User)null);

            var result = Subject.Login(CreateMockRequest(), ValidUsername, "wrongpassword");

            result.Should().BeNull();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void login_should_return_null_with_unknown_user()
        {
            Mocker.GetMock<IUserService>()
                .Setup(s => s.FindUser("unknown", ValidPassword))
                .Returns((User)null);

            var result = Subject.Login(CreateMockRequest(), "unknown", ValidPassword);

            result.Should().BeNull();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void login_should_return_null_when_auth_disabled()
        {
            Mocker.GetMock<IConfigFileProvider>()
                .Setup(c => c.AuthenticationMethod)
                .Returns(AuthenticationType.None);

            // Re-resolve to pick up new config
            var service = Mocker.Resolve<AuthenticationService>();

            var result = service.Login(CreateMockRequest(), ValidUsername, ValidPassword);

            result.Should().BeNull();
        }

        [Test]
        public void logout_should_not_throw_when_user_is_null()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            // No exception expected
            Subject.Logout(context);
        }

        [Test]
        public void logout_should_not_throw_when_user_is_authenticated()
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, ValidUsername) };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "test");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);

            // No exception expected
            Subject.Logout(context);
        }
    }
}
