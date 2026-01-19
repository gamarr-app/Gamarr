using System;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]

    public class ProperSpecificationFixture : CoreTest<ProperSpecification>
    {
        private RemoteGame _parseResultSingle;
        private GameFile _firstFile;
        private GameFile _secondFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            _firstFile = new GameFile { Quality = new QualityModel(Quality.GOG, new Revision(version: 1)), DateAdded = DateTime.Now };
            _secondFile = new GameFile { Quality = new QualityModel(Quality.GOG, new Revision(version: 1)), DateAdded = DateTime.Now };

            var fakeSeries = Builder<Game>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.GOG.Id })
                         .With(c => c.GameFile = _firstFile)
                         .Build();

            _parseResultSingle = new RemoteGame
            {
                Game = fakeSeries,
                ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Scene, new Revision(version: 2)) },
            };
        }

        private void WithFirstFileUpgradable()
        {
            _firstFile.Quality = new QualityModel(Quality.Scene);

            // Ensure the profile allows upgrades and cutoff is not met
            _parseResultSingle.Game.QualityProfile.Items = Qualities.QualityFixture.GetDefaultQualities();
            _parseResultSingle.Game.QualityProfile.UpgradeAllowed = true;
            _parseResultSingle.Game.QualityProfile.Cutoff = Quality.Steam.Id;

            // Set the release to a higher QUALITY (not just revision) so it's a quality upgrade
            // This bypasses the 7-day proper check since it's not just a revision upgrade
            _parseResultSingle.ParsedGameInfo.Quality = new QualityModel(Quality.GOG, new Revision(version: 2));
        }

        [Test]
        public void should_return_false_when_gameFile_was_added_more_than_7_days_ago()
        {
            _firstFile.Quality.Quality = Quality.Scene;

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_gameFile_was_added_more_than_7_days_ago_but_proper_is_for_better_quality()
        {
            WithFirstFileUpgradable();

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_episodeFile_was_added_more_than_7_days_ago_but_is_for_search()
        {
            WithFirstFileUpgradable();

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, new GameSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_proper_but_auto_download_propers_is_false()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            _firstFile.Quality.Quality = Quality.Scene;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_gameFile_was_added_today()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            _firstFile.Quality.Quality = Quality.Scene;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        public void should_return_true_when_propers_are_not_preferred()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            _firstFile.Quality.Quality = Quality.Scene;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }
    }
}
