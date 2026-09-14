using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using NzbDrone.Test.Common;
using Gamarr.Http.ModelBinding;

namespace NzbDrone.Api.Test.ModelBinding
{
    [TestFixture]
    public class RadarrCompatibilityValueProviderFactoryFixture : TestBase
    {
        private static IQueryCollection Query(params (string Key, string Value)[] pairs)
        {
            return new QueryCollection(pairs.ToDictionary(p => p.Key, p => new StringValues(p.Value)));
        }

        [TestCase("includeUnknownMovieItems", "includeUnknownGameItems")]
        [TestCase("includeMovie", "includeGame")]
        [TestCase("movieId", "gameId")]
        [TestCase("movieIds", "gameIds")]
        [TestCase("IncludeUnknownMovieItems", "includeUnknownGameItems")]
        public void should_alias_movie_named_parameter_to_game(string sent, string expected)
        {
            var aliases = RadarrCompatibilityValueProviderFactory.BuildAliases(Query((sent, "true")));

            aliases.Should().ContainKey(expected);
            aliases[expected].ToString().Should().Be("true");
        }

        [Test]
        public void should_not_alias_parameters_without_movie_in_the_name()
        {
            var aliases = RadarrCompatibilityValueProviderFactory.BuildAliases(Query(("includeGame", "true"), ("pageSize", "50")));

            aliases.Should().BeEmpty();
        }

        [Test]
        public void should_not_overwrite_a_game_named_parameter_that_was_sent_explicitly()
        {
            var aliases = RadarrCompatibilityValueProviderFactory.BuildAliases(Query(("includeUnknownGameItems", "false"), ("includeUnknownMovieItems", "true")));

            aliases.Should().BeEmpty();
        }

        [Test]
        public void should_keep_every_value_of_a_repeated_parameter()
        {
            var query = new QueryCollection(new Dictionary<string, StringValues> { { "movieIds", new StringValues(new[] { "1", "2" }) } });

            var aliases = RadarrCompatibilityValueProviderFactory.BuildAliases(query);

            aliases["gameIds"].Should().BeEquivalentTo(new[] { "1", "2" });
        }

        [Test]
        public void should_add_a_value_provider_that_binds_the_aliased_name()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Query = Query(("includeUnknownMovieItems", "true"));

            var context = new ValueProviderFactoryContext(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));

            new RadarrCompatibilityValueProviderFactory().CreateValueProviderAsync(context).GetAwaiter().GetResult();

            context.ValueProviders.Should().HaveCount(1);
            context.ValueProviders[0].GetValue("includeUnknownGameItems").FirstValue.Should().Be("true");
        }

        [Test]
        public void should_not_add_a_value_provider_when_nothing_needs_aliasing()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Query = Query(("pageSize", "50"));

            var context = new ValueProviderFactoryContext(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));

            new RadarrCompatibilityValueProviderFactory().CreateValueProviderAsync(context).GetAwaiter().GetResult();

            context.ValueProviders.Should().BeEmpty();
        }
    }
}
