using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class ExecutableFileSpecificationFixture : CoreTest<ExecutableFileSpecification>
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                Release = new ReleaseInfo(),
                Game = new Game()
            };
        }

        private void GivenTitle(string title)
        {
            _remoteGame.Release.Title = title;
        }

        [TestCase("Ted Lasso S04E06 1080p ATVP WEB-DL DDP5-1 Atmos.mkv.exe")]
        [TestCase("Kirby and the Forgotten Land [01004D300C5AE000][v0].nsp.exe")]
        [TestCase("Game.Title.2023.REPACK-FitGirl.exe")]
        [TestCase("Game.Title.2023.msi")]
        [TestCase("Game.Title.2023.scr")]
        [TestCase("Game.Title.2023.com")]
        [TestCase("Game.Title.2023.pif")]
        [TestCase("Game.Title.2023.bat")]
        [TestCase("Game.Title.2023.cmd")]
        [TestCase("Game.Title.2023.vbs")]
        [TestCase("Game.Title.2023.js")]
        [TestCase("Game.Title.2023.jar")]
        [TestCase("Game.Title.2023.ps1")]
        public void should_reject_executable_release(string title)
        {
            GivenTitle(title);

            var decision = Subject.IsSatisfiedBy(_remoteGame, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.ExecutableFile);
        }

        [TestCase("Game.Title.2023.EXE")]
        [TestCase("Game Title (2023) Atmos.mkv.ExE")]
        public void should_reject_executable_release_regardless_of_case(string title)
        {
            GivenTitle(title);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [TestCase("Game.Title.2023.REPACK-FitGirl")]
        [TestCase("Kirby and the Forgotten Land [01004D300C5AE000][v0].nsp")]
        [TestCase("Game Title (2023) [Nintendo Switch]")]
        [TestCase("Game.Title.2023-CODEX.iso")]
        [TestCase("Game.Title.2023.GOG.rar")]
        public void should_accept_normal_release(string title)
        {
            GivenTitle(title);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [TestCase("Game.Title.2023.REPACK-FitGirl www.example.com")]
        [TestCase("Game.Title.2023-DODI https://dodi-repacks.site.com")]
        public void should_accept_release_signed_with_a_tracker_address(string title)
        {
            GivenTitle(title);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void should_accept_when_title_is_missing(string title)
        {
            GivenTitle(title);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
