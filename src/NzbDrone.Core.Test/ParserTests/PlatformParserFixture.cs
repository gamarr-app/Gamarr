using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class PlatformParserFixture : CoreTest
    {
        [TestCase("Portal 2 (2011) [Ps3][EUR FREE][MULTi5]", PlatformFamily.PlayStation, "PS3")]
        [TestCase("Game Title 2023 PS3 EUR ISO", PlatformFamily.PlayStation, "PS3")]
        [TestCase("Game.Title.2023.PlayStation3.EUR.ISO", PlatformFamily.PlayStation, "PS3")]
        [TestCase("Game Title (2023) [PS4] [USA]", PlatformFamily.PlayStation, "PS4")]
        [TestCase("Game.Title.2023.PS5.EUR.PKG", PlatformFamily.PlayStation, "PS5")]
        [TestCase("Game Title 2023 PSVita USA VPK", PlatformFamily.PlayStation, "PS Vita")]
        [TestCase("Game.Title.2023.PSP.EUR.ISO", PlatformFamily.PlayStation, "PSP")]
        public void should_parse_playstation_platform(string postTitle, PlatformFamily expectedFamily, string expectedString)
        {
            var result = PlatformParser.ParsePlatform(postTitle);
            var resultString = PlatformParser.ParsePlatformString(postTitle);

            result.Should().Be(expectedFamily);
            resultString.Should().Be(expectedString);
        }

        [TestCase("Game Title 2023 Xbox Series X", PlatformFamily.Xbox, "Xbox Series X")]
        [TestCase("Game.Title.2023.XSX.USA", PlatformFamily.Xbox, "Xbox Series X")]
        [TestCase("Game Title (2023) [Xbox One]", PlatformFamily.Xbox, "Xbox One")]
        [TestCase("Game.Title.2023.XBONE.USA", PlatformFamily.Xbox, "Xbox One")]
        [TestCase("Game Title 2023 Xbox 360 JTAG RGH", PlatformFamily.Xbox, "Xbox 360")]
        [TestCase("Game.Title.2023.X360.USA.ISO", PlatformFamily.Xbox, "Xbox 360")]
        public void should_parse_xbox_platform(string postTitle, PlatformFamily expectedFamily, string expectedString)
        {
            var result = PlatformParser.ParsePlatform(postTitle);
            var resultString = PlatformParser.ParsePlatformString(postTitle);

            result.Should().Be(expectedFamily);
            resultString.Should().Be(expectedString);
        }

        [TestCase("Game Title 2023 Switch NSP", PlatformFamily.Nintendo, "Switch")]
        [TestCase("Game.Title.2023.NSW.USA.XCI", PlatformFamily.Nintendo, "Switch")]
        [TestCase("Game Title (2023) [Nintendo Switch]", PlatformFamily.Nintendo, "Switch")]
        [TestCase("Game.Title.2023.WiiU.USA.WUX", PlatformFamily.Nintendo, "Wii U")]
        [TestCase("Game Title 2023 Wii ISO PAL", PlatformFamily.Nintendo, "Wii")]
        [TestCase("Game.Title.2023.3DS.USA.CIA", PlatformFamily.Nintendo, "3DS")]
        [TestCase("Game Title 2023 NDS USA ROM", PlatformFamily.Nintendo, "NDS")]
        public void should_parse_nintendo_platform(string postTitle, PlatformFamily expectedFamily, string expectedString)
        {
            var result = PlatformParser.ParsePlatform(postTitle);
            var resultString = PlatformParser.ParsePlatformString(postTitle);

            result.Should().Be(expectedFamily);
            resultString.Should().Be(expectedString);
        }

        [TestCase("Portal 2 2011 MAC", PlatformFamily.Mac, "Mac")]
        [TestCase("Game.Title.2023.macOS.DMG", PlatformFamily.Mac, "Mac")]
        [TestCase("Game Title (2023) [MAC]", PlatformFamily.Mac, "Mac")]
        [TestCase("Game.Title.2023.OSX.DMG", PlatformFamily.Mac, "Mac")]
        public void should_parse_mac_platform(string postTitle, PlatformFamily expectedFamily, string expectedString)
        {
            var result = PlatformParser.ParsePlatform(postTitle);
            var resultString = PlatformParser.ParsePlatformString(postTitle);

            result.Should().Be(expectedFamily);
            resultString.Should().Be(expectedString);
        }

        [TestCase("Game.Title.2023.Linux.tar.gz", PlatformFamily.Linux, "Linux")]
        [TestCase("Game Title 2023 [Linux]", PlatformFamily.Linux, "Linux")]
        public void should_parse_linux_platform(string postTitle, PlatformFamily expectedFamily, string expectedString)
        {
            var result = PlatformParser.ParsePlatform(postTitle);
            var resultString = PlatformParser.ParsePlatformString(postTitle);

            result.Should().Be(expectedFamily);
            resultString.Should().Be(expectedString);
        }

        [TestCase("Game.Title.2023.CODEX")]
        [TestCase("Game Title 2023 GOG")]
        [TestCase("Game.Title.2023.FitGirl.Repack")]
        [TestCase("Game Title (2023) MULTI5")]
        public void should_return_unknown_for_pc_releases(string postTitle)
        {
            var result = PlatformParser.ParsePlatform(postTitle);
            var resultString = PlatformParser.ParsePlatformString(postTitle);

            result.Should().Be(PlatformFamily.Unknown);
            resultString.Should().BeNull();
        }

        [TestCase("Portal 2 (2011) [Ps3][EUR FREE][MULTi5]")]
        public void should_parse_platform_from_full_parser(string postTitle)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle, true);

            result.Should().NotBeNull();
            result.Platform.Should().Be(PlatformFamily.PlayStation);
            result.PlatformString.Should().Be("PS3");
        }
    }
}
