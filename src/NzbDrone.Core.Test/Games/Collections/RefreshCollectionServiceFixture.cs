using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.ImportLists.ImportExclusions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Games.Collections
{
    [TestFixture]
    public class RefreshCollectionServiceFixture : CoreTest<RefreshCollectionService>
    {
        private GameCollection _collection;
        private GameCollection _collectionInfo;
        private List<GameMetadata> _collectionGames;

        [SetUp]
        public void Setup()
        {
            _collection = new GameCollection
            {
                Id = 1,
                IgdbId = 100,
                Title = "Test Collection",
                CleanTitle = "testcollection",
                SortTitle = "test collection",
                Overview = "Original overview",
                Monitored = true,
                QualityProfileId = 1,
                RootFolderPath = "/games",
                SearchOnAdd = true,
                MinimumAvailability = GameStatusType.Released,
                Tags = new HashSet<int> { 1 },
                LastInfoSync = DateTime.UtcNow.AddDays(-20)
            };

            _collectionGames = new List<GameMetadata>
            {
                new GameMetadata
                {
                    IgdbId = 201,
                    Title = "Game One",
                    Status = GameStatusType.Released
                },
                new GameMetadata
                {
                    IgdbId = 202,
                    Title = "Game Two",
                    Status = GameStatusType.Released
                }
            };

            _collectionInfo = new GameCollection
            {
                IgdbId = 100,
                Title = "Updated Collection Title",
                CleanTitle = "updatedcollectiontitle",
                SortTitle = "updated collection title",
                Overview = "Updated overview",
                Images = new List<MediaCover.MediaCover>(),
                Games = _collectionGames
            };

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.GetCollection(It.IsAny<int>()))
                  .Returns(_collection);

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.GetAllCollections())
                  .Returns(new List<GameCollection> { _collection });

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.UpdateCollection(It.IsAny<GameCollection>()))
                  .Returns<GameCollection>(c => c);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(It.IsAny<int>()))
                  .Returns(_collectionInfo);

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(It.IsAny<int>()))
                  .Returns(() => new List<GameMetadata>(_collectionGames));

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AllGameIgdbIds())
                  .Returns(new List<int>());

            Mocker.GetMock<IImportListExclusionService>()
                  .Setup(s => s.All())
                  .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IAddGameService>()
                  .Setup(s => s.AddGames(It.IsAny<List<Game>>(), It.IsAny<bool>()))
                  .Returns<List<Game>, bool>((games, _) => games);
        }

        private RefreshCollectionsCommand CreateCommand(List<int> collectionIds = null)
        {
            return new RefreshCollectionsCommand(collectionIds ?? new List<int>())
            {
                Trigger = CommandTrigger.Manual
            };
        }

        [Test]
        public void should_add_new_games_from_collection()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.Count == 2 &&
                      g.Any(x => x.IgdbId == 201) &&
                      g.Any(x => x.IgdbId == 202)), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_set_collection_properties_on_added_games()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.All(x => x.QualityProfileId == _collection.QualityProfileId) &&
                      g.All(x => x.RootFolderPath == _collection.RootFolderPath) &&
                      g.All(x => x.Monitored == true) &&
                      g.All(x => x.Tags.Contains(1))), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_set_add_options_with_collection_method()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.All(x => x.AddOptions.AddMethod == AddGameMethod.Collection) &&
                      g.All(x => x.AddOptions.SearchForGame == _collection.SearchOnAdd)), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_filter_out_games_already_in_library()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AllGameIgdbIds())
                  .Returns(new List<int> { 201 });

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.Count == 1 &&
                      g[0].IgdbId == 202), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_filter_out_games_in_import_exclusions()
        {
            Mocker.GetMock<IImportListExclusionService>()
                  .Setup(s => s.All())
                  .Returns(new List<ImportListExclusion>
                  {
                      new ImportListExclusion { IgdbId = 202, GameTitle = "Game Two" }
                  });

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.Count == 1 &&
                      g[0].IgdbId == 201), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_filter_out_games_with_non_released_or_early_access_status()
        {
            _collectionGames.Add(new GameMetadata
            {
                IgdbId = 203,
                Title = "Announced Game",
                Status = GameStatusType.Announced
            });

            _collectionGames.Add(new GameMetadata
            {
                IgdbId = 204,
                Title = "TBA Game",
                Status = GameStatusType.TBA
            });

            // Return the full list from metadata service (simulating after refresh)
            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(() => new List<GameMetadata>(_collectionGames));

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.Count == 2 &&
                      g.All(x => x.IgdbId == 201 || x.IgdbId == 202)), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_include_early_access_games()
        {
            _collectionGames.Add(new GameMetadata
            {
                IgdbId = 205,
                Title = "Early Access Game",
                Status = GameStatusType.EarlyAccess
            });

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(() => new List<GameMetadata>(_collectionGames));

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.Count == 3 &&
                      g.Any(x => x.IgdbId == 205)), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_not_add_games_when_collection_is_not_monitored()
        {
            _collection.Monitored = false;

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.IsAny<List<Game>>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_not_add_games_when_all_already_exist()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AllGameIgdbIds())
                  .Returns(new List<int> { 201, 202 });

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.IsAny<List<Game>>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_remove_collection_when_game_not_found_exception()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(_collection.IgdbId))
                  .Throws(new GameNotFoundException(_collection.IgdbId));

            Assert.Throws<GameNotFoundException>(() =>
                Subject.Execute(CreateCommand(new List<int> { _collection.Id })));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.RemoveCollection(It.Is<GameCollection>(c => c.Id == _collection.Id)), Times.Once());
        }

        [Test]
        public void should_update_collection_title_when_changed()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.Is<GameCollection>(c =>
                      c.Title == "Updated Collection Title")), Times.Once());
        }

        [Test]
        public void should_update_collection_overview()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.Is<GameCollection>(c =>
                      c.Overview == "Updated overview")), Times.Once());
        }

        [Test]
        public void should_update_clean_title_and_sort_title()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.Is<GameCollection>(c =>
                      c.CleanTitle == "updatedcollectiontitle" &&
                      c.SortTitle == "updated collection title")), Times.Once());
        }

        [Test]
        public void should_update_last_info_sync()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.Is<GameCollection>(c =>
                      c.LastInfoSync.HasValue &&
                      c.LastInfoSync.Value >= DateTime.UtcNow.AddMinutes(-1))), Times.Once());
        }

        [Test]
        public void should_upsert_remote_game_metadata()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.UpsertMany(It.Is<List<GameMetadata>>(g =>
                      g.Count == 2 &&
                      g.Any(x => x.IgdbId == 201) &&
                      g.Any(x => x.IgdbId == 202))), Times.Once());
        }

        [Test]
        public void should_set_collection_igdb_id_on_game_metadata()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.UpsertMany(It.Is<List<GameMetadata>>(g =>
                      g.All(x => x.CollectionIgdbId == _collection.IgdbId))), Times.Once());
        }

        [Test]
        public void should_delete_metadata_no_longer_in_remote_collection()
        {
            var orphanedMetadata = new GameMetadata
            {
                IgdbId = 999,
                Title = "Removed Game",
                CollectionIgdbId = _collection.IgdbId
            };

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(new List<GameMetadata> { orphanedMetadata });

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.DeleteMany(It.Is<List<GameMetadata>>(g =>
                      g.Count == 1 &&
                      g[0].IgdbId == 999)), Times.Once());
        }

        [Test]
        public void should_not_delete_metadata_still_in_remote_collection()
        {
            var existingMetadata = new GameMetadata
            {
                IgdbId = 201,
                Title = "Game One",
                CollectionIgdbId = _collection.IgdbId
            };

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(new List<GameMetadata> { existingMetadata });

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.DeleteMany(It.Is<List<GameMetadata>>(g => g.Count == 0)), Times.Once());
        }

        [Test]
        public void should_refresh_all_collections_when_no_ids_specified()
        {
            var secondCollection = new GameCollection
            {
                Id = 2,
                IgdbId = 200,
                Title = "Second Collection",
                SortTitle = "second collection",
                Monitored = false,
                LastInfoSync = DateTime.UtcNow.AddDays(-20)
            };

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.GetAllCollections())
                  .Returns(new List<GameCollection> { _collection, secondCollection });

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.GetCollection(2))
                  .Returns(secondCollection);

            var secondCollectionInfo = new GameCollection
            {
                IgdbId = 200,
                Title = "Second Collection Updated",
                CleanTitle = "secondcollectionupdated",
                SortTitle = "second collection updated",
                Overview = "Second overview",
                Images = new List<MediaCover.MediaCover>(),
                Games = new List<GameMetadata>()
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(200))
                  .Returns(secondCollectionInfo);

            Subject.Execute(CreateCommand());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(100), Times.Once());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(200), Times.Once());
        }

        [Test]
        public void should_publish_collection_refresh_complete_event()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<NzbDrone.Core.Games.Events.CollectionRefreshCompleteEvent>()), Times.Once());
        }

        [Test]
        public void should_publish_refresh_complete_event_after_batch_refresh()
        {
            Subject.Execute(CreateCommand());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<NzbDrone.Core.Games.Events.CollectionRefreshCompleteEvent>()), Times.Once());
        }

        [Test]
        public void should_continue_refreshing_when_one_collection_throws_game_not_found()
        {
            var secondCollection = new GameCollection
            {
                Id = 2,
                IgdbId = 200,
                Title = "Second Collection",
                SortTitle = "second collection",
                Monitored = true,
                LastInfoSync = DateTime.UtcNow.AddDays(-20)
            };

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.GetAllCollections())
                  .Returns(new List<GameCollection> { _collection, secondCollection });

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.GetCollection(2))
                  .Returns(secondCollection);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(100))
                  .Throws(new GameNotFoundException(100));

            var secondCollectionInfo = new GameCollection
            {
                IgdbId = 200,
                Title = "Second Collection",
                CleanTitle = "secondcollection",
                SortTitle = "second collection",
                Overview = "Overview",
                Images = new List<MediaCover.MediaCover>(),
                Games = new List<GameMetadata>()
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(200))
                  .Returns(secondCollectionInfo);

            Subject.Execute(CreateCommand());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(200), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<NzbDrone.Core.Games.Events.CollectionRefreshCompleteEvent>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_refresh_collection_updated_less_than_six_hours_ago()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddHours(-3);

            var command = new RefreshCollectionsCommand
            {
                Trigger = CommandTrigger.Scheduled
            };

            Subject.Execute(command);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_refresh_collection_updated_more_than_fifteen_days_ago()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddDays(-20);

            Subject.Execute(CreateCommand());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(_collection.IgdbId), Times.Once());
        }

        [Test]
        public void should_refresh_collection_when_last_info_sync_is_null()
        {
            _collection.LastInfoSync = null;

            Subject.Execute(CreateCommand());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(_collection.IgdbId), Times.Once());
        }

        [Test]
        public void should_always_refresh_when_trigger_is_manual()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddHours(-1);

            Subject.Execute(CreateCommand());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(_collection.IgdbId), Times.Once());
        }

        [Test]
        public void should_not_refresh_when_trigger_is_scheduled_and_recently_updated()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddHours(-3);

            var command = new RefreshCollectionsCommand
            {
                Trigger = CommandTrigger.Scheduled
            };

            Subject.Execute(command);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_still_sync_games_when_skipping_refresh()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddHours(-3);

            // Put games in metadata so SyncCollectionGames can find them
            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(_collectionGames);

            var command = new RefreshCollectionsCommand
            {
                Trigger = CommandTrigger.Scheduled
            };

            Subject.Execute(command);

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g => g.Count == 2), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_handle_collection_with_no_games_from_provider()
        {
            _collectionInfo.Games = new List<GameMetadata>();
            _collectionGames.Clear();

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.UpsertMany(It.Is<List<GameMetadata>>(g => g.Count == 0)), Times.Once());

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.IsAny<List<Game>>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_filter_both_existing_and_excluded_games()
        {
            _collectionGames.Add(new GameMetadata
            {
                IgdbId = 203,
                Title = "Game Three",
                Status = GameStatusType.Released
            });

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AllGameIgdbIds())
                  .Returns(new List<int> { 201 });

            Mocker.GetMock<IImportListExclusionService>()
                  .Setup(s => s.All())
                  .Returns(new List<ImportListExclusion>
                  {
                      new ImportListExclusion { IgdbId = 202, GameTitle = "Game Two" }
                  });

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.Count == 1 &&
                      g[0].IgdbId == 203), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_refresh_multiple_collections_by_id()
        {
            var secondCollection = new GameCollection
            {
                Id = 2,
                IgdbId = 200,
                Title = "Second Collection",
                SortTitle = "second collection",
                Monitored = true
            };

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.GetCollection(2))
                  .Returns(secondCollection);

            var secondCollectionInfo = new GameCollection
            {
                IgdbId = 200,
                Title = "Second Collection",
                CleanTitle = "secondcollection",
                SortTitle = "second collection",
                Overview = "Overview",
                Images = new List<MediaCover.MediaCover>(),
                Games = new List<GameMetadata>
                {
                    new GameMetadata { IgdbId = 301, Title = "Game Three", Status = GameStatusType.Released }
                }
            };

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(200))
                  .Returns(secondCollectionInfo);

            Subject.Execute(CreateCommand(new List<int> { _collection.Id, 2 }));

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(100), Times.Once());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetCollectionInfo(200), Times.Once());
        }

        [Test]
        public void should_set_minimum_availability_on_added_games()
        {
            _collection.MinimumAvailability = GameStatusType.EarlyAccess;

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.All(x => x.MinimumAvailability == GameStatusType.EarlyAccess)), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_filter_deleted_games_from_sync()
        {
            _collectionGames.Add(new GameMetadata
            {
                IgdbId = 206,
                Title = "Deleted Game",
                Status = GameStatusType.Deleted
            });

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.GetGamesByCollectionIgdbId(_collection.IgdbId))
                  .Returns(() => new List<GameMetadata>(_collectionGames));

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.Is<List<Game>>(g =>
                      g.All(x => x.IgdbId != 206)), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_update_images_on_collection()
        {
            var images = new List<MediaCover.MediaCover>
            {
                new MediaCover.MediaCover { Url = "http://example.com/image.jpg" }
            };

            _collectionInfo.Images = images;

            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(v => v.UpdateCollection(It.Is<GameCollection>(c =>
                      c.Images.Count == 1 &&
                      c.Images[0].Url == "http://example.com/image.jpg")), Times.Once());
        }

        [Test]
        public void should_pass_ignore_errors_true_when_adding_games()
        {
            Subject.Execute(CreateCommand(new List<int> { _collection.Id }));

            Mocker.GetMock<IAddGameService>()
                  .Verify(v => v.AddGames(It.IsAny<List<Game>>(), true), Times.Once());
        }

        [Test]
        public void should_return_true_from_should_refresh_when_never_synced()
        {
            _collection.LastInfoSync = null;

            Subject.ShouldRefresh(_collection).Should().BeTrue();
        }

        [Test]
        public void should_return_true_from_should_refresh_when_synced_over_fifteen_days_ago()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddDays(-16);

            Subject.ShouldRefresh(_collection).Should().BeTrue();
        }

        [Test]
        public void should_return_false_from_should_refresh_when_synced_less_than_six_hours_ago()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddHours(-3);

            Subject.ShouldRefresh(_collection).Should().BeFalse();
        }

        [Test]
        public void should_return_false_from_should_refresh_when_synced_between_six_hours_and_fifteen_days()
        {
            _collection.LastInfoSync = DateTime.UtcNow.AddDays(-5);

            Subject.ShouldRefresh(_collection).Should().BeFalse();
        }
    }
}
