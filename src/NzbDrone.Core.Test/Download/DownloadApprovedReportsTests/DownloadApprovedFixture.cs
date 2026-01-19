using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.DownloadApprovedReportsTests
{
    [TestFixture]
    public class DownloadApprovedFixture : CoreTest<ProcessDownloadDecisions>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IPrioritizeDownloadDecision>()
                .Setup(v => v.PrioritizeDecisionsForGames(It.IsAny<List<DownloadDecision>>()))
                .Returns<List<DownloadDecision>>(v => v);
        }

        private Game GetGame(int id)
        {
            return Builder<Game>.CreateNew()
                            .With(e => e.Id = id)
                                 .With(m => m.Tags = new HashSet<int>())

                            .Build();
        }

        private RemoteGame GetRemoteGame(QualityModel quality, Game game = null, DownloadProtocol downloadProtocol = DownloadProtocol.Usenet)
        {
            if (game == null)
            {
                game = GetGame(1);
            }

            game.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() };

            var remoteGame = new RemoteGame()
            {
                ParsedGameInfo = new ParsedGameInfo()
                {
                    Quality = quality,
                    Year = 1998,
                    GameTitles = new List<string> { "A Game" },
                },
                Game = game,

                Release = new ReleaseInfo()
                {
                    PublishDate = DateTime.UtcNow,
                    Title = "A.Game.1998",
                    Size = 200,
                    DownloadProtocol = downloadProtocol
                }
            };

            return remoteGame;
        }

        [Test]
        public async Task should_download_report_if_game_was_not_already_downloaded()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteGame>(), null), Times.Once());
        }

        [Test]
        public async Task should_only_download_game_once()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame));
            decisions.Add(new DownloadDecision(remoteGame));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteGame>(), null), Times.Once());
        }

        [Test]
        public async Task should_not_download_if_any_game_was_already_downloaded()
        {
            var remoteGame1 = GetRemoteGame(
                                                    new QualityModel(Quality.HDTV720p));

            var remoteGame2 = GetRemoteGame(
                                                    new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteGame>(), null), Times.Once());
        }

        [Test]
        public async Task should_return_downloaded_reports()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(1);
        }

        [Test]
        public async Task should_return_all_downloaded_reports()
        {
            var remoteGame1 = GetRemoteGame(new QualityModel(Quality.HDTV720p), GetGame(1));

            var remoteGame2 = GetRemoteGame(new QualityModel(Quality.HDTV720p), GetGame(2));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(2);
        }

        [Test]
        public async Task should_only_return_downloaded_reports()
        {
            var remoteGame1 = GetRemoteGame(
                                                    new QualityModel(Quality.HDTV720p),
                                                    GetGame(1));

            var remoteGame2 = GetRemoteGame(
                                                    new QualityModel(Quality.HDTV720p),
                                                    GetGame(2));

            var remoteGame3 = GetRemoteGame(
                                                    new QualityModel(Quality.HDTV720p),
                                                    GetGame(2));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame1));
            decisions.Add(new DownloadDecision(remoteGame2));
            decisions.Add(new DownloadDecision(remoteGame3));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(2);
        }

        [Test]
        public async Task should_not_add_to_downloaded_list_when_download_fails()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.IsAny<RemoteGame>(), null)).Throws(new Exception());

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().BeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_an_empty_list_when_none_are_appproved()
        {
            var decisions = new List<DownloadDecision>();
            RemoteGame remoteGame = null;
            decisions.Add(new DownloadDecision(remoteGame, new DownloadRejection(DownloadRejectionReason.Unknown, "Failure!")));
            decisions.Add(new DownloadDecision(remoteGame, new DownloadRejection(DownloadRejectionReason.Unknown, "Failure!")));

            Subject.GetQualifiedReports(decisions).Should().BeEmpty();
        }

        [Test]
        public async Task should_not_grab_if_pending()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame, new DownloadRejection(DownloadRejectionReason.Unknown, "Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteGame>(), null), Times.Never());
        }

        [Test]
        public async Task should_not_add_to_pending_if_game_was_grabbed()
        {
            var removeGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(removeGame));
            decisions.Add(new DownloadDecision(removeGame, new DownloadRejection(DownloadRejectionReason.Unknown, "Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.AddMany(It.IsAny<List<Tuple<DownloadDecision, PendingReleaseReason>>>()), Times.Never());
        }

        [Test]
        public async Task should_add_to_pending_even_if_already_added_to_pending()
        {
            var remoteEpisode = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode, new DownloadRejection(DownloadRejectionReason.Unknown, "Failure!", RejectionType.Temporary)));
            decisions.Add(new DownloadDecision(remoteEpisode, new DownloadRejection(DownloadRejectionReason.Unknown, "Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.AddMany(It.IsAny<List<Tuple<DownloadDecision, PendingReleaseReason>>>()), Times.Once());
        }

        [Test]
        public async Task should_add_to_failed_if_already_failed_for_that_protocol()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame));
            decisions.Add(new DownloadDecision(remoteGame));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.IsAny<RemoteGame>(), null))
                  .Throws(new DownloadClientUnavailableException("Download client failed"));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteGame>(), null), Times.Once());
        }

        [Test]
        public async Task should_not_add_to_failed_if_failed_for_a_different_protocol()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p), null, DownloadProtocol.Usenet);
            var remoteGame2 = GetRemoteGame(new QualityModel(Quality.HDTV720p), null, DownloadProtocol.Torrent);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame));
            decisions.Add(new DownloadDecision(remoteGame2));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.Is<RemoteGame>(r => r.Release.DownloadProtocol == DownloadProtocol.Usenet), null))
                  .Throws(new DownloadClientUnavailableException("Download client failed"));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.Is<RemoteGame>(r => r.Release.DownloadProtocol == DownloadProtocol.Usenet), null), Times.Once());
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.Is<RemoteGame>(r => r.Release.DownloadProtocol == DownloadProtocol.Torrent), null), Times.Once());
        }

        [Test]
        public async Task should_add_to_rejected_if_release_unavailable_on_indexer()
        {
            var remoteGame = GetRemoteGame(new QualityModel(Quality.HDTV720p));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteGame));

            Mocker.GetMock<IDownloadService>()
                  .Setup(s => s.DownloadReport(It.IsAny<RemoteGame>(), null))
                  .Throws(new ReleaseUnavailableException(remoteGame.Release, "That 404 Error is not just a Quirk"));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().BeEmpty();
            result.Rejected.Should().NotBeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
