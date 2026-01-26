using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class RefreshGameServiceDlcFixture : CoreTest<RefreshGameService>
    {
        private Game _game;
        private GameMetadata _gameMetadata;
        private GameMetadata _updatedGameInfo;

        [SetUp]
        public void Setup()
        {
            _gameMetadata = Builder<GameMetadata>.CreateNew()
                .With(m => m.Id = 1)
                .With(m => m.IgdbId = 100)
                .With(m => m.Title = "Test Game")
                .With(m => m.Status = GameStatusType.Released)
                .With(m => m.ParentGameId = null)
                .With(m => m.IgdbDlcIds = new List<int>())
                .With(m => m.SteamDlcIds = new List<int>())
                .With(m => m.AlternativeTitles = new List<AlternativeTitle>())
                .With(m => m.Translations = new List<GameTranslation>())
                .With(m => m.CollectionIgdbId = 0)
                .Build();

            _game = Builder<Game>.CreateNew()
                .With(g => g.Id = 1)
                .With(g => g.GameMetadataId = 1)
                .With(g => g.GameMetadata = new LazyLoaded<GameMetadata>(_gameMetadata))
                .With(g => g.Path = @"/games/test-game")
                .With(g => g.QualityProfileId = 1)
                .With(g => g.Tags = new HashSet<int>())
                .Build();

            _updatedGameInfo = Builder<GameMetadata>.CreateNew()
                .With(m => m.IgdbId = 100)
                .With(m => m.Title = "Test Game")
                .With(m => m.Status = GameStatusType.Released)
                .With(m => m.ParentGameId = null)
                .With(m => m.IgdbDlcIds = new List<int>())
                .With(m => m.SteamDlcIds = new List<int>())
                .With(m => m.AlternativeTitles = new List<AlternativeTitle>())
                .With(m => m.Translations = new List<GameTranslation>())
                .With(m => m.CollectionIgdbId = 0)
                .With(m => m.CollectionTitle = null)
                .Build();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(It.IsAny<int>()))
                  .Returns(_game);

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.Get(It.IsAny<int>()))
                  .Returns(_gameMetadata);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoByIgdbId(It.IsAny<int>()))
                  .Returns(_updatedGameInfo);

            Mocker.GetMock<IAlternativeTitleService>()
                  .Setup(s => s.UpdateTitles(It.IsAny<List<AlternativeTitle>>(), It.IsAny<GameMetadata>()))
                  .Returns(new List<AlternativeTitle>());

            Mocker.GetMock<IGameTranslationService>()
                  .Setup(s => s.UpdateTranslations(It.IsAny<List<GameTranslation>>(), It.IsAny<GameMetadata>()));

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.Never);

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.UpdateTags(It.IsAny<Game>()))
                  .Returns(false);
        }

        private RefreshGameCommand CreateCommand(int gameId)
        {
            return new RefreshGameCommand(new List<int> { gameId })
            {
                Trigger = CommandTrigger.Manual
            };
        }

        [Test]
        public void should_set_parent_game_id_when_metadata_has_parent()
        {
            _updatedGameInfo.ParentGameId = 50;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.ParentGameId == 50)), Times.Once());
        }

        [Test]
        public void should_not_overwrite_parent_game_id_when_metadata_has_zero()
        {
            _gameMetadata.ParentGameId = 25;
            _updatedGameInfo.ParentGameId = 0;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.ParentGameId == 25)), Times.Once());
        }

        [Test]
        public void should_update_parent_game_id_when_new_value_is_positive()
        {
            _gameMetadata.ParentGameId = 25;
            _updatedGameInfo.ParentGameId = 75;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.ParentGameId == 75)), Times.Once());
        }

        [Test]
        public void should_set_igdb_dlc_ids_when_metadata_has_dlcs()
        {
            _updatedGameInfo.IgdbDlcIds = new List<int> { 200, 201, 202 };

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m =>
                      m.IgdbDlcIds.Count == 3 &&
                      m.IgdbDlcIds.Contains(200) &&
                      m.IgdbDlcIds.Contains(201) &&
                      m.IgdbDlcIds.Contains(202))), Times.Once());
        }

        [Test]
        public void should_not_overwrite_igdb_dlc_ids_when_metadata_returns_null()
        {
            _gameMetadata.IgdbDlcIds = new List<int> { 300, 301 };
            _updatedGameInfo.IgdbDlcIds = null;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m =>
                      m.IgdbDlcIds.Count == 2 &&
                      m.IgdbDlcIds.Contains(300))), Times.Once());
        }

        [Test]
        public void should_not_overwrite_igdb_dlc_ids_when_metadata_returns_empty_list()
        {
            _gameMetadata.IgdbDlcIds = new List<int> { 400, 401 };
            _updatedGameInfo.IgdbDlcIds = new List<int>();

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m =>
                      m.IgdbDlcIds.Count == 2 &&
                      m.IgdbDlcIds.Contains(400))), Times.Once());
        }

        [Test]
        public void should_update_igdb_dlc_ids_when_new_list_has_entries()
        {
            _gameMetadata.IgdbDlcIds = new List<int> { 300, 301 };
            _updatedGameInfo.IgdbDlcIds = new List<int> { 500, 501, 502 };

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m =>
                      m.IgdbDlcIds.Count == 3 &&
                      m.IgdbDlcIds.Contains(500) &&
                      m.IgdbDlcIds.Contains(501) &&
                      m.IgdbDlcIds.Contains(502))), Times.Once());
        }

        [Test]
        public void should_set_steam_dlc_ids_when_metadata_has_steam_dlcs()
        {
            _updatedGameInfo.SteamDlcIds = new List<int> { 100200, 100201, 100202 };

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m =>
                      m.SteamDlcIds.Count == 3 &&
                      m.SteamDlcIds.Contains(100200) &&
                      m.SteamDlcIds.Contains(100201) &&
                      m.SteamDlcIds.Contains(100202))), Times.Once());
        }

        [Test]
        public void should_mark_game_as_deleted_when_game_not_found_exception_thrown()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoByIgdbId(It.IsAny<int>()))
                  .Throws(new GameNotFoundException(100));

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.Status == GameStatusType.Deleted)), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_publish_game_updated_event_when_game_not_found()
        {
            _gameMetadata.Status = GameStatusType.Released;

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoByIgdbId(It.IsAny<int>()))
                  .Throws(new GameNotFoundException(100));

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<NzbDrone.Core.Games.Events.GameUpdatedEvent>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_mark_as_deleted_when_already_deleted()
        {
            _gameMetadata.Status = GameStatusType.Deleted;

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoByIgdbId(It.IsAny<int>()))
                  .Throws(new GameNotFoundException(100));

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.IsAny<GameMetadata>()), Times.Never());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_use_steam_app_id_lookup_when_igdb_id_is_zero()
        {
            _game = Builder<Game>.CreateNew()
                .With(g => g.Id = 2)
                .With(g => g.GameMetadataId = 2)
                .With(g => g.GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                {
                    Id = 2,
                    IgdbId = 0,
                    SteamAppId = 12345,
                    Title = "Steam Only Game",
                    Status = GameStatusType.Released,
                    IgdbDlcIds = new List<int>(),
                    SteamDlcIds = new List<int>(),
                    AlternativeTitles = new List<AlternativeTitle>(),
                    Translations = new List<GameTranslation>(),
                    CollectionIgdbId = 0
                }))
                .With(g => g.Path = @"/games/steam-game")
                .With(g => g.Tags = new HashSet<int>())
                .Build();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(2))
                  .Returns(_game);

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.Get(2))
                  .Returns(_game.GameMetadata.Value);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoBySteamAppId(12345))
                  .Returns(_updatedGameInfo);

            Subject.Execute(new RefreshGameCommand(new List<int> { 2 }) { Trigger = CommandTrigger.Manual });

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameInfoBySteamAppId(12345), Times.Once());

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameInfoByIgdbId(It.IsAny<int>()), Times.Never());

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void should_return_without_update_when_steam_lookup_returns_null()
        {
            _game = Builder<Game>.CreateNew()
                .With(g => g.Id = 3)
                .With(g => g.GameMetadataId = 3)
                .With(g => g.GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                {
                    Id = 3,
                    IgdbId = 0,
                    SteamAppId = 99999,
                    Title = "Missing Steam Game",
                    Status = GameStatusType.Released,
                    IgdbDlcIds = new List<int>(),
                    SteamDlcIds = new List<int>(),
                    AlternativeTitles = new List<AlternativeTitle>(),
                    Translations = new List<GameTranslation>(),
                    CollectionIgdbId = 0
                }))
                .With(g => g.Path = @"/games/missing-game")
                .With(g => g.Tags = new HashSet<int>())
                .Build();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(3))
                  .Returns(_game);

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.Get(3))
                  .Returns(_game.GameMetadata.Value);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfoBySteamAppId(99999))
                  .Returns((GameMetadata)null);

            Subject.Execute(new RefreshGameCommand(new List<int> { 3 }) { Trigger = CommandTrigger.Manual });

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.IsAny<GameMetadata>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_update_collection_info_when_metadata_has_collection()
        {
            _updatedGameInfo.CollectionIgdbId = 500;
            _updatedGameInfo.CollectionTitle = "Test Collection";

            var newCollection = new GameCollection
            {
                Id = 10,
                IgdbId = 500,
                Title = "Test Collection"
            };

            Mocker.GetMock<IAddGameCollectionService>()
                  .Setup(s => s.AddGameCollection(It.IsAny<GameCollection>()))
                  .Returns(newCollection);

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), It.IsAny<List<RootFolder>>()))
                  .Returns(@"/games");

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m =>
                      m.CollectionIgdbId == 500 &&
                      m.CollectionTitle == "Test Collection")), Times.Once());
        }

        [Test]
        public void should_clear_collection_when_metadata_has_no_collection()
        {
            _gameMetadata.CollectionIgdbId = 500;
            _gameMetadata.CollectionTitle = "Old Collection";
            _updatedGameInfo.CollectionIgdbId = 0;
            _updatedGameInfo.CollectionTitle = null;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m =>
                      m.CollectionIgdbId == 0 &&
                      m.CollectionTitle == null)), Times.Once());
        }

        [Test]
        public void should_publish_game_updated_event_after_refresh()
        {
            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<NzbDrone.Core.Games.Events.GameUpdatedEvent>()), Times.Once());
        }

        [Test]
        public void should_update_igdb_id_when_game_was_replaced()
        {
            _updatedGameInfo.IgdbId = 999;
            _updatedGameInfo.Title = "Replacement Game";

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.IgdbId == 999)), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
