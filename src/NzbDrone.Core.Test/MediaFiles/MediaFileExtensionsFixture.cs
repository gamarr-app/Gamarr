using System.IO;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class MediaFileExtensionsFixture : CoreTest
    {
        [TestCase(@"data.bin.001")]
        [TestCase(@"data.bin.002")]
        [TestCase(@"game.7z.999")]
        [TestCase(@"game.r00")]
        [TestCase(@"game.r57")]
        [TestCase(@"game.z01")]
        [TestCase(@"Game.Title.part01.rar")]
        [TestCase(@"setup.exe")]
        [TestCase(@"game.iso")]
        [TestCase(@"data1.bin")]
        public void should_recognize_game_file(string fileName)
        {
            MediaFileExtensions.IsGameFileExtension(Path.GetExtension(fileName)).Should().BeTrue();
        }

        [TestCase(@"readme.txt")]
        [TestCase(@"cover.jpg")]
        [TestCase(@"release.nfo")]
        [TestCase(@"game.z1")]
        [TestCase(@"game.0001")]
        [TestCase(@"game")]
        [TestCase(@"")]
        public void should_not_recognize_non_game_file(string fileName)
        {
            MediaFileExtensions.IsGameFileExtension(Path.GetExtension(fileName)).Should().BeFalse();
        }

        [Test]
        public void split_volume_extensions_should_not_leak_into_parser_facing_extension_set()
        {
            // FileExtensions.RemoveFileExtension strips trailing Extensions
            // members from release titles; numeric split-volume suffixes there
            // would eat version components like "v1.001".
            MediaFileExtensions.Extensions.Should().NotContain(".001");
            MediaFileExtensions.Extensions.Should().NotContain(".r00");
        }
    }
}
