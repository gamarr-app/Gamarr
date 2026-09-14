using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Gamarr.Http.ModelBinding
{
    /// <summary>
    /// Radarr-compatible clients (nzb360, LunaSea, ...) send movie-named query parameters to our
    /// v3 API. Nothing binds them, so the request succeeds with a 200 and quietly uses our
    /// defaults instead: <c>?includeUnknownMovieItems=true</c> returns the *filtered* queue, so a
    /// download whose game didn't resolve can never be seen from those apps. Silently answering a
    /// different question than the one asked is worse than a 400.
    ///
    /// This exposes a <c>movie</c> -> <c>game</c> aliased copy of the query string as an extra
    /// value provider. It is appended after the built-in ones, so a real game-named parameter
    /// always wins when both are present.
    /// </summary>
    public class RadarrCompatibilityValueProviderFactory : IValueProviderFactory
    {
        private static readonly Regex MovieToken = new Regex("movie", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var aliases = BuildAliases(context.ActionContext.HttpContext.Request.Query);

            if (aliases.Count > 0)
            {
                context.ValueProviders.Add(new QueryStringValueProvider(BindingSource.Query, new QueryCollection(aliases), CultureInfo.InvariantCulture));
            }

            return Task.CompletedTask;
        }

        public static Dictionary<string, StringValues> BuildAliases(IQueryCollection query)
        {
            var aliases = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            // Not query.ContainsKey: the comparer of an IQueryCollection is the caller's business,
            // and binding is case-insensitive, so the collision check has to be too.
            var sentKeys = new HashSet<string>(query.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var pair in query)
            {
                if (!pair.Key.Contains("movie", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var alias = MovieToken.Replace(pair.Key, "game");

                if (!sentKeys.Contains(alias))
                {
                    aliases[alias] = pair.Value;
                }
            }

            return aliases;
        }
    }
}
