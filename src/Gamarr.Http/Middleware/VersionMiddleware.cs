using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NzbDrone.Common.EnvironmentInfo;
using Gamarr.Http.Extensions;

namespace Gamarr.Http.Middleware
{
    public class VersionMiddleware
    {
        private const string VERSIONHEADER = "X-Application-Version";
        private const string COMPATIBLEVERSION = "5.0.0.0";

        private readonly RequestDelegate _next;
        private readonly string _version;

        public VersionMiddleware(RequestDelegate next)
        {
            _next = next;
            _version = BuildInfo.Version.ToString();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.IsApiRequest() && !context.Response.Headers.ContainsKey(VERSIONHEADER))
            {
                context.Response.Headers[VERSIONHEADER] = context.Request.IsProwlarrIndexerTestRequest() ? COMPATIBLEVERSION : _version;
            }

            await _next(context);
        }
    }
}
