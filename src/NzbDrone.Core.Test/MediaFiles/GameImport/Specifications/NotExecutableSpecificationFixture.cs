using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class NotExecutableSpecificationFixture : CoreTest<NotExecutableSpecification>
    {
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _localGame = new LocalGame
            {
                Path = @"C:\Test\Unsorted\Game.Title.2023\Game.Title.2023.iso".AsOsAgnostic(),
                Size = 100,
                Game = Builder<Game>.CreateNew().Build()
            };
        }

        private void GivenFile(string fileName)
        {
            _localGame.Path = (@"C:\Test\Unsorted\Game.Title.2023\" + fileName).AsOsAgnostic();
        }

        [TestCase("Ted Lasso S04E06 1080p ATVP WEB-DL DDP5-1 Atmos.mkv.exe")]
        [TestCase("Game.Title.2023.iso.exe")]
        [TestCase("Kirby and the Forgotten Land [v0].nsp.exe")]
        [TestCase("Game.Title.2023.mp4.msi")]
        [TestCase("Readme.txt.scr")]
        public void should_reject_executable_hiding_behind_a_content_extension(string fileName)
        {
            GivenFile(fileName);

            var decision = Subject.IsSatisfiedBy(_localGame, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(ImportRejectionReason.ExecutableFile);
        }

        [TestCase("autorun.com")]
        [TestCase("installer.pif")]
        [TestCase("payload.vbs")]
        [TestCase("payload.js")]
        [TestCase("payload.jar")]
        [TestCase("payload.ps1")]
        [TestCase("payload.bat")]
        [TestCase("payload.cmd")]
        [TestCase("screensaver.scr")]
        public void should_reject_executable_that_is_never_a_game_file(string fileName)
        {
            GivenFile(fileName);

            var decision = Subject.IsSatisfiedBy(_localGame, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(ImportRejectionReason.ExecutableFile);
        }

        // Repack, GOG and store installers legitimately ship these, and
        // MediaFileExtensions maps them to Quality.Scene.
        [TestCase("setup.exe")]
        [TestCase("Game.Title.2023.exe")]
        [TestCase("Game.Title.2023.msi")]
        public void should_accept_a_plain_installer(string fileName)
        {
            GivenFile(fileName);

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [TestCase("Game.Title.2023.iso")]
        [TestCase("Kirby and the Forgotten Land [v0].nsp")]
        [TestCase("Game.Title.2023.bin")]
        public void should_accept_a_game_file(string fileName)
        {
            GivenFile(fileName);

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_path_is_missing()
        {
            _localGame.Path = null;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }
    }
}
