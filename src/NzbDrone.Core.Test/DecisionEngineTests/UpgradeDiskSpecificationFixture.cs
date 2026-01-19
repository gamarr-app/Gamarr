using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class UpgradeDiskSpecificationFixture : CoreTest<UpgradeDiskSpecification>
    {
        private UpgradeDiskSpecification _upgradeDisk;

        private RemoteGame _parseResultSingle;
        private GameFile _firstFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();
            _upgradeDisk = Mocker.Resolve<UpgradeDiskSpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            _firstFile = new GameFile { Quality = new QualityModel(Quality.GOG, new Revision(version: 2)), DateAdded = DateTime.Now };

            var fakeGame = Builder<Game>.CreateNew()
                .With(c => c.QualityProfile = new QualityProfile
                {
                    UpgradeAllowed = true,
                    Cutoff = Quality.GOG.Id,
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("None"),
                    MinFormatScore = 0,
                })
                .With(e => e.GameFile = _firstFile)
                .Build();

            _parseResultSingle = new RemoteGame
            {
                Game = fakeGame,
                ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Scene, new Revision(version: 2)) },
                CustomFormats = new List<CustomFormat>()
            };

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(x => x.ParseCustomFormat(It.IsAny<GameFile>()))
                .Returns(new List<CustomFormat>());
        }

        private void GivenProfile(QualityProfile profile)
        {
            CustomFormatsTestHelpers.GivenCustomFormats();
            profile.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems();
            profile.MinFormatScore = 0;
            _parseResultSingle.Game.QualityProfile = profile;

            Console.WriteLine(profile.ToJson());
        }

        private void GivenFileQuality(QualityModel quality)
        {
            _firstFile.Quality = quality;
        }

        private void GivenNewQuality(QualityModel quality)
        {
            _parseResultSingle.ParsedGameInfo.Quality = quality;
        }

        private void GivenOldCustomFormats(List<CustomFormat> formats)
        {
            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(x => x.ParseCustomFormat(It.IsAny<GameFile>()))
                .Returns(formats);
        }

        private void GivenNewCustomFormats(List<CustomFormat> formats)
        {
            _parseResultSingle.CustomFormats = formats;
        }

        private void WithFirstFileUpgradable()
        {
            _firstFile.Quality = new QualityModel(Quality.Scene);
        }

        [Test]
        public void should_return_true_if_game_has_no_existing_file()
        {
            _parseResultSingle.Game.GameFile = null;
            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_upgradable_if_only_game_is_upgradable()
        {
            WithFirstFileUpgradable();
            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_be_upgradable_if_qualities_are_the_same()
        {
            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(x => x.ParseCustomFormat(It.IsAny<GameFile>()))
                .Returns(new List<CustomFormat>());

            _firstFile.Quality = new QualityModel(Quality.Steam);
            _parseResultSingle.ParsedGameInfo.Quality = new QualityModel(Quality.Steam);
            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_be_upgradable_if_revision_downgrade_if_propers_are_preferred()
        {
            _firstFile.Quality = new QualityModel(Quality.Steam, new Revision(2));
            _parseResultSingle.ParsedGameInfo.Quality = new QualityModel(Quality.Steam);
            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_current_game_is_equal_to_cutoff()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Uplay, new Revision(version: 2)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_current_game_is_greater_than_cutoff()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.GOG, new Revision(version: 2)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_new_game_is_proper_but_existing_is_not()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Uplay, new Revision(version: 1)));
            GivenNewQuality(new QualityModel(Quality.Uplay, new Revision(version: 2)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_cutoff_is_met_and_quality_is_higher()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Uplay, new Revision(version: 2)));
            GivenNewQuality(new QualityModel(Quality.GOG, new Revision(version: 2)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_quality_cutoff_is_met_and_quality_is_higher_but_language_is_met()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Uplay, new Revision(version: 2)));
            GivenNewQuality(new QualityModel(Quality.GOG, new Revision(version: 2)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_cutoff_is_met_and_quality_is_higher_and_language_is_higher()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Uplay, new Revision(version: 2)));
            GivenNewQuality(new QualityModel(Quality.GOG, new Revision(version: 2)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_cutoff_is_not_met_and_new_quality_is_higher_and_language_is_higher()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Scene, new Revision(version: 2)));
            GivenNewQuality(new QualityModel(Quality.GOG, new Revision(version: 2)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_cutoff_is_not_met_and_quality_is_higher()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            // File has lower quality, release has higher quality
            GivenFileQuality(new QualityModel(Quality.Scene, new Revision(version: 1)));
            GivenNewQuality(new QualityModel(Quality.GOG, new Revision(version: 1)));
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_custom_formats_is_met_and_quality_and_format_higher()
        {
            var customFormat = new CustomFormat("My Format", new ResolutionSpecification { Value = (int)Resolution.R1080p }) { Id = 1 };

            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Uplay.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                MinFormatScore = 0,
                FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("My Format"),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Uplay));
            GivenNewQuality(new QualityModel(Quality.GOG));

            GivenOldCustomFormats(new List<CustomFormat>());
            GivenNewCustomFormats(new List<CustomFormat> { customFormat });

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_cutoffs_are_met_but_is_a_revision_upgrade()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Origin.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = true
            });

            GivenFileQuality(new QualityModel(Quality.Steam, new Revision(version: 1)));
            GivenNewQuality(new QualityModel(Quality.Steam, new Revision(version: 2)));

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_quality_profile_does_not_allow_upgrades_but_cutoff_is_set_to_highest_quality()
        {
            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Retail.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                UpgradeAllowed = false
            });

            GivenFileQuality(new QualityModel(Quality.Steam));
            GivenNewQuality(new QualityModel(Quality.GOG));

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_quality_profile_does_not_allow_upgrades_but_format_cutoff_is_above_current_score()
        {
            var customFormat = new CustomFormat("My Format", new ResolutionSpecification { Value = (int)Resolution.R1080p }) { Id = 1 };

            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Scene.Id,
                MinFormatScore = 0,
                CutoffFormatScore = 10000,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("My Format"),
                UpgradeAllowed = false
            });

            _parseResultSingle.Game.QualityProfile.FormatItems = new List<ProfileFormatItem>
            {
                new ProfileFormatItem
                {
                    Format = customFormat,
                    Score = 50
                }
            };

            GivenFileQuality(new QualityModel(Quality.Steam));
            GivenNewQuality(new QualityModel(Quality.Steam));

            GivenOldCustomFormats(new List<CustomFormat>());
            GivenNewCustomFormats(new List<CustomFormat> { customFormat });

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_quality_profile_does_not_allow_upgrades_but_format_cutoff_is_above_current_score_and_is_revision_upgrade()
        {
            var customFormat = new CustomFormat("My Format", new ResolutionSpecification { Value = (int)Resolution.R1080p }) { Id = 1 };

            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.DownloadPropersAndRepacks)
                .Returns(ProperDownloadTypes.DoNotPrefer);

            GivenProfile(new QualityProfile
            {
                Cutoff = Quality.Scene.Id,
                MinFormatScore = 0,
                CutoffFormatScore = 10000,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("My Format"),
                UpgradeAllowed = false
            });

            _parseResultSingle.Game.QualityProfile.FormatItems = new List<ProfileFormatItem>
            {
                new ProfileFormatItem
                {
                    Format = customFormat,
                    Score = 50
                }
            };

            GivenFileQuality(new QualityModel(Quality.Steam, new Revision(version: 1)));
            GivenNewQuality(new QualityModel(Quality.Steam, new Revision(version: 2)));

            GivenOldCustomFormats(new List<CustomFormat>());
            GivenNewCustomFormats(new List<CustomFormat> { customFormat });

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }
    }
}
