using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetTools;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Http
{
    public class HttpProxySettingsProvider : IHttpProxySettingsProvider
    {
        private readonly IConfigService _configService;

        public HttpProxySettingsProvider(IConfigService configService)
        {
            _configService = configService;
        }

        public HttpProxySettings GetProxySettings(HttpUri uri)
        {
            var proxySettings = GetProxySettings();
            if (proxySettings == null)
            {
                return null;
            }

            if (ShouldProxyBeBypassed(proxySettings, uri))
            {
                return null;
            }

            return proxySettings;
        }

        public HttpProxySettings GetProxySettings()
        {
            if (!_configService.ProxyEnabled)
            {
                return null;
            }

            return new HttpProxySettings(_configService.ProxyType,
                                _configService.ProxyHostname,
                                _configService.ProxyPort,
                                _configService.ProxyBypassFilter,
                                _configService.ProxyBypassLocalAddresses,
                                _configService.ProxyUsername,
                                _configService.ProxyPassword);
        }

        public bool ShouldProxyBeBypassed(HttpProxySettings proxySettings, HttpUri url)
        {
            // We are utilizing the WebProxy implementation here to save us having to re-implement it. This way we use Microsofts implementation
            if (IsBypassedByIpAddressRange(proxySettings.BypassListAsArray, url.Host))
            {
                return true;
            }

            var proxy = new WebProxy(proxySettings.Host + ":" + proxySettings.Port, proxySettings.BypassLocalAddress, proxySettings.BypassListAsArray);

            try
            {
                return proxy.IsBypassed((Uri)url);
            }
            catch (SocketException)
            {
                // WebProxy's local-address check resolves this machine's own hostname for IP hosts;
                // when that lookup fails, treat only loopback as local instead of failing the request.
                var noLocalProxy = new WebProxy(proxySettings.Host + ":" + proxySettings.Port, false, proxySettings.BypassListAsArray);

                return noLocalProxy.IsBypassed((Uri)url) ||
                       (proxySettings.BypassLocalAddress && IPAddress.TryParse(url.Host, out var address) && IPAddress.IsLoopback(address));
            }
        }

        private static bool IsBypassedByIpAddressRange(string[] bypassList, string host)
        {
            return bypassList.Any(bypass =>
                IPAddressRange.TryParse(bypass, out var ipAddressRange) &&
                IPAddress.TryParse(host, out var ipAddress) &&
                ipAddressRange.Contains(ipAddress));
        }
    }
}
