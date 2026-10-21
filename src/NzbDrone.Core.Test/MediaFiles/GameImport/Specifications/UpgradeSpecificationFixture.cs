using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class UpgradeSpecificationFixture : CoreTest<UpgradeSpecification>
    {
        private Game _game;
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .With(e => e.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                                     .Build();

            _localGame = new LocalGame()
            {
                Path = @"C:\Test\30 Rock\30.rock.s01e01.avi",
                Quality = new QualityModel(Quality.Uplay, new Revision(version: 1)),
                Game = _game
            };
        }

        [Test]
        public void should_return_true_if_no_existing_episodeFile()
        {
            _localGame.Game.GameFile = null;
            _localGame.Game.GameFileId = 0;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_upgrade_for_existing_episodeFile()
        {
            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile =
                    new GameFile
                    {
                        Quality = new QualityModel(Quality.Scene, new Revision(version: 1))
                    };

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_not_an_upgrade_for_existing_episodeFile()
        {
            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile =
                new GameFile
                {
                    Quality = new QualityModel(Quality.Repack, new Revision(version: 1))
                };

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_not_a_revision_upgrade_and_prefers_propers()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile =
                new GameFile
                {
                    Quality = new QualityModel(Quality.Uplay, new Revision(version: 2))
                };

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_not_a_revision_upgrade_and_does_not_prefer_propers()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile =
                new GameFile
                {
                    Quality = new QualityModel(Quality.Uplay, new Revision(version: 2))
                };

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_comparing_to_a_lower_quality_proper()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            _localGame.Quality = new QualityModel(Quality.GOG);

            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile =
                new GameFile
                {
                    Quality = new QualityModel(Quality.GOG, new Revision(version: 2))
                };

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_game_file_is_null()
        {
            _localGame.Game.GameFile = null;
            _localGame.Game.GameFileId = 1;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_upgrade_to_custom_format_score()
        {
            var gameFileCustomFormats = Builder<CustomFormat>.CreateListOfSize(1).Build().ToList();

            var gameFile = new GameFile
            {
                Quality = new QualityModel(Quality.GOG)
            };

            _game.QualityProfile.FormatItems = gameFileCustomFormats.Select(c => new ProfileFormatItem
            {
                Format = c,
                Score = 10
            })
                .ToList();

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.DownloadPropersAndRepacks)
                .Returns(ProperDownloadTypes.DoNotPrefer);

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(s => s.ParseCustomFormat(gameFile))
                .Returns(gameFileCustomFormats);

            _localGame.Quality = new QualityModel(Quality.GOG);
            _localGame.CustomFormats = Builder<CustomFormat>.CreateListOfSize(1).Build().ToList();
            _localGame.CustomFormatScore = 20;

            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile = gameFile;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_not_upgrade_to_custom_format_score_but_is_upgrade_to_quality()
        {
            var gameFileCustomFormats = Builder<CustomFormat>.CreateListOfSize(1).Build().ToList();

            var gameFile = new GameFile
            {
                Quality = new QualityModel(Quality.GOG)
            };

            _game.QualityProfile.FormatItems = gameFileCustomFormats.Select(c => new ProfileFormatItem
            {
                Format = c,
                Score = 50
            })
                .ToList();

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.DownloadPropersAndRepacks)
                .Returns(ProperDownloadTypes.DoNotPrefer);

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(s => s.ParseCustomFormat(gameFile))
                .Returns(gameFileCustomFormats);

            _localGame.Quality = new QualityModel(Quality.Repack);
            _localGame.CustomFormats = Builder<CustomFormat>.CreateListOfSize(1).Build().ToList();
            _localGame.CustomFormatScore = 20;

            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile = gameFile;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_not_upgrade_to_custom_format_score()
        {
            var gameFileCustomFormats = Builder<CustomFormat>.CreateListOfSize(1).Build().ToList();

            var gameFile = new GameFile
            {
                Quality = new QualityModel(Quality.GOG)
            };

            _game.QualityProfile.FormatItems = gameFileCustomFormats.Select(c => new ProfileFormatItem
            {
                Format = c,
                Score = 50
            })
                .ToList();

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.DownloadPropersAndRepacks)
                .Returns(ProperDownloadTypes.DoNotPrefer);

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(s => s.ParseCustomFormat(gameFile))
                .Returns(gameFileCustomFormats);

            _localGame.Quality = new QualityModel(Quality.GOG);
            _localGame.CustomFormats = Builder<CustomFormat>.CreateListOfSize(1).Build().ToList();
            _localGame.CustomFormatScore = 20;

            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile = gameFile;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeFalse();
        }

        // gamarr-app/Gamarr#204: a DLC-only or DLC-bundle download was compared
        // against the BASE game file, which is routinely a PROPER (v2) while the
        // component release parses as "Unknown v1" — so every one of them was
        // rejected as a revision downgrade and the queue item stayed
        // importPending forever. These assert the comparison is against the
        // installed COMPONENT instead.
        private void GivenComponentRelease(ReleaseContentType contentType, string releaseName, GameVersion version = null)
        {
            _localGame.Path = @"C:\Downloads\" + releaseName;
            _localGame.Quality = new QualityModel(Quality.Unknown, new Revision(version: 1));
            _localGame.FileGameInfo = new ParsedGameInfo
            {
                ReleaseTitle = releaseName,
                ContentType = contentType,
                GameVersion = version
            };

            // The base game on disk is a PROPER — the exact shape that rejected
            // every component release before the fix.
            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile = new GameFile
            {
                Quality = new QualityModel(Quality.Repack, new Revision(version: 2))
            };

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);
        }

        private void GivenFilesOnDisk(params GameFile[] files)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(files.ToList());
        }

        private void GivenComponents(params GameComponent[] components)
        {
            Mocker.GetMock<IGameComponentService>()
                  .Setup(s => s.GetByGame(_game.Id))
                  .Returns(components.ToList());
        }

        [TestCase(ReleaseContentType.DlcOnly)]
        [TestCase(ReleaseContentType.SeasonPass)]
        public void should_accept_component_release_when_nothing_is_in_the_slot(ReleaseContentType contentType)
        {
            GivenComponentRelease(contentType, "Game.Wave.1-6.DLC.Unlocker.NSW-GRP");

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_compare_a_dlc_release_against_the_base_game_file()
        {
            GivenComponentRelease(ReleaseContentType.DlcOnly, "Game.Wave.2.DLC.NSW-GRP");

            // Only the base game file is on disk, so there is no DLC to compare
            // against and the proper base must not be consulted at all.
            GivenFilesOnDisk(new GameFile
            {
                RelativePath = "Game.iso",
                Quality = new QualityModel(Quality.Repack, new Revision(version: 2))
            });

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Verify(s => s.ParseCustomFormat(_localGame.Game.GameFile), Times.Never());
        }

        [Test]
        public void should_accept_a_dlc_release_when_a_different_dlc_is_on_disk()
        {
            GivenComponentRelease(ReleaseContentType.DlcOnly, "Game.Wave.2.DLC.NSW-GRP");

            GivenFilesOnDisk(new GameFile
            {
                RelativePath = @"DLC\Game.Wave.5.DLC.NSW-GRP\dlc.nsp",
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 2))
            });

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_a_dlc_release_that_is_a_revision_downgrade_of_the_installed_dlc()
        {
            GivenComponentRelease(ReleaseContentType.DlcOnly, "Game.Wave.2.DLC.NSW-GRP");

            GivenFilesOnDisk(new GameFile
            {
                RelativePath = @"DLC\Game.Wave.2.DLC.NSW-GRP\dlc.nsp",
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 2))
            });

            var decision = Subject.IsSatisfiedBy(_localGame, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(ImportRejectionReason.NotRevisionUpgrade);
        }

        [Test]
        public void should_accept_a_dlc_release_that_upgrades_the_installed_dlc_revision()
        {
            GivenComponentRelease(ReleaseContentType.DlcOnly, "Game.Wave.2.DLC.NSW-GRP");
            _localGame.Quality = new QualityModel(Quality.Unknown, new Revision(version: 2));

            GivenFilesOnDisk(new GameFile
            {
                RelativePath = @"DLC\Game.Wave.2.DLC.NSW-GRP\dlc.nsp",
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 1))
            });

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_match_the_installed_dlc_through_its_component_slot()
        {
            GivenComponentRelease(ReleaseContentType.DlcOnly, "Game.Wave.2.DLC.NSW-GRP");

            // The slot title is metadata-shaped ("Wave 2"), so the subfolder
            // name alone wouldn't match the release — the ComponentId link does.
            GivenComponents(new GameComponent
            {
                Id = 7,
                GameId = _game.Id,
                ComponentType = GameComponentType.Dlc,
                Key = "igdb:12345",
                Title = "Wave 2"
            });

            GivenFilesOnDisk(new GameFile
            {
                RelativePath = @"DLC\something-else-entirely\dlc.nsp",
                ComponentId = 7,
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 2))
            });

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_an_update_release_when_no_update_of_that_version_is_on_disk()
        {
            GivenComponentRelease(ReleaseContentType.UpdateOnly, "Game.Update.v1.6-GRP", GameVersion.Parse("1.6"));

            GivenFilesOnDisk(new GameFile
            {
                RelativePath = @"Updates\v1.5\update.nsp",
                GameVersion = GameVersion.Parse("1.5"),
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 2))
            });

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_an_update_release_that_is_a_revision_downgrade_of_the_same_version()
        {
            GivenComponentRelease(ReleaseContentType.UpdateOnly, "Game.Update.v1.5-GRP", GameVersion.Parse("1.5"));

            GivenFilesOnDisk(new GameFile
            {
                RelativePath = @"Updates\v1.5\update.nsp",
                GameVersion = GameVersion.Parse("1.5"),
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 2))
            });

            var decision = Subject.IsSatisfiedBy(_localGame, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(ImportRejectionReason.NotRevisionUpgrade);
        }

        [Test]
        public void should_treat_an_update_only_quality_as_a_component_release()
        {
            // Some update releases are only recognised by the quality parser, so
            // the content type stays Unknown. The mover imports those alongside
            // the base too, and the decision has to agree with it.
            _localGame.Path = @"C:\Downloads\Game.Update.Only-GRP";
            _localGame.Quality = new QualityModel(Quality.UpdateOnly, new Revision(version: 1));
            _localGame.Game.GameFileId = 1;
            _localGame.Game.GameFile = new GameFile
            {
                Quality = new QualityModel(Quality.Repack, new Revision(version: 2))
            };

            _localGame.IsComponentRelease.Should().BeTrue();

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_a_component_release_when_a_folder_backed_base_file_is_on_disk()
        {
            // A folder-style GameFile has an EMPTY RelativePath (its path is the
            // game folder), which must not be read as a component subfolder.
            GivenComponentRelease(ReleaseContentType.SeasonPass, "Game.Wave.1-6.DLC.Unlocker.NSW-GRP");

            GivenFilesOnDisk(new GameFile
            {
                RelativePath = string.Empty,
                Quality = new QualityModel(Quality.Unknown, new Revision(version: 2))
            });

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }
    }
}
