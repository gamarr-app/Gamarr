using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Games;
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
    }
}
