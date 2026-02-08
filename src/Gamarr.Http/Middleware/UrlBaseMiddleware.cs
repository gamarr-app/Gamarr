using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NzbDrone.Common.Extensions;

namespace Gamarr.Http.Middleware
{
    public class UrlBaseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _urlBase;

        public UrlBaseMiddleware(RequestDelegate next, string urlBase)
        {
            _next = next;
            _urlBase = urlBase;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_urlBase.IsNotNullOrWhiteSpace() && context.Request.PathBase.Value.IsNullOrWhiteSpace())
            {
                // Build redirect from safe PathString components rather than raw string concatenation
                var basePath = new PathString(_urlBase);
                var redirectPath = basePath.Add(context.Request.Path);
                var redirectUrl = redirectPath.Value + context.Request.QueryString;

                // Validate the final URL is a safe local redirect
                if (redirectUrl == null ||
                    redirectUrl.StartsWith("//") ||
                    redirectUrl.Contains('\\') ||
                    !redirectUrl.StartsWith("/") ||
                    !Uri.TryCreate(redirectUrl, UriKind.Relative, out _))
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                context.Response.Redirect(redirectUrl);
                context.Response.StatusCode = 307;

                return;
            }

            await _next(context);
        }
    }
}
