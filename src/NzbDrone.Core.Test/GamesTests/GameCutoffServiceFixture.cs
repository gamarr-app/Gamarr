using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GamesTests
{
    [TestFixture]
    public class GameCutoffServiceFixture : CoreTest<GameCutoffService>
    {
        private QualityProfile _profile;
        private PagingSpec<Game> _pagingSpec;

        [SetUp]
        public void Setup()
        {
            _profile = new QualityProfile
            {
                Id = 1,
                Name = "Test Profile",
                Cutoff = Quality.Repack.Id,
                UpgradeAllowed = true,
                Items = new List<QualityProfileQualityItem>
                {
                    new QualityProfileQualityItem { Quality = Quality.Unknown, Allowed = true },
                    new QualityProfileQualityItem { Quality = Quality.GOG, Allowed = true },
                    new QualityProfileQualityItem { Quality = Quality.Steam, Allowed = true },
                    new QualityProfileQualityItem { Quality = Quality.Repack, Allowed = true }
                }
            };

            _pagingSpec = new PagingSpec<Game>
            {
                Page = 1,
                PageSize = 10,
                SortKey = "Title",
                SortDirection = SortDirection.Ascending
            };

            Mocker.GetMock<IQualityProfileService>()
                  .Setup(s => s.All())
                  .Returns(new List<QualityProfile> { _profile });
        }

        [Test]
        public void should_return_empty_list_when_no_qualities_below_cutoff()
        {
            // Set cutoff to the first quality (nothing below it)
            _profile.Cutoff = Quality.Unknown.Id;

            var result = Subject.GamesWhereCutoffUnmet(_pagingSpec);

            result.Records.Should().BeEmpty();
            Mocker.GetMock<IGameRepository>()
                  .Verify(s => s.GamesWhereCutoffUnmet(It.IsAny<PagingSpec<Game>>(), It.IsAny<List<QualitiesBelowCutoff>>()), Times.Never());
        }

        [Test]
        public void should_call_repository_when_qualities_below_cutoff()
        {
            var expectedGames = new List<Game> { new Game { Id = 1, Title = "Test Game" } };

            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GamesWhereCutoffUnmet(It.IsAny<PagingSpec<Game>>(), It.IsAny<List<QualitiesBelowCutoff>>()))
                  .Returns(new PagingSpec<Game> { Records = expectedGames });

            var result = Subject.GamesWhereCutoffUnmet(_pagingSpec);

            Mocker.GetMock<IGameRepository>()
                  .Verify(s => s.GamesWhereCutoffUnmet(_pagingSpec, It.Is<List<QualitiesBelowCutoff>>(l => l.Count == 1)), Times.Once());
        }

        [Test]
        public void should_use_first_allowed_quality_when_upgrade_not_allowed()
        {
            _profile.UpgradeAllowed = false;

            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GamesWhereCutoffUnmet(It.IsAny<PagingSpec<Game>>(), It.IsAny<List<QualitiesBelowCutoff>>()))
                  .Returns(new PagingSpec<Game> { Records = new List<Game>() });

            Subject.GamesWhereCutoffUnmet(_pagingSpec);

            Mocker.GetMock<IGameRepository>()
                  .Verify(s => s.GamesWhereCutoffUnmet(_pagingSpec, It.IsAny<List<QualitiesBelowCutoff>>()), Times.Once());
        }

        [Test]
        public void should_process_multiple_profiles()
        {
            var profile2 = new QualityProfile
            {
                Id = 2,
                Name = "Test Profile 2",
                Cutoff = Quality.Steam.Id,
                UpgradeAllowed = true,
                Items = new List<QualityProfileQualityItem>
                {
                    new QualityProfileQualityItem { Quality = Quality.Unknown, Allowed = true },
                    new QualityProfileQualityItem { Quality = Quality.GOG, Allowed = true },
                    new QualityProfileQualityItem { Quality = Quality.Steam, Allowed = true }
                }
            };

            Mocker.GetMock<IQualityProfileService>()
                  .Setup(s => s.All())
                  .Returns(new List<QualityProfile> { _profile, profile2 });

            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GamesWhereCutoffUnmet(It.IsAny<PagingSpec<Game>>(), It.IsAny<List<QualitiesBelowCutoff>>()))
                  .Returns(new PagingSpec<Game> { Records = new List<Game>() });

            Subject.GamesWhereCutoffUnmet(_pagingSpec);

            Mocker.GetMock<IGameRepository>()
                  .Verify(s => s.GamesWhereCutoffUnmet(_pagingSpec, It.Is<List<QualitiesBelowCutoff>>(l => l.Count == 2)), Times.Once());
        }
    }
}
