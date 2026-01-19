using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class UrlFixture : CoreTest
    {
        [TestCase("[www.test.com] - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("test.net - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("[www.test-hyphen.com] - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("www.test123.org - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("[test.co.uk] - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("www.test-hyphen.net.au - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("[www.test123.co.nz] - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("test-hyphen123.org.au - Game.Title.2023.720p.HDTV.X264-DIMENSION", "Game Title")]
        [TestCase("[www.test123.de] - Mad Game Title 2023 [Bluray720p]", "Mad Game Title")]
        [TestCase("www.test-hyphen.de - Mad Game Title 2023 [Bluray1080p]", "Mad Game Title")]
        [TestCase("www.test123.co.za - The Game Title Bros. (2023)", "The Game Title Bros.")]
        [TestCase("[www.test-hyphen.ca] - Game Title (2023)", "Game Title")]
        [TestCase("test123.ca - Game Time 2023 720p HDTV x264 CRON", "Game Time")]
        [TestCase("[www.test-hyphen123.co.za] - Game Title 2023", "Game Title")]
        [TestCase("(gameawake.com) Game Title 2023 [720p] [English Subbed]", "Game Title")]
        public void should_not_parse_url_in_name(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle).GameTitle.CleanGameTitle();
            result.Should().Be(title.CleanGameTitle());
        }

        [TestCase("Game.2023.English.HDTV.XviD-LOL[www.abb.com]", "LOL")]
        [TestCase("Game Title 2023 English HDTV XviD LOL[www.academy.org]", null)]
        [TestCase("Game Title Now 2023 DVDRip XviD RUNNER[www.aetna.net]", null)]
        [TestCase("Game.Title.2023.DVDRip.XviD-RUNNER[www.alfaromeo.io]", "RUNNER")]
        [TestCase("Game.Title.2023.English.HDTV.XviD-LOL[www.abbott.gov]", "LOL")]
        [TestCase("Game Title 2023 English HDTV XviD LOL[www.actor.org]", null)]
        [TestCase("Game Title Future 2023 DVDRip XviD RUNNER[www.allstate.net]", null)]
        public void should_not_parse_url_in_group(string title, string expected)
        {
            Parser.ReleaseGroupParser.ParseReleaseGroup(title).Should().Be(expected);
        }
    }
}
