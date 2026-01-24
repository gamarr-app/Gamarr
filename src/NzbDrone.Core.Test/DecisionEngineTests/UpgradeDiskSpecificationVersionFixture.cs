using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class UpgradeDiskSpecificationVersionFixture : CoreTest<UpgradeDiskSpecification>
    {
        private UpgradeDiskSpecification _upgradeDisk;
        private RemoteGame _remoteGame;
        private GameFile _existingFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();
            _upgradeDisk = Mocker.Resolve<UpgradeDiskSpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            _existingFile = new GameFile
            {
                Quality = new QualityModel(Quality.GOG, new Revision(version: 2)),
                GameVersion = new GameVersion(1, 0, 0)
            };

            var fakeGame = Builder<Game>.CreateNew()
                .With(g => g.QualityProfile = new QualityProfile
                {
                    UpgradeAllowed = true,
                    Cutoff = Quality.GOG.Id,
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("None"),
                    MinFormatScore = 0,
                })
                .With(g => g.GameFile = _existingFile)
                .Build();

            _remoteGame = new RemoteGame
            {
                Game = fakeGame,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.GOG, new Revision(version: 2)),
                    GameVersion = new GameVersion(2, 0, 0)
                },
                CustomFormats = new List<CustomFormat>()
            };

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(x => x.ParseCustomFormat(It.IsAny<GameFile>()))
                .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_accept_version_upgrade_when_cutoff_met()
        {
            // Existing file meets cutoff (GOG >= GOG), but new release has a newer game version
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(1, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(2, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_cutoff_met_and_no_version_upgrade()
        {
            // Existing file meets cutoff (GOG >= GOG), and new release has the same game version
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(2, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(2, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_version_upgrade_when_quality_is_lower()
        {
            // New quality is lower (Scene < GOG), but new release has a newer game version
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(1, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Scene, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(2, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_no_existing_file()
        {
            // No existing game file, should always accept
            _remoteGame.Game.GameFile = null;

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_quality_lower_and_same_version()
        {
            // New quality is lower (Scene < GOG), and same game version
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(1, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Scene, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(1, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_version_upgrade_when_existing_has_no_version()
        {
            // Existing file has no version, new release has a version
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion();

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(1, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_cutoff_met_and_new_version_is_null()
        {
            // Cutoff met, and new release has no version info
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(1, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = null;

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_version_upgrade_when_revision_is_lower()
        {
            // New revision is lower (v1 < v2), but new release has a newer game version
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(1, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 1));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(2, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_minor_version_upgrade_when_cutoff_met()
        {
            // Cutoff met, minor version bump (1.0 -> 1.1)
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(1, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(1, 1, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_cutoff_met_and_version_is_older()
        {
            // Cutoff met, but new version is older than existing
            _existingFile.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _existingFile.GameVersion = new GameVersion(2, 0, 0);

            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(1, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }
    }
}
