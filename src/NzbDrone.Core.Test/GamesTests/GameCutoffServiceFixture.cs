using System.Collections.Generic;
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

        [SetUp]
        public void Setup()
        {
            _profile = new QualityProfile
            {
                Id = 1,
                Cutoff = Quality.GOG.Id,
                UpgradeAllowed = true,
                Items = new List<QualityProfileQualityItem>
                {
                    new QualityProfileQualityItem { Quality = Quality.Unknown, Allowed = true },
                    new QualityProfileQualityItem { Quality = Quality.GOG, Allowed = true }
                }
            };

            Mocker.GetMock<IQualityProfileService>()
                  .Setup(s => s.All())
                  .Returns(new List<QualityProfile> { _profile });
        }

        [Test]
        public void should_return_empty_when_no_qualities_below_cutoff()
        {
            _profile.Cutoff = Quality.Unknown.Id;

            var pagingSpec = new PagingSpec<Game>
            {
                Page = 1,
                PageSize = 10,
                SortKey = "title",
                SortDirection = SortDirection.Ascending
            };

            var result = Subject.GamesWhereCutoffUnmet(pagingSpec);

            result.Records.Should().BeEmpty();
        }

        [Test]
        public void should_query_repository_when_qualities_below_cutoff()
        {
            var pagingSpec = new PagingSpec<Game>
            {
                Page = 1,
                PageSize = 10,
                SortKey = "title",
                SortDirection = SortDirection.Ascending
            };

            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GamesWhereCutoffUnmet(It.IsAny<PagingSpec<Game>>(), It.IsAny<List<QualitiesBelowCutoff>>()))
                  .Returns(pagingSpec);

            var result = Subject.GamesWhereCutoffUnmet(pagingSpec);

            Mocker.GetMock<IGameRepository>()
                  .Verify(s => s.GamesWhereCutoffUnmet(pagingSpec, It.IsAny<List<QualitiesBelowCutoff>>()), Times.Once());
        }
    }
}
