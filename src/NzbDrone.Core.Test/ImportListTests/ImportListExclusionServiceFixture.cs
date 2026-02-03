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
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.AllExcludedIgdbIds())
                  .Returns(new List<int>());

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.AllExcludedSteamAppIds())
                  .Returns(new List<int>());
        }

        [Test]
        public void should_add_exclusion()
        {
            var exclusion = new ImportListExclusion { IgdbId = 123, GameTitle = "Test Game" };

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(123, 0))
                  .Returns(false);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.Insert(exclusion))
                  .Returns(exclusion);

            Subject.Add(exclusion);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Insert(exclusion), Times.Once());
        }

        [Test]
        public void should_not_add_duplicate_exclusion_by_igdb()
        {
            var exclusion = new ImportListExclusion { IgdbId = 123, GameTitle = "Test Game" };
            var existing = new ImportListExclusion { Id = 1, IgdbId = 123, GameTitle = "Test Game" };

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(123, 0))
                  .Returns(true);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.FindByIgdbId(123))
                  .Returns(existing);

            var result = Subject.Add(exclusion);

            result.Id.Should().Be(1);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Insert(It.IsAny<ImportListExclusion>()), Times.Never());
        }

        [Test]
        public void should_not_add_duplicate_exclusion_by_steam()
        {
            var exclusion = new ImportListExclusion { SteamAppId = 456, GameTitle = "Test Game" };
            var existing = new ImportListExclusion { Id = 1, SteamAppId = 456, GameTitle = "Test Game" };

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(0, 456))
                  .Returns(true);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.FindBySteamAppId(456))
                  .Returns(existing);

            var result = Subject.Add(exclusion);

            result.Id.Should().Be(1);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Insert(It.IsAny<ImportListExclusion>()), Times.Never());
        }

        [Test]
        public void should_check_if_game_is_excluded_by_igdb()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(123, 0))
                  .Returns(true);

            Subject.IsGameExcluded(123, 0).Should().BeTrue();
        }

        [Test]
        public void should_check_if_game_is_excluded_by_steam()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(0, 456))
                  .Returns(true);

            Subject.IsGameExcluded(0, 456).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_game_is_not_excluded()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.IsGameExcluded(123, 0))
                  .Returns(false);

            Subject.IsGameExcluded(123, 0).Should().BeFalse();
        }

        [Test]
        public void should_delete_exclusion()
        {
            Subject.Delete(1);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.Delete(1), Times.Once());
        }

        [Test]
        public void should_add_exclusions_on_games_deleted_event()
        {
            var games = new List<Game>
            {
                new Game { Id = 1, IgdbId = 100, Title = "Game 1", Year = 2023 },
                new Game { Id = 2, IgdbId = 200, Title = "Game 2", Year = 2024 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, true);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.Is<List<ImportListExclusion>>(l => l.Count == 2)), Times.Once());
        }

        [Test]
        public void should_not_add_exclusions_when_add_exclusion_flag_is_false()
        {
            var games = new List<Game>
            {
                new Game { Id = 1, IgdbId = 100, Title = "Game 1", Year = 2023 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, false);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.IsAny<List<ImportListExclusion>>()), Times.Never());
        }

        [Test]
        public void should_not_add_existing_exclusions_on_delete()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.AllExcludedIgdbIds())
                  .Returns(new List<int> { 100 });

            var games = new List<Game>
            {
                new Game { Id = 1, IgdbId = 100, Title = "Game 1", Year = 2023 },
                new Game { Id = 2, IgdbId = 200, Title = "Game 2", Year = 2024 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, true);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.Is<List<ImportListExclusion>>(l => l.Count == 1 && l.First().IgdbId == 200)), Times.Once());
        }

        [Test]
        public void should_not_add_existing_steam_exclusions_on_delete()
        {
            Mocker.GetMock<IImportListExclusionRepository>()
                  .Setup(s => s.AllExcludedSteamAppIds())
                  .Returns(new List<int> { 500 });

            var games = new List<Game>
            {
                new Game { Id = 1, SteamAppId = 500, Title = "Game 1", Year = 2023 },
                new Game { Id = 2, SteamAppId = 600, Title = "Game 2", Year = 2024 }
            };

            var deleteEvent = new GamesDeletedEvent(games, false, true);

            Subject.HandleAsync(deleteEvent);

            Mocker.GetMock<IImportListExclusionRepository>()
                  .Verify(s => s.InsertMany(It.Is<List<ImportListExclusion>>(l => l.Count == 1 && l.First().SteamAppId == 600)), Times.Once());
        }
    }
}
