using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;
using Gamarr.Http.Authentication;

namespace NzbDrone.App.Test
{
    [TestFixture]
    public class AuthenticationFixture : TestBase
    {
        private ServiceProvider BuildAuthServiceProvider()
        {
            var services = new ServiceCollection();

            var configFileProvider = new Mock<IConfigFileProvider>();
            configFileProvider.Setup(c => c.InstanceName).Returns("Gamarr");
            services.AddSingleton(configFileProvider.Object);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.GetTempPath()));

            services.AddAppAuthentication();

            return services.BuildServiceProvider();
        }

        [Test]
        public void cookie_options_should_have_redirect_event_handlers()
        {
            using var provider = BuildAuthServiceProvider();

            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            var options = optionsMonitor.Get(nameof(AuthenticationType.Forms));

            options.Events.OnRedirectToLogin.Should().NotBeNull();
            options.Events.OnRedirectToAccessDenied.Should().NotBeNull();
        }

        [Test]
        public void cookie_name_should_be_based_on_instance_name()
        {
            using var provider = BuildAuthServiceProvider();

            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            var options = optionsMonitor.Get(nameof(AuthenticationType.Forms));

            options.Cookie.Name.Should().Be("GamarrAuth");
        }

        [Test]
        public void data_protection_should_round_trip()
        {
            var services = new ServiceCollection();
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.GetTempPath()));

            using var provider = services.BuildServiceProvider();
            var protector = provider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test");

            var plaintext = "cookie-payload";
            var encrypted = protector.Protect(plaintext);
            var decrypted = protector.Unprotect(encrypted);

            decrypted.Should().Be(plaintext);
        }
    }
}
