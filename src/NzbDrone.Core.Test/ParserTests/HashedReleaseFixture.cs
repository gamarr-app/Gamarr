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
                @"C:\Test\Game.Title.2018-CODEX\0e895c37245186812cb08aab1529cf8ee389dd05.exe".AsOsAgnostic(),
                "Game Title",
                Quality.Scene,
                "CODEX"
            },
            new object[]
            {
                @"C:\Test\0e895c37245186812cb08aab1529cf8ee389dd05\Game.Title.2018-PLAZA.exe".AsOsAgnostic(),
                "Game Title 2018",
                Quality.Scene,
                "PLAZA"
            },
            new object[]
            {
                @"C:\Test\Game.Title.2018.GOG\AHFMZXGHEWD660.exe".AsOsAgnostic(),
                "Game Title",
                Quality.GOG,
                null
            },
            new object[]
            {
                @"C:\Test\Game.Title.2018-FitGirl.Repack\Backup_72023S02-12.bin".AsOsAgnostic(),
                "Game Title",
                Quality.Repack,
                "FitGirl"
            },
            new object[]
            {
                @"C:\Test\Game.Title.2018-SKIDROW\123.exe".AsOsAgnostic(),
                "Game Title",
                Quality.Scene,
                "SKIDROW"
            },
            new object[]
            {
                @"C:\Test\Game.Title.2018-DODI\abc.exe".AsOsAgnostic(),
                "Game Title",
                Quality.Repack,
                "DODI"
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
