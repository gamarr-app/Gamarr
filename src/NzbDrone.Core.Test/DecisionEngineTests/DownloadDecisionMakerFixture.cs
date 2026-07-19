using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class DownloadDecisionMakerFixture : CoreTest<DownloadDecisionMaker>
    {
        private List<ReleaseInfo> _reports;
        private RemoteGame _remoteEpisode;

        private Mock<IDownloadDecisionEngineSpecification> _pass1;
        private Mock<IDownloadDecisionEngineSpecification> _pass2;
        private Mock<IDownloadDecisionEngineSpecification> _pass3;

        private Mock<IDownloadDecisionEngineSpecification> _fail1;
        private Mock<IDownloadDecisionEngineSpecification> _fail2;
        private Mock<IDownloadDecisionEngineSpecification> _fail3;

        [SetUp]
        public void Setup()
        {
            _pass1 = new Mock<IDownloadDecisionEngineSpecification>();
            _pass2 = new Mock<IDownloadDecisionEngineSpecification>();
            _pass3 = new Mock<IDownloadDecisionEngineSpecification>();

            _fail1 = new Mock<IDownloadDecisionEngineSpecification>();
            _fail2 = new Mock<IDownloadDecisionEngineSpecification>();
            _fail3 = new Mock<IDownloadDecisionEngineSpecification>();

            _pass1.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null)).Returns(DownloadSpecDecision.Accept);
            _pass2.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null)).Returns(DownloadSpecDecision.Accept);
            _pass3.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null)).Returns(DownloadSpecDecision.Accept);

            _fail1.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null)).Returns(DownloadSpecDecision.Reject(DownloadRejectionReason.Unknown, "fail1"));
            _fail2.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null)).Returns(DownloadSpecDecision.Reject(DownloadRejectionReason.Unknown, "fail2"));
            _fail3.Setup(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null)).Returns(DownloadSpecDecision.Reject(DownloadRejectionReason.Unknown, "fail3"));

            _reports = new List<ReleaseInfo> { new ReleaseInfo { Title = "Cyberpunk.2077.v2.1-CODEX" } };
            _remoteEpisode = new RemoteGame
            {
                Game = new Game(),
                ParsedGameInfo = new ParsedGameInfo()
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(c => c.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SearchCriteriaBase>())).Returns(_remoteEpisode);
        }

        private void GivenSpecifications(params Mock<IDownloadDecisionEngineSpecification>[] mocks)
        {
            Mocker.SetConstant<IEnumerable<IDownloadDecisionEngineSpecification>>(mocks.Select(c => c.Object));
        }

        [Test]
        public void should_use_component_quality_profile_for_matched_dlc_release()
        {
            GivenSpecifications(_pass1);

            _reports[0].Title = "Hades.The.Blood.Price.DLC-FAKE";
            _remoteEpisode.Game = new Game
            {
                Id = 5,
                QualityProfile = new NzbDrone.Core.Profiles.Qualities.QualityProfile { Id = 1, Name = "Game profile", FormatItems = new List<NzbDrone.Core.Profiles.ProfileFormatItem>() }
            };
            _remoteEpisode.ParsedGameInfo = new ParsedGameInfo
            {
                ContentType = ReleaseContentType.DlcOnly,
                GameTitles = new List<string> { "Hades The Blood Price" }
            };

            Mocker.GetMock<NzbDrone.Core.Games.Components.IGameComponentService>()
                  .Setup(s => s.GetByGame(5))
                  .Returns(new List<NzbDrone.Core.Games.Components.GameComponent>
                  {
                      new NzbDrone.Core.Games.Components.GameComponent
                      {
                          ComponentType = NzbDrone.Core.Games.Components.GameComponentType.Dlc,
                          Title = "The Blood Price",
                          QualityProfileId = 7
                      }
                  });

            Mocker.GetMock<NzbDrone.Core.Profiles.Qualities.IQualityProfileService>()
                  .Setup(s => s.Get(7))
                  .Returns(new NzbDrone.Core.Profiles.Qualities.QualityProfile { Id = 7, Name = "DLC profile", FormatItems = new List<NzbDrone.Core.Profiles.ProfileFormatItem>() });

            var decisions = Subject.GetRssDecision(_reports);

            decisions.First().RemoteGame.EffectiveQualityProfile.Id.Should().Be(7);
        }

        [Test]
        public void should_keep_game_profile_for_non_dlc_releases()
        {
            GivenSpecifications(_pass1);

            _remoteEpisode.Game = new Game
            {
                Id = 5,
                QualityProfile = new NzbDrone.Core.Profiles.Qualities.QualityProfile { Id = 1, Name = "Game profile", FormatItems = new List<NzbDrone.Core.Profiles.ProfileFormatItem>() }
            };

            var decisions = Subject.GetRssDecision(_reports);

            decisions.First().RemoteGame.EffectiveQualityProfile.Id.Should().Be(1);

            Mocker.GetMock<NzbDrone.Core.Games.Components.IGameComponentService>()
                  .Verify(s => s.GetByGame(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_call_all_specifications()
        {
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            Subject.GetRssDecision(_reports).ToList();

            _fail1.Verify(c => c.IsSatisfiedBy(_remoteEpisode, null), Times.Once());
            _fail2.Verify(c => c.IsSatisfiedBy(_remoteEpisode, null), Times.Once());
            _fail3.Verify(c => c.IsSatisfiedBy(_remoteEpisode, null), Times.Once());
            _pass1.Verify(c => c.IsSatisfiedBy(_remoteEpisode, null), Times.Once());
            _pass2.Verify(c => c.IsSatisfiedBy(_remoteEpisode, null), Times.Once());
            _pass3.Verify(c => c.IsSatisfiedBy(_remoteEpisode, null), Times.Once());
        }

        [Test]
        public void should_return_rejected_if_single_specs_fail()
        {
            GivenSpecifications(_fail1);

            var result = Subject.GetRssDecision(_reports);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_rejected_if_one_of_specs_fail()
        {
            GivenSpecifications(_pass1, _fail1, _pass2, _pass3);

            var result = Subject.GetRssDecision(_reports);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_pass_if_all_specs_pass()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            var result = Subject.GetRssDecision(_reports);

            result.Single().Approved.Should().BeTrue();
        }

        [Test]
        public void should_have_same_number_of_rejections_as_specs_that_failed()
        {
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            var result = Subject.GetRssDecision(_reports);
            result.Single().Rejections.Should().HaveCount(3);
        }

        [Test]
        public void should_not_attempt_to_map_episode_if_not_parsable()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);
            _reports[0].Title = "aXf92kL_mnB4zQ-rT7";

            Subject.GetRssDecision(_reports).ToList();

            Mocker.GetMock<IParsingService>().Verify(c => c.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SearchCriteriaBase>()), Times.Never());

            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
        }

        [Test]
        public void should_return_rejected_result_for_unparsable_search()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);
            _reports[0].Title = "aXf92kL_mnB4zQ-rT7";

            Subject.GetSearchDecision(_reports, new GameSearchCriteria()).ToList();

            Mocker.GetMock<IParsingService>().Verify(c => c.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SearchCriteriaBase>()), Times.Never());

            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
        }

        [Test]
        public void should_not_attempt_to_make_decision_if_series_is_unknown()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteEpisode.Game = null;

            Subject.GetRssDecision(_reports);

            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<RemoteGame>(), null), Times.Never());
        }

        [Test]
        public void broken_report_shouldnt_blowup_the_process()
        {
            GivenSpecifications(_pass1);

            Mocker.GetMock<IParsingService>().Setup(c => c.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SearchCriteriaBase>()))
                     .Throws<TestException>();

            _reports = new List<ReleaseInfo>
                {
                    new ReleaseInfo { Title = "Cyberpunk.2077.v2.1-CODEX" },
                    new ReleaseInfo { Title = "Cyberpunk.2077.v2.1-CODEX" },
                    new ReleaseInfo { Title = "Cyberpunk.2077.v2.1-CODEX" }
                };

            Subject.GetRssDecision(_reports);

            Mocker.GetMock<IParsingService>().Verify(c => c.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SearchCriteriaBase>()), Times.Exactly(_reports.Count));

            ExceptionVerification.ExpectedErrors(3);
        }

        [Test]
        public void should_return_unknown_series_rejection_if_series_is_unknown()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteEpisode.Game = null;

            var result = Subject.GetRssDecision(_reports);

            result.Should().HaveCount(1);
        }

        [Test]
        public void should_not_allow_download_if_series_is_unknown()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteEpisode.Game = null;

            var result = Subject.GetRssDecision(_reports);

            result.Should().HaveCount(1);

            // result.First().RemoteGame.DownloadAllowed.Should().BeFalse();
        }

        [Test]
        public void should_not_allow_download_if_no_game_found()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            _remoteEpisode.Game = null;

            var result = Subject.GetRssDecision(_reports);

            result.Should().HaveCount(1);
            result.First().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_a_decision_when_exception_is_caught()
        {
            GivenSpecifications(_pass1);

            Mocker.GetMock<IParsingService>().Setup(c => c.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SearchCriteriaBase>()))
                     .Throws<TestException>();

            _reports = new List<ReleaseInfo>
                {
                    new ReleaseInfo { Title = "Cyberpunk.2077.v2.1-CODEX" },
                };

            Subject.GetRssDecision(_reports).Should().HaveCount(1);

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
