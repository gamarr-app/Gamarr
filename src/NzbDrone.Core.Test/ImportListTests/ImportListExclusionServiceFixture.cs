using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests
{
    [TestFixture]
    public class ImportListExclusionServiceFixture : CoreTest<ImportListExclusionService>
    {
        private ImportListExclusion _exclusion;

        [SetUp]
        public void Setup()
        {
            _exclusion = new ImportListExclusion
            {
                Id = 1,
                IgdbId = 12345,
                SteamAppId = 67890,
                GameTitle = "Test Game",
                GameYear = 2023
            };

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.AllExcludedIgdbIds())
                  .Returns(new List<int>());

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(It.IsAny<int>()))
                  .Returns(false);
        }

        private void GivenExistingExclusion()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(_exclusion.IgdbId))
                  .Returns(true);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.FindByIgdbid(_exclusion.IgdbId))
                  .Returns(_exclusion);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.AllExcludedIgdbIds())
                  .Returns(new List<int> { _exclusion.IgdbId });
        }

        [Test]
        public void should_add_new_exclusion()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.Insert(_exclusion))
                  .Returns(_exclusion);

            Subject.Add(_exclusion);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Insert(_exclusion), Times.Once());
        }

        [Test]
        public void should_return_existing_exclusion_if_already_excluded()
        {
            GivenExistingExclusion();

            var result = Subject.Add(_exclusion);

            result.Should().Be(_exclusion);
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Insert(It.IsAny<ImportListExclusion>()), Times.Never());
        }

        [Test]
        public void should_return_true_when_game_is_excluded()
        {
            GivenExistingExclusion();

            Subject.IsGameExcluded(_exclusion.IgdbId).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_game_is_not_excluded()
        {
            Subject.IsGameExcluded(99999).Should().BeFalse();
        }

        [Test]
        public void should_delete_exclusion_by_id()
        {
            Subject.Delete(1);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Delete(1), Times.Once());
        }

        [Test]
        public void should_delete_multiple_exclusions()
        {
            var ids = new List<int> { 1, 2, 3 };

            Subject.Delete(ids);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.DeleteMany(ids), Times.Once());
        }

        [Test]
        public void should_get_all_exclusions()
        {
            var exclusions = new List<ImportListExclusion> { _exclusion };

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.All())
                  .Returns(exclusions);

            Subject.All().Should().BeEquivalentTo(exclusions);
        }

        [Test]
        public void should_get_all_excluded_igdb_ids()
        {
            var ids = new List<int> { 1, 2, 3 };

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.AllExcludedIgdbIds())
                  .Returns(ids);

            Subject.AllExcludedIgdbIds().Should().BeEquivalentTo(ids);
        }

        [Test]
        public void should_update_exclusion()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.Update(_exclusion))
                  .Returns(_exclusion);

            Subject.Update(_exclusion);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Update(_exclusion), Times.Once());
        }

        [Test]
        public void should_add_exclusions_for_deleted_games_when_flag_is_set()
        {
            var games = new List<Game>
            {
                new Game { Id = 1, IgdbId = 111, Title = "Game 1", Year = 2021 },
                new Game { Id = 2, IgdbId = 222, Title = "Game 2", Year = 2022 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, true);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.Is<List<ImportListExclusion>>(l => l.Count == 2)), Times.Once());
        }

        [Test]
        public void should_not_add_exclusions_for_deleted_games_when_flag_is_not_set()
        {
            var games = new List<Game>
            {
                new Game { Id = 1, IgdbId = 111, Title = "Game 1", Year = 2021 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, false);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.IsAny<List<ImportListExclusion>>()), Times.Never());
        }

        [Test]
        public void should_dedupe_exclusions_when_adding_multiple()
        {
            var exclusions = new List<ImportListExclusion>
            {
                new ImportListExclusion { IgdbId = 111 },
                new ImportListExclusion { IgdbId = 111 }, // duplicate
                new ImportListExclusion { IgdbId = 222 }
            };

            Subject.Add(exclusions);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.Is<List<ImportListExclusion>>(l => l.Count == 2)), Times.Once());
        }

        [Test]
        public void should_not_add_already_existing_exclusions()
        {
            GivenExistingExclusion();

            var exclusions = new List<ImportListExclusion>
            {
                new ImportListExclusion { IgdbId = _exclusion.IgdbId },
                new ImportListExclusion { IgdbId = 999 }
            };

            Subject.Add(exclusions);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.Is<List<ImportListExclusion>>(l => l.Count == 1 && l[0].IgdbId == 999)), Times.Once());
        }
    }
}
