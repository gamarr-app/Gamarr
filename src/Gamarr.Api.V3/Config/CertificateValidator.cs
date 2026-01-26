using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentValidation;
using FluentValidation.Validators;
using NLog;
using NzbDrone.Common.Instrumentation;

namespace Gamarr.Api.V3.Config
{
    public static class CertificateValidation
    {
        public static IRuleBuilderOptions<T, string> IsValidCertificate<T>(this IRuleBuilder<T, string> ruleBuilder)
            where T : HostConfigResource
        {
            return ruleBuilder.SetValidator(new CertificateValidator<T>());
        }
    }

    public class CertificateValidator<T> : PropertyValidator<T, string>
        where T : HostConfigResource
    {
        public override string Name => "CertificateValidator";

        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(CertificateValidator<>));

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            var resource = context.InstanceToValidate;

            try
            {
                new X509Certificate2(resource.SslCertPath, resource.SslCertPassword, X509KeyStorageFlags.DefaultKeySet);

                return true;
            }
            catch (CryptographicException ex)
            {
                Logger.Debug(ex, "Invalid SSL certificate file or password. {0}", ex.Message);

                context.MessageFormatter.AppendArgument("message", ex.Message);

                return false;
            }
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Invalid SSL certificate file or password. {message}";
    }
}
