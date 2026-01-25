using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    // TODO: Update for custom qualities!
    public class PrioritizeDownloadDecisionFixture : CoreTest<DownloadDecisionPriorizationService>
    {
        private CustomFormat _customFormat1;
        private CustomFormat _customFormat2;

        [SetUp]
        public void Setup()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);

            _customFormat1 = new CustomFormat("My Format 1", new LanguageSpecification { Value = (int)Language.English }) { Id = 1 };
            _customFormat2 = new CustomFormat("My Format 2", new LanguageSpecification { Value = (int)Language.French }) { Id = 2 };

            CustomFormatsTestHelpers.GivenCustomFormats(_customFormat1, _customFormat2);

            Mocker.GetMock<IQualityDefinitionService>()
                  .Setup(s => s.Get(It.IsAny<Quality>()))
                  .Returns(new QualityDefinition { PreferredSize = null });
        }

        private void GivenPreferredSize(double? size)
        {
            Mocker.GetMock<IQualityDefinitionService>()
                  .Setup(s => s.Get(It.IsAny<Quality>()))
                  .Returns(new QualityDefinition { PreferredSize = size });
        }

        private RemoteGame GivenRemoteGame(QualityModel quality, int age = 0, long size = 0, DownloadProtocol downloadProtocol = DownloadProtocol.Usenet, int runtime = 150, int indexerPriority = 25)
        {
            var remoteGame = new RemoteGame();
            remoteGame.ParsedGameInfo = new ParsedGameInfo();
            remoteGame.ParsedGameInfo.GameTitles = new List<string> { "A Game" };
            remoteGame.ParsedGameInfo.Year = 1998;
            remoteGame.ParsedGameInfo.Quality = quality;

            remoteGame.Game = Builder<Game>.CreateNew().With(m => m.QualityProfile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_customFormat1.Name, _customFormat2.Name),
                MinFormatScore = 0
            })
                .With(m => m.Title = "A Game")
                .With(m => m.GameMetadata.Value.Runtime = runtime).Build();

            remoteGame.Release = new ReleaseInfo();
            remoteGame.Release.PublishDate = DateTime.Now.AddDays(-age);
            remoteGame.Release.Size = size;
            remoteGame.Release.DownloadProtocol = downloadProtocol;
            remoteGame.Release.Title = "A Game 1998";
            remoteGame.Release.IndexerPriority = indexerPriority;

            remoteGame.CustomFormats = new List<CustomFormat>();
            remoteGame.CustomFormatScore = 0;

            return remoteGame;
        }

        private void GivenPreferredDownloadProtocol(DownloadProtocol downloadProtocol)
        {
            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(new DelayProfile
                  {
                      PreferredProtocol = downloadProtocol
                  });
        }

        [Test]
        public void should_put_reals_before_non_reals()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay, new Revision(version: 1, real: 0)));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay, new Revision(version: 1, real: 1)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Revision.Real.Should().Be(1);
        }

        [Test]
        public void should_put_propers_before_non_propers()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay, new Revision(version: 1)));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay, new Revision(version: 2)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_put_higher_quality_before_lower()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Scene));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Quality.Should().Be(Quality.Uplay);
        }

        [Test]
        public void should_order_by_age_then_largest_rounded_to_200mb()
        {
            var remoteGameSd = GivenRemoteGame(new QualityModel(Quality.Scene), size: 100.Megabytes(), age: 1);
            var remoteGameHdSmallOld = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 1200.Megabytes(), age: 1000);
            var remoteGameSmallYoung = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 1250.Megabytes(), age: 10);
            var remoteGameHdLargeYoung = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 3000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGameSd));
            decisions.Add(new DownloadDecision(remoteGameHdSmallOld));
            decisions.Add(new DownloadDecision(remoteGameSmallYoung));
            decisions.Add(new DownloadDecision(remoteGameHdLargeYoung));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGameHdLargeYoung);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_both_over()
        {
            // 2 MB/Min * 150 Min Runtime = 300 MB
            GivenPreferredSize(2);

            var remoteGameSmall = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 400.Megabytes(), age: 1);
            var remoteGameLarge = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 15000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGameSmall));
            decisions.Add(new DownloadDecision(remoteGameLarge));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGameSmall);
        }

        [Test]
        public void should_order_by_largest_to_if_zero_runtime()
        {
            // 2 MB/Min * 150 Min Runtime = 300 MB
            GivenPreferredSize(2);

            var remoteGameSmall = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 400.Megabytes(), age: 1, runtime: 0);
            var remoteGameLarge = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 15000.Megabytes(), age: 1, runtime: 0);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGameSmall));
            decisions.Add(new DownloadDecision(remoteGameLarge));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGameLarge);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_both_under()
        {
            // 390 MB/Min * 150 Min Runtime = 58,500 MB
            GivenPreferredSize(390);

            var remoteGameSmall = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 100.Megabytes(), age: 1);
            var remoteGameLarge = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 15000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGameSmall));
            decisions.Add(new DownloadDecision(remoteGameLarge));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGameLarge);
        }

        [Test]
        public void should_order_by_closest_to_preferred_size_if_preferred_is_in_between()
        {
            // 46 MB/Min * 150 Min Runtime = 6900 MB
            GivenPreferredSize(46);

            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 100.Megabytes(), age: 1);
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 500.Megabytes(), age: 1);
            var remoteGame3 = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 7000.Megabytes(), age: 1);
            var remoteGame4 = GivenRemoteGame(new QualityModel(Quality.Uplay), size: 15000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));
            decisions.Add(new DownloadDecision(remoteGame3));
            decisions.Add(new DownloadDecision(remoteGame4));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGame3);
        }

        [Test]
        public void should_order_by_youngest()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay), age: 10);
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay), age: 5);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGame2);
        }

        [Test]
        public void should_put_usenet_above_torrent_when_usenet_is_preferred()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);

            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay), downloadProtocol: DownloadProtocol.Torrent);
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay), downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Release.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
        }

        [Test]
        public void should_put_torrent_above_usenet_when_torrent_is_preferred()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Torrent);

            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay), downloadProtocol: DownloadProtocol.Torrent);
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay), downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Release.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
        }

        [Test]
        public void should_prefer_releases_with_more_seeders()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 100;

            remoteGame1.Release = torrentInfo1;
            remoteGame1.Release.Title = "A Game 1998";
            remoteGame2.Release = torrentInfo2;
            remoteGame2.Release.Title = "A Game 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteGame.Release).Seeders.Should().Be(torrentInfo2.Seeders);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_given_equal_number_of_seeds()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Peers = 100;

            remoteGame1.Release = torrentInfo1;
            remoteGame1.Release.Title = "A Game 1998";
            remoteGame2.Release = torrentInfo2;
            remoteGame2.Release.Title = "A Game 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteGame.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_no_seeds()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 0;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 0;
            torrentInfo2.Peers = 100;

            remoteGame1.Release = torrentInfo1;
            remoteGame1.Release.Title = "A Game 1998";
            remoteGame2.Release = torrentInfo2;
            remoteGame2.Release.Title = "A Game 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteGame.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_first_release_if_peers_and_size_are_too_similar()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 1000;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteGame1.Release = torrentInfo1;
            remoteGame1.Release.Title = "A Game 1998";
            remoteGame2.Release = torrentInfo2;
            remoteGame2.Release.Title = "A Game 1998";

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteGame.Release).Should().Be(torrentInfo1);
        }

        [Test]
        public void should_prefer_first_release_if_age_and_size_are_too_similar()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Uplay));

            remoteGame1.Release.PublishDate = DateTime.UtcNow.AddDays(-100);
            remoteGame1.Release.Size = 200.Megabytes();

            remoteGame2.Release.PublishDate = DateTime.UtcNow.AddDays(-150);
            remoteGame2.Release.Size = 250.Megabytes();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Release.Should().Be(remoteGame1.Release);
        }

        [Test]
        public void should_prefer_better_custom_format()
        {
            var quality1 = new QualityModel(Quality.Repack);
            var remoteGame1 = GivenRemoteGame(quality1);

            var quality2 = new QualityModel(Quality.Repack);
            var remoteGame2 = GivenRemoteGame(quality2);
            remoteGame2.CustomFormats.Add(_customFormat1);
            remoteGame2.CustomFormatScore = remoteGame2.Game.QualityProfile.CalculateCustomFormatScore(remoteGame2.CustomFormats);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Release.Should().Be(remoteGame2.Release);
        }

        [Test]
        public void should_prefer_better_custom_format2()
        {
            var quality1 = new QualityModel(Quality.Repack);
            var remoteGame1 = GivenRemoteGame(quality1);
            remoteGame1.CustomFormats.Add(_customFormat1);
            remoteGame1.CustomFormatScore = remoteGame1.Game.QualityProfile.CalculateCustomFormatScore(remoteGame1.CustomFormats);

            var quality2 = new QualityModel(Quality.Repack);
            var remoteGame2 = GivenRemoteGame(quality2);
            remoteGame2.CustomFormats.Add(_customFormat2);
            remoteGame2.CustomFormatScore = remoteGame2.Game.QualityProfile.CalculateCustomFormatScore(remoteGame2.CustomFormats);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Release.Should().Be(remoteGame2.Release);
        }

        [Test]
        public void should_prefer_2_custom_formats()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Repack));
            remoteGame1.CustomFormats.Add(_customFormat1);
            remoteGame1.CustomFormatScore = remoteGame1.Game.QualityProfile.CalculateCustomFormatScore(remoteGame1.CustomFormats);

            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Repack));
            remoteGame2.CustomFormats.AddRange(new List<CustomFormat> { _customFormat1, _customFormat2 });
            remoteGame2.CustomFormatScore = remoteGame2.Game.QualityProfile.CalculateCustomFormatScore(remoteGame2.CustomFormats);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Release.Should().Be(remoteGame2.Release);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_prefer_and_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(1)));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(2)));

            remoteGame1.CustomFormatScore = 10;
            remoteGame2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_do_not_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(1)));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(2)));

            remoteGame1.CustomFormatScore = 10;
            remoteGame2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_score_over_proper_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(1)));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(2)));

            remoteGame1.CustomFormatScore = 10;
            remoteGame2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Quality.Should().Be(Quality.Steam);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteGame.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void should_prefer_score_over_real_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(1, 0)));
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam, new Revision(1, 1)));

            remoteGame1.CustomFormatScore = 10;
            remoteGame2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Quality.Should().Be(Quality.Steam);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Revision.Real.Should().Be(0);
            qualifiedReports.First().RemoteGame.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void sort_download_decisions_based_on_indexer_priority()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Steam), indexerPriority: 25);
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam), indexerPriority: 50);
            var remoteGame3 = GivenRemoteGame(new QualityModel(Quality.Steam), indexerPriority: 1);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteGame1), new DownloadDecision(remoteGame2), new DownloadDecision(remoteGame3) });

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGame3);
            qualifiedReports.Skip(1).First().RemoteGame.Should().Be(remoteGame1);
            qualifiedReports.Last().RemoteGame.Should().Be(remoteGame2);
        }

        [Test]
        public void ensure_download_decisions_indexer_priority_is_not_perfered_over_quality()
        {
            // Quality order: Scene (4) < Steam (9) < Uplay (12)
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay), indexerPriority: 25);
            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam), indexerPriority: 50);
            var remoteGame3 = GivenRemoteGame(new QualityModel(Quality.Scene), indexerPriority: 1);
            var remoteGame4 = GivenRemoteGame(new QualityModel(Quality.Steam), indexerPriority: 25);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteGame1), new DownloadDecision(remoteGame2), new DownloadDecision(remoteGame3), new DownloadDecision(remoteGame4) });

            // Quality takes priority over indexer priority
            // Expected order: Uplay (highest), then Steam (lower indexer priority first), then Scene (lowest)
            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.Should().Be(remoteGame1);
            qualifiedReports.Skip(1).First().RemoteGame.Should().Be(remoteGame4);
            qualifiedReports.Skip(2).First().RemoteGame.Should().Be(remoteGame2);
            qualifiedReports.Last().RemoteGame.Should().Be(remoteGame3);
        }

        [Test]
        public void should_prefer_release_with_all_dlc_over_base_game()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Steam));
            remoteGame1.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;

            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam));
            remoteGame2.ParsedGameInfo.ContentType = ReleaseContentType.BaseGameWithAllDlc;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.ContentType.Should().Be(ReleaseContentType.BaseGameWithAllDlc);
        }

        [Test]
        public void should_prefer_release_with_all_dlc_over_unknown_content_type()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Steam));
            remoteGame1.ParsedGameInfo.ContentType = ReleaseContentType.Unknown;

            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Steam));
            remoteGame2.ParsedGameInfo.ContentType = ReleaseContentType.BaseGameWithAllDlc;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.ContentType.Should().Be(ReleaseContentType.BaseGameWithAllDlc);
        }

        [Test]
        public void should_not_prefer_dlc_content_type_over_higher_quality()
        {
            var remoteGame1 = GivenRemoteGame(new QualityModel(Quality.Uplay));
            remoteGame1.ParsedGameInfo.ContentType = ReleaseContentType.BaseGame;

            var remoteGame2 = GivenRemoteGame(new QualityModel(Quality.Scene));
            remoteGame2.ParsedGameInfo.ContentType = ReleaseContentType.BaseGameWithAllDlc;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            // Higher quality (Uplay) should still win over content type
            var qualifiedReports = Subject.PrioritizeDecisionsForGames(decisions);
            qualifiedReports.First().RemoteGame.ParsedGameInfo.Quality.Quality.Should().Be(Quality.Uplay);
        }
    }
}
