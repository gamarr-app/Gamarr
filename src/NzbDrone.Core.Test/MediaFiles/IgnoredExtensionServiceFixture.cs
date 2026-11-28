using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class IgnoredExtensionServiceFixture : CoreTest<IgnoredExtensionService>
    {
        private void GivenSetting(string value)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.UserIgnoredExtensions)
                  .Returns(value);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(" , ,; ")]
        [TestCase(".")]
        public void should_parse_blank_setting_to_nothing(string setting)
        {
            FileExtensions.ParseIgnoredExtensions(setting).Should().BeEmpty();
        }

        [TestCase(".zip,.rar", ".zip", ".rar")]
        [TestCase("zip, rar", ".zip", ".rar")]
        [TestCase("  .ZIP ;  Rar\n.nfo ", ".zip", ".rar", ".nfo")]
        [TestCase("..nfo,,nfo", ".nfo")]
        public void should_normalise_the_setting(string setting, params string[] expected)
        {
            FileExtensions.ParseIgnoredExtensions(setting).Should().BeEquivalentTo(expected);
        }

        [TestCase("Game/readme.nfo")]
        [TestCase("Game/README.NFO")]
        [TestCase("Game.v1.0/Game.zip")]
        public void should_ignore_listed_extensions_regardless_of_case_or_dots(string path)
        {
            GivenSetting("nfo, .ZIP");

            Subject.IsIgnored(path).Should().BeTrue();
        }

        [TestCase("Game/game.iso")]
        [TestCase("Game/nfo")]
        [TestCase("Game/readme.nfo.iso")]
        [TestCase("Game/noextension")]
        [TestCase("")]
        [TestCase(null)]
        public void should_not_ignore_other_paths(string path)
        {
            GivenSetting("nfo, .zip");

            Subject.IsIgnored(path).Should().BeFalse();
        }

        [Test]
        public void should_ignore_nothing_by_default()
        {
            GivenSetting(string.Empty);

            Subject.IsIgnored("Game/game.zip").Should().BeFalse();
        }
    }
}
