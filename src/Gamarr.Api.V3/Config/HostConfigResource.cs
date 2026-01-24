using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Security;
using NzbDrone.Core.Update;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Config
{
    public class HostConfigResource : RestResource
    {
        public string BindAddress { get; set; }
        public int Port { get; set; }
        public int SslPort { get; set; }
        public bool EnableSsl { get; set; }
        public bool LaunchBrowser { get; set; }
        public AuthenticationType AuthenticationMethod { get; set; }
        public AuthenticationRequiredType AuthenticationRequired { get; set; }
        public bool AnalyticsEnabled { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PasswordConfirmation { get; set; }
        public string LogLevel { get; set; }
        public int LogSizeLimit { get; set; }
        public string ConsoleLogLevel { get; set; }
        public string Branch { get; set; }
        public string ApiKey { get; set; }
        public string SslCertPath { get; set; }
        public string SslCertPassword { get; set; }
        public string UrlBase { get; set; }
        public string InstanceName { get; set; }
        public string ApplicationUrl { get; set; }
        public bool UpdateAutomatically { get; set; }
        public UpdateMechanism UpdateMechanism { get; set; }
        public string UpdateScriptPath { get; set; }
        public bool ProxyEnabled { get; set; }
        public ProxyType ProxyType { get; set; }
        public string ProxyHostname { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public string ProxyBypassFilter { get; set; }
        public bool ProxyBypassLocalAddresses { get; set; }
        public CertificateValidationType CertificateValidation { get; set; }
        public string BackupFolder { get; set; }
        public int BackupInterval { get; set; }
        public int BackupRetention { get; set; }
        public bool TrustCgnatIpAddresses { get; set; }
    }

    public static class HostConfigResourceMapper
    {
        public static HostConfigResource ToResource(this IConfigFileProvider configFile, IConfigService configService)
        {
            var resource = new HostConfigResource();

            MapFromConfigFile(resource, configFile);
            MapFromConfigService(resource, configService);

            return resource;
        }

        private static void MapFromConfigFile(HostConfigResource resource, IConfigFileProvider configFile)
        {
            resource.BindAddress = configFile.BindAddress;
            resource.Port = configFile.Port;
            resource.SslPort = configFile.SslPort;
            resource.EnableSsl = configFile.EnableSsl;
            resource.LaunchBrowser = configFile.LaunchBrowser;
            resource.AuthenticationMethod = configFile.AuthenticationMethod;
            resource.AuthenticationRequired = configFile.AuthenticationRequired;
            resource.AnalyticsEnabled = configFile.AnalyticsEnabled;
            resource.LogLevel = configFile.LogLevel;
            resource.LogSizeLimit = configFile.LogSizeLimit;
            resource.ConsoleLogLevel = configFile.ConsoleLogLevel;
            resource.Branch = configFile.Branch;
            resource.ApiKey = configFile.ApiKey;
            resource.SslCertPath = configFile.SslCertPath;
            resource.SslCertPassword = configFile.SslCertPassword;
            resource.UrlBase = configFile.UrlBase;
            resource.InstanceName = configFile.InstanceName;
            resource.UpdateAutomatically = configFile.UpdateAutomatically;
            resource.UpdateMechanism = configFile.UpdateMechanism;
            resource.UpdateScriptPath = configFile.UpdateScriptPath;
        }

        private static void MapFromConfigService(HostConfigResource resource, IConfigService configService)
        {
            resource.ProxyEnabled = configService.ProxyEnabled;
            resource.ProxyType = configService.ProxyType;
            resource.ProxyHostname = configService.ProxyHostname;
            resource.ProxyPort = configService.ProxyPort;
            resource.ProxyUsername = configService.ProxyUsername;
            resource.ProxyPassword = configService.ProxyPassword;
            resource.ProxyBypassFilter = configService.ProxyBypassFilter;
            resource.ProxyBypassLocalAddresses = configService.ProxyBypassLocalAddresses;
            resource.CertificateValidation = configService.CertificateValidation;
            resource.BackupFolder = configService.BackupFolder;
            resource.BackupInterval = configService.BackupInterval;
            resource.BackupRetention = configService.BackupRetention;
            resource.ApplicationUrl = configService.ApplicationUrl;
        }
    }
}
