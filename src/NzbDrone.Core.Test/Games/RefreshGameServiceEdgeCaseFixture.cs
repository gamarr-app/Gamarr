using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class RefreshGameServiceEdgeCaseFixture : CoreTest<RefreshGameService>
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
                .With(m => m.Overview = "Original overview")
                .With(m => m.Year = 2020)
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
                .With(m => m.Overview = "Original overview")
                .With(m => m.Year = 2020)
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

        private RefreshGameCommand CreateCommand(int gameId, CommandTrigger trigger = CommandTrigger.Manual)
        {
            return new RefreshGameCommand(new List<int> { gameId })
            {
                Trigger = trigger
            };
        }

        [Test]
        public void should_update_alternative_titles_during_refresh()
        {
            var altTitles = new List<AlternativeTitle>
            {
                new AlternativeTitle("Game Alt Title 1") { GameMetadataId = 1 },
                new AlternativeTitle("Game Alt Title 2") { GameMetadataId = 1 }
            };

            _updatedGameInfo.AlternativeTitles = altTitles;

            Mocker.GetMock<IAlternativeTitleService>()
                  .Setup(s => s.UpdateTitles(It.IsAny<List<AlternativeTitle>>(), It.IsAny<GameMetadata>()))
                  .Returns(altTitles);

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IAlternativeTitleService>()
                  .Verify(v => v.UpdateTitles(
                      It.Is<List<AlternativeTitle>>(list => list.Count == 2),
                      It.IsAny<GameMetadata>()), Times.Once());
        }

        [Test]
        public void should_pass_empty_list_for_alternative_titles_when_null_in_metadata()
        {
            _updatedGameInfo.AlternativeTitles = null;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IAlternativeTitleService>()
                  .Verify(v => v.UpdateTitles(
                      It.Is<List<AlternativeTitle>>(list => list.Count == 0),
                      It.IsAny<GameMetadata>()), Times.Once());
        }

        [Test]
        public void should_update_translations_during_refresh()
        {
            var translations = new List<GameTranslation>
            {
                new GameTranslation { GameMetadataId = 1, Title = "Jeu Test", Language = Language.French },
                new GameTranslation { GameMetadataId = 1, Title = "Testspiel", Language = Language.German }
            };

            _updatedGameInfo.Translations = translations;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameTranslationService>()
                  .Verify(v => v.UpdateTranslations(
                      It.Is<List<GameTranslation>>(list => list.Count == 2),
                      It.IsAny<GameMetadata>()), Times.Once());
        }

        [Test]
        public void should_pass_empty_list_for_translations_when_null_in_metadata()
        {
            _updatedGameInfo.Translations = null;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameTranslationService>()
                  .Verify(v => v.UpdateTranslations(
                      It.Is<List<GameTranslation>>(list => list.Count == 0),
                      It.IsAny<GameMetadata>()), Times.Once());
        }

        [Test]
        public void should_trigger_rescan_when_rescan_after_refresh_is_always()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.Always);

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IDiskScanService>()
                  .Verify(v => v.Scan(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public void should_not_trigger_rescan_when_rescan_after_refresh_is_never()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.Never);

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IDiskScanService>()
                  .Verify(v => v.Scan(It.IsAny<Game>()), Times.Never());
        }

        [Test]
        public void should_publish_scan_skipped_event_when_rescan_after_refresh_is_never()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.Never);

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.Is<GameScanSkippedEvent>(
                      e => e.Reason == GameScanSkippedReason.NeverRescanAfterRefresh)), Times.Once());
        }

        [Test]
        public void should_trigger_rescan_when_rescan_after_refresh_is_after_manual_and_trigger_is_manual()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.AfterManual);

            Subject.Execute(CreateCommand(_game.Id, CommandTrigger.Manual));

            Mocker.GetMock<IDiskScanService>()
                  .Verify(v => v.Scan(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public void should_not_trigger_rescan_when_rescan_after_refresh_is_after_manual_and_trigger_is_scheduled()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.AfterManual);

            Subject.Execute(CreateCommand(_game.Id, CommandTrigger.Scheduled));

            Mocker.GetMock<IDiskScanService>()
                  .Verify(v => v.Scan(It.IsAny<Game>()), Times.Never());
        }

        [Test]
        public void should_publish_scan_skipped_event_when_rescan_after_manual_and_trigger_is_scheduled()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.AfterManual);

            Subject.Execute(CreateCommand(_game.Id, CommandTrigger.Scheduled));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.Is<GameScanSkippedEvent>(
                      e => e.Reason == GameScanSkippedReason.RescanAfterManualRefreshOnly)), Times.Once());
        }

        [Test]
        public void should_skip_refresh_when_should_refresh_game_returns_false_for_scheduled_trigger()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetAllGames())
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<ICheckIfGameShouldBeRefreshed>()
                  .Setup(s => s.ShouldRefresh(It.IsAny<GameMetadata>()))
                  .Returns(false);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetChangedGames(It.IsAny<System.DateTime>()))
                  .Returns(new HashSet<int>());

            var command = new RefreshGameCommand
            {
                Trigger = CommandTrigger.Scheduled
            };

            Subject.Execute(command);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameInfoByIgdbId(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_always_refresh_when_trigger_is_manual_regardless_of_should_refresh_game()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetAllGames())
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<ICheckIfGameShouldBeRefreshed>()
                  .Setup(s => s.ShouldRefresh(It.IsAny<GameMetadata>()))
                  .Returns(false);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetChangedGames(It.IsAny<System.DateTime>()))
                  .Returns(new HashSet<int>());

            var command = new RefreshGameCommand
            {
                Trigger = CommandTrigger.Manual
            };

            Subject.Execute(command);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameInfoByIgdbId(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void should_update_game_status_from_metadata()
        {
            _gameMetadata.Status = GameStatusType.Announced;
            _updatedGameInfo.Status = GameStatusType.Released;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.Status == GameStatusType.Released)), Times.Once());
        }

        [Test]
        public void should_update_overview_from_metadata()
        {
            _updatedGameInfo.Overview = "Updated overview text";

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.Overview == "Updated overview text")), Times.Once());
        }

        [Test]
        public void should_update_year_from_metadata()
        {
            _updatedGameInfo.Year = 2025;

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.Year == 2025)), Times.Once());
        }

        [Test]
        public void should_force_rescan_when_game_is_new()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.Never);

            var command = new RefreshGameCommand(new List<int> { _game.Id }, true)
            {
                Trigger = CommandTrigger.Manual
            };

            Subject.Execute(command);

            Mocker.GetMock<IDiskScanService>()
                  .Verify(v => v.Scan(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public void should_refresh_game_when_in_updated_igdb_games_set()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetAllGames())
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<ICheckIfGameShouldBeRefreshed>()
                  .Setup(s => s.ShouldRefresh(It.IsAny<GameMetadata>()))
                  .Returns(false);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetChangedGames(It.IsAny<System.DateTime>()))
                  .Returns(new HashSet<int> { 100 });

            var command = new RefreshGameCommand
            {
                Trigger = CommandTrigger.Scheduled,
                LastStartTime = System.DateTime.UtcNow.AddDays(-1)
            };

            Subject.Execute(command);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetGameInfoByIgdbId(100), Times.Once());
        }

        [Test]
        public void should_not_check_changed_games_when_last_start_time_is_older_than_14_days()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetAllGames())
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<ICheckIfGameShouldBeRefreshed>()
                  .Setup(s => s.ShouldRefresh(It.IsAny<GameMetadata>()))
                  .Returns(false);

            var command = new RefreshGameCommand
            {
                Trigger = CommandTrigger.Scheduled,
                LastStartTime = System.DateTime.UtcNow.AddDays(-15)
            };

            Subject.Execute(command);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(v => v.GetChangedGames(It.IsAny<System.DateTime>()), Times.Never());
        }

        [Test]
        public void should_publish_game_refresh_starting_event()
        {
            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<NzbDrone.Core.Games.Events.GameRefreshStartingEvent>()), Times.Once());
        }

        [Test]
        public void should_publish_game_refresh_complete_event()
        {
            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<NzbDrone.Core.Games.Events.GameRefreshCompleteEvent>()), Times.Once());
        }

        [Test]
        public void should_update_title_from_metadata()
        {
            _updatedGameInfo.Title = "New Game Title";

            Subject.Execute(CreateCommand(_game.Id));

            Mocker.GetMock<IGameMetadataService>()
                  .Verify(v => v.Upsert(It.Is<GameMetadata>(m => m.Title == "New Game Title")), Times.Once());
        }

        [Test]
        public void should_still_rescan_and_update_tags_when_should_refresh_returns_false()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetAllGames())
                  .Returns(new List<Game> { _game });

            Mocker.GetMock<ICheckIfGameShouldBeRefreshed>()
                  .Setup(s => s.ShouldRefresh(It.IsAny<GameMetadata>()))
                  .Returns(false);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetChangedGames(It.IsAny<System.DateTime>()))
                  .Returns(new HashSet<int>());

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.RescanAfterRefresh)
                  .Returns(RescanAfterRefreshType.Always);

            var command = new RefreshGameCommand
            {
                Trigger = CommandTrigger.Scheduled
            };

            Subject.Execute(command);

            Mocker.GetMock<IDiskScanService>()
                  .Verify(v => v.Scan(It.IsAny<Game>()), Times.Once());

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateTags(It.IsAny<Game>()), Times.Once());
        }
    }
}
