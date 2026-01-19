using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class HashedReleaseFixture : CoreTest
    {
        public static object[] HashedReleaseParserCases =
        {
            new object[]
            {
                @"C:\Test\Some.Hashed.Release.2018.720p.WEB-DL.AAC2.0.H.264-Mercury\0e895c37245186812cb08aab1529cf8ee389dd05.mkv".AsOsAgnostic(),
                "Some Hashed Release",
                Quality.WEBDL720p,
                "Mercury"
            },
            new object[]
            {
                @"C:\Test\0e895c37245186812cb08aab1529cf8ee389dd05\Some.Hashed.Release.2018.720p.WEB-DL.AAC2.0.H.264-Mercury.mkv".AsOsAgnostic(),
                "Some Hashed Release",
                Quality.WEBDL720p,
                "Mercury"
            },
            new object[]
            {
                @"C:\Test\Game.2018.DVDRip.XviD-GAMARR\AHFMZXGHEWD660.mkv".AsOsAgnostic(),
                "Game",
                Quality.DVD,
                "GAMARR"
            },
            new object[]
            {
                @"C:\Test\Game.2018.1080p.BluRay.x264-GAMARR\Backup_72023S02-12.mkv".AsOsAgnostic(),
                "Game",
                Quality.Bluray1080p,
                "GAMARR"
            },
            new object[]
            {
                @"C:\Test\Game.2018.1080p.BluRay.x264\Backup_72023S02-12.mkv".AsOsAgnostic(),
                "Game",
                Quality.Bluray1080p,
                null
            },
            new object[]
            {
                @"C:\Test\Game 2018 720p WEB-DL DD5 1 H 264-ECI\123.mkv".AsOsAgnostic(),
                "Game",
                Quality.WEBDL720p,
                "ECI"
            },
            new object[]
            {
                @"C:\Test\Game 2018 720p WEB-DL DD5 1 H 264-ECI\abc.mkv".AsOsAgnostic(),
                "Game",
                Quality.WEBDL720p,
                "ECI"
            },
            new object[]
            {
                @"C:\Test\Game 2018 720p WEB-DL DD5 1 H 264-ECI\b00bs.mkv".AsOsAgnostic(),
                "Game",
                Quality.WEBDL720p,
                "ECI"
            },
            new object[]
            {
                @"C:\Test\Game.Title.2018.720p.HDTV.x264-NZBgeek/cgajsofuejsa501.mkv".AsOsAgnostic(),
                "Game Title",
                Quality.HDTV720p,
                "NZBgeek"
            },
            new object[]
            {
                @"C:\Test\Game.2018.1080p.WEB-DL.DD5.1.H264-RARBG\170424_26.mkv".AsOsAgnostic(),
                "Game",
                Quality.WEBDL1080p,
                "RARBG"
            },
            new object[]
            {
                @"C:\Test\Game.Title.2018.720p.HDTV.H.264\abc.xyz.af6021c37f7852.mkv".AsOsAgnostic(),
                "Game Title",
                Quality.HDTV720p,
                null
            }
        };

        [Test]
        [TestCaseSource(nameof(HashedReleaseParserCases))]
        public void should_properly_parse_hashed_releases(string path, string title, Quality quality, string releaseGroup)
        {
            var result = Parser.Parser.ParseGamePath(path);
            result.PrimaryGameTitle.Should().Be(title);
            result.Quality.Quality.Should().Be(quality);
            result.ReleaseGroup.Should().Be(releaseGroup);
        }
    }
}
