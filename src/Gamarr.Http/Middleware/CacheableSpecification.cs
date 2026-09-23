using System;
using Microsoft.AspNetCore.Http;
using NzbDrone.Common.EnvironmentInfo;

namespace Gamarr.Http.Middleware
{
    public interface ICacheableSpecification
    {
        bool IsCacheable(HttpRequest request);
    }

    public class CacheableSpecification : ICacheableSpecification
    {
        public bool IsCacheable(HttpRequest request)
        {
            if (!RuntimeInfo.IsProduction)
            {
                return false;
            }

            if (request.Query.ContainsKey("h"))
            {
                return true;
            }

            if (request.Path.StartsWithSegments("/api", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if (request.Path.StartsWithSegments("/signalr", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            // Cover requests are only cacheable via the `h` query key checked
            // above; without it the file may not be downloaded yet, so caching
            // here would pin a 404 in the browser for good.
            if (request.Path.StartsWithSegments("/MediaCover", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            var path = request.Path.Value ?? "";

            if (path.EndsWith("/index.js"))
            {
                return false;
            }

            if (path.EndsWith("/initialize.json"))
            {
                return false;
            }

            if (path.StartsWith("/feed", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if ((path.StartsWith("/logfile", StringComparison.CurrentCultureIgnoreCase) ||
                path.StartsWith("/updatelogfile", StringComparison.CurrentCultureIgnoreCase)) &&
                path.EndsWith(".txt", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
