using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    // #203: an UpdateOnly release was compared against the BASE game file,
    // which parses as "Unknown" and therefore ranks below everything, so every
    // update release — the installed one included — looked like an upgrade.
    [TestFixture]
    [NonParallelizable]
    public class UpgradeDiskSpecificationUpdateVersionFixture : CoreTest<UpgradeDiskSpecification>
    {
        private UpgradeDiskSpecification _upgradeDisk;
        private RemoteGame _remoteGame;
        private GameFile _baseFile;
        private List<GameComponent> _components;
        private List<GameFile> _files;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();
            _upgradeDisk = Mocker.Resolve<UpgradeDiskSpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            // The reporter's base game: imported with no detectable quality.
            _baseFile = new GameFile
            {
                Id = 1,
                RelativePath = "Game.nsp",
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 2)),
                GameVersion = new GameVersion()
            };

            var fakeGame = Builder<Game>.CreateNew()
                .With(g => g.Id = 7)
                .With(g => g.MonitorUpdates = true)
                .With(g => g.GameFileId = 1)
                .With(g => g.QualityProfile = new QualityProfile
                {
                    UpgradeAllowed = true,
                    Cutoff = Quality.GOG.Id,
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("None"),
                    MinFormatScore = 0,
                })
                .With(g => g.GameFile = _baseFile)
                .Build();

            _remoteGame = new RemoteGame
            {
                Game = fakeGame,
                Release = new ReleaseInfo { Title = "Game Update v1.7.0 NSW-GROUP" },
                ParsedGameInfo = new ParsedGameInfo
                {
                    ContentType = ReleaseContentType.UpdateOnly,
                    Quality = new QualityModel(Quality.UpdateOnly, new Revision(version: 1)),
                    GameVersion = new GameVersion(1, 7, 0)
                },
                CustomFormats = new List<CustomFormat>()
            };

            _components = new List<GameComponent>();
            _files = new List<GameFile> { _baseFile };

            Mocker.GetMock<IGameComponentService>()
                .Setup(s => s.GetByGame(It.IsAny<int>()))
                .Returns(() => _components);

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                .Returns(() => _files);

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(x => x.ParseCustomFormat(It.IsAny<GameFile>()))
                .Returns(new List<CustomFormat>());

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.UpgradeGameVersions)
                .Returns(true);
        }

        private void GivenInstalledUpdate(string key, GameVersion fileVersion)
        {
            var component = new GameComponent
            {
                Id = 42,
                GameId = _remoteGame.Game.Id,
                ComponentType = GameComponentType.Update,
                Key = key,
                Title = key,
                Monitored = true
            };

            _components.Add(component);

            _files.Add(new GameFile
            {
                Id = 2,
                ComponentId = component.Id,
                RelativePath = $"Updates/{key}/update.nsp",
                Quality = new QualityModel(Quality.UpdateOnly, new Revision(version: 1)),
                GameVersion = fileVersion
            });
        }

        [Test]
        public void should_reject_update_release_with_same_version_as_installed_update()
        {
            // The reporter's "other upload of the same version" case.
            GivenInstalledUpdate("v4.0.1", new GameVersion(4, 0, 1));

            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(4, 0, 1);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_older_update_release_when_newer_update_is_installed()
        {
            // v1.7 offered while v4.0.1 is on disk — the exact trace in #203.
            GivenInstalledUpdate("v4.0.1", new GameVersion(4, 0, 1));

            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(1, 7, 0);

            var decision = _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation());

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.DiskHigherVersion);
        }

        [Test]
        public void should_accept_newer_update_release_than_installed_update()
        {
            GivenInstalledUpdate("v4.0.1", new GameVersion(4, 0, 1));

            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(4, 1, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_update_release_when_no_update_is_installed()
        {
            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_unversioned_update_release_when_an_update_is_installed()
        {
            GivenInstalledUpdate("v4.0.1", new GameVersion(4, 0, 1));

            _remoteGame.ParsedGameInfo.GameVersion = null;

            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_compare_update_release_against_base_file_quality()
        {
            // Update Only ranks above the base file's Unknown, which is what
            // used to approve every update. The installed update version must
            // decide instead.
            GivenInstalledUpdate("v4.0.1", new GameVersion(4, 0, 1));

            _baseFile.Quality = new QualityModel(Quality.Unknown, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.UpdateOnly, new Revision(version: 1));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(4, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_use_update_component_key_when_linked_file_has_no_version()
        {
            // Older databases have the version in the component key only.
            GivenInstalledUpdate("v4.0.1", new GameVersion());

            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(1, 7, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_use_highest_installed_update_when_several_are_present()
        {
            GivenInstalledUpdate("v1.7.0", new GameVersion(1, 7, 0));

            _components.Add(new GameComponent
            {
                Id = 43,
                GameId = _remoteGame.Game.Id,
                ComponentType = GameComponentType.Update,
                Key = "v4.0.1",
                Title = "v4.0.1",
                Monitored = true
            });

            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion(2, 0, 0);

            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_ignore_update_components_when_release_is_not_an_update()
        {
            // A base-game release still goes through the normal quality path.
            GivenInstalledUpdate("v4.0.1", new GameVersion(4, 0, 1));

            _remoteGame.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
            _remoteGame.ParsedGameInfo.GameVersion = new GameVersion();

            _upgradeDisk.IsSatisfiedBy(_remoteGame, new ReleaseDecisionInformation()).Accepted.Should().BeTrue();
        }
    }
}
