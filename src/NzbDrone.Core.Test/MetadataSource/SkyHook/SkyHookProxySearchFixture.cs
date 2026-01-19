using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.SkyHook;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSource.SkyHook
{
    [TestFixture]
    [IntegrationTest]
#pragma warning disable CS0618 // SkyHookProxy is obsolete but kept for testing legacy functionality
    public class SkyHookProxySearchFixture : CoreTest<SkyHookProxy>
    {
        [SetUp]
        public void Setup()
        {
            UseRealHttp();
        }

        [TestCase("Prometheus", "Prometheus")]

        // TODO: IGDB Doesn't like when we clean periods from this
        // [TestCase("The Man from U.N.C.L.E.", "The Man from U.N.C.L.E.")]
        [TestCase("imdb:tt2527336", "Star Wars: The Last Jedi")]
        [TestCase("imdb:tt2798920", "Annihilation")]
        [TestCase("https://www.imdb.com/title/tt0033467/", "Citizen Kane")]
        [TestCase("https://www.thegamedb.org/game/775-le-voyage-dans-la-lune", "A Trip to the Moon")]
        public void successful_search(string title, string expected)
        {
            var result = Subject.SearchForNewGame(title);

            result.Should().NotBeEmpty();

            result[0].Title.Should().Be(expected);

            ExceptionVerification.IgnoreWarns();
        }

        [TestCase("igdbid:")]
        [TestCase("igdbid: 99999999999999999999")]
        [TestCase("igdbid: 0")]
        [TestCase("igdbid: -12")]
        [TestCase("igdbid:1")]
        [TestCase("adjalkwdjkalwdjklawjdlKAJD;EF")]
        [TestCase("imdb: tt9805708")]
        [TestCase("https://www.UNKNOWN-DOMAIN.com/title/tt0033467/")]
        [TestCase("https://www.thegamedb.org/MALFORMED/775-le-voyage-dans-la-lune")]
        public void no_search_result(string term)
        {
            var result = Subject.SearchForNewGame(term);
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }
    }
}
