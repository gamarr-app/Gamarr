using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class QualityAllowedByProfileSpecificationFixture : CoreTest<QualityAllowedByProfileSpecification>
    {
        private RemoteGame _remoteGame;

        public static object[] AllowedTestCases =
        {
            new object[] { Quality.Scene },
            new object[] { Quality.Uplay },
            new object[] { Quality.GOG }
        };

        public static object[] DeniedTestCases =
        {
            new object[] { Quality.Scene },
            new object[] { Quality.Epic },
            new object[] { Quality.Repack }
        };

        [SetUp]
        public void Setup()
        {
            var fakeSeries = Builder<Game>.CreateNew()
                .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.GOG.Id })
                         .Build();

            _remoteGame = new RemoteGame
            {
                Game = fakeSeries,
                ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Scene, new Revision(version: 2)) },
            };
        }

        [Test]
        [TestCaseSource("AllowedTestCases")]
        public void should_allow_if_quality_is_defined_in_profile(Quality qualityType)
        {
            _remoteGame.ParsedGameInfo.Quality.Quality = qualityType;
            _remoteGame.Game.QualityProfile.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.Scene, Quality.Uplay, Quality.GOG);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        [TestCaseSource("DeniedTestCases")]
        public void should_not_allow_if_quality_is_not_defined_in_profile(Quality qualityType)
        {
            _remoteGame.ParsedGameInfo.Quality.Quality = qualityType;
            _remoteGame.Game.QualityProfile.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.Scene, Quality.Uplay, Quality.GOG);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }
    }
}
