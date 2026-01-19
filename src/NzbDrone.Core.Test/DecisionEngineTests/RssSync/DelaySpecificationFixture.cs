using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class DelaySpecificationFixture : CoreTest<DelaySpecification>
    {
        private QualityProfile _profile;
        private DelayProfile _delayProfile;
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _profile = Builder<QualityProfile>.CreateNew()
                                       .Build();

            _delayProfile = Builder<DelayProfile>.CreateNew()
                                                 .With(d => d.PreferredProtocol = DownloadProtocol.Usenet)
                                                 .Build();

            var series = Builder<Game>.CreateNew()
                                        .With(s => s.QualityProfile = _profile)
                                        .Build();

            _remoteGame = Builder<RemoteGame>.CreateNew()
                                                   .With(r => r.Game = series)
                                                   .Build();

            _profile.Items = new List<QualityProfileQualityItem>();
            _profile.Items.Add(new QualityProfileQualityItem { Allowed = true, Quality = Quality.Uplay });
            _profile.Items.Add(new QualityProfileQualityItem { Allowed = true, Quality = Quality.Epic });
            _profile.Items.Add(new QualityProfileQualityItem { Allowed = true, Quality = Quality.Repack });

            _profile.Cutoff = Quality.Epic.Id;

            _remoteGame.ParsedGameInfo = new ParsedGameInfo();
            _remoteGame.Release = new ReleaseInfo();
            _remoteGame.Release.DownloadProtocol = DownloadProtocol.Usenet;

            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(_delayProfile);

            Mocker.GetMock<IPendingReleaseService>()
                  .Setup(s => s.GetPendingRemoteGames(It.IsAny<int>()))
                  .Returns(new List<RemoteGame>());
        }

        private void GivenExistingFile(QualityModel quality)
        {
            // _remoteEpisode.Episodes.First().EpisodeFileId = 1;
            _remoteGame.Game.GameFile = new GameFile { Quality = quality };
        }

        private void GivenUpgradeForExistingFile()
        {
            Mocker.GetMock<IUpgradableSpecification>()
                  .Setup(s => s.IsUpgradable(It.IsAny<QualityProfile>(), It.IsAny<QualityModel>(), It.IsAny<List<CustomFormat>>(), It.IsAny<QualityModel>(), It.IsAny<List<CustomFormat>>()))
                  .Returns(UpgradeableRejectReason.None);
        }

        [Test]
        public void should_be_true_when_user_invoked_search()
        {
            Subject.IsSatisfiedBy(new RemoteGame(), new GameSearchCriteria() { UserInvokedSearch = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_system_invoked_search_and_release_is_younger_than_delay()
        {
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Scene);
            _remoteGame.Release.PublishDate = DateTime.UtcNow;

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteGame, new GameSearchCriteria()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_profile_does_not_have_a_delay()
        {
            _delayProfile.UsenetDelay = 0;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_quality_is_last_allowed_in_profile_and_bypass_disabled()
        {
            _remoteGame.Release.PublishDate = DateTime.UtcNow;
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Repack);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_quality_is_last_allowed_in_profile_and_bypass_enabled()
        {
            _delayProfile.UsenetDelay = 720;
            _delayProfile.BypassIfHighestQuality = true;

            _remoteGame.Release.PublishDate = DateTime.UtcNow;
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Repack);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_release_is_older_than_delay()
        {
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Uplay);
            _remoteGame.Release.PublishDate = DateTime.UtcNow.AddHours(-10);

            _delayProfile.UsenetDelay = 60;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_release_is_younger_than_delay()
        {
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Scene);
            _remoteGame.Release.PublishDate = DateTime.UtcNow;

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_release_is_a_proper_for_existing_game()
        {
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Uplay, new Revision(version: 2));
            _remoteGame.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.Uplay));
            GivenUpgradeForExistingFile();

            Mocker.GetMock<IUpgradableSpecification>()
                  .Setup(s => s.IsRevisionUpgrade(It.IsAny<QualityModel>(), It.IsAny<QualityModel>()))
                  .Returns(true);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_release_is_a_real_for_existing_game()
        {
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Uplay, new Revision(real: 1));
            _remoteGame.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.Uplay));
            GivenUpgradeForExistingFile();

            Mocker.GetMock<IUpgradableSpecification>()
                  .Setup(s => s.IsRevisionUpgrade(It.IsAny<QualityModel>(), It.IsAny<QualityModel>()))
                  .Returns(true);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_release_is_proper_for_existing_game_of_different_quality()
        {
            _remoteGame.ParsedGameInfo.Quality = new QualityModel(Quality.Uplay, new Revision(version: 2));
            _remoteGame.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.Scene));

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_false_when_custom_format_score_is_above_minimum_but_bypass_disabled()
        {
            _remoteGame.Release.PublishDate = DateTime.UtcNow;
            _remoteGame.CustomFormatScore = 100;

            _delayProfile.UsenetDelay = 720;
            _delayProfile.MinimumCustomFormatScore = 50;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_false_when_custom_format_score_is_above_minimum_and_bypass_enabled_but_under_minimum()
        {
            _remoteGame.Release.PublishDate = DateTime.UtcNow;
            _remoteGame.CustomFormatScore = 5;

            _delayProfile.UsenetDelay = 720;
            _delayProfile.BypassIfAboveCustomFormatScore = true;
            _delayProfile.MinimumCustomFormatScore = 50;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_custom_format_score_is_above_minimum_and_bypass_enabled()
        {
            _remoteGame.Release.PublishDate = DateTime.UtcNow;
            _remoteGame.CustomFormatScore = 100;

            _delayProfile.UsenetDelay = 720;
            _delayProfile.BypassIfAboveCustomFormatScore = true;
            _delayProfile.MinimumCustomFormatScore = 50;

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
