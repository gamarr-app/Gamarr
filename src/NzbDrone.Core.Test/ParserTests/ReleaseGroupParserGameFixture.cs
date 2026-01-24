using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ReleaseGroupParserGameFixture
    {
        [TestCase("[DODI Repack]", "DODI")]
        [TestCase("[FitGirl Repack]", "FitGirl")]
        [TestCase("[XATAB]", "XATAB")]
        [TestCase("[ElAmigos]", "ElAmigos")]
        [TestCase("[CorePack]", "CorePack")]
        [TestCase("[KaOs]", "KaOs")]
        [TestCase("[R.G. Mechanics]", "R.G. Mechanics")]
        [TestCase("[R.G. Catalyst]", "R.G. Catalyst")]
        [TestCase("[Chovka]", "Chovka")]
        [TestCase("[EMPRESS]", "EMPRESS")]
        [TestCase("[GOG]", "GOG")]
        [TestCase("[Masquerade]", "Masquerade")]
        public void should_parse_game_repack_release_group(string group, string expected)
        {
            ReleaseGroupParser.ParseReleaseGroup($"Some.Game.v1.0 {group}").Should().Be(expected);
        }

        [Test]
        public void should_parse_standard_dash_separated_release_group()
        {
            ReleaseGroupParser.ParseReleaseGroup("Some.Game-CODEX").Should().Be("CODEX");
        }
    }
}
