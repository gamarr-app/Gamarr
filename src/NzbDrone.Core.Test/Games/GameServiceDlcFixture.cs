using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games
{
    [TestFixture]
    public class GameServiceDlcFixture : CoreTest<GameService>
    {
        private Game _parentGame;
        private Game _dlcGame1;
        private Game _dlcGame2;
        private Game _mainGame;
        private Game _remakeGame;

        [SetUp]
        public void Setup()
        {
            _parentGame = new Game
            {
                Id = 1,
                GameMetadataId = 1,
                GameMetadata = new GameMetadata
                {
                    Id = 1,
                    IgdbId = 100,
                    SteamAppId = 1000,
                    Title = "Parent Game",
                    GameType = GameType.MainGame,
                    ParentGameId = null
                },
                Tags = new HashSet<int>()
            };

            _dlcGame1 = new Game
            {
                Id = 2,
                GameMetadataId = 2,
                GameMetadata = new GameMetadata
                {
                    Id = 2,
                    IgdbId = 200,
                    SteamAppId = 2000,
                    Title = "DLC Pack 1",
                    GameType = GameType.DlcAddon,
                    ParentGameId = 100
                },
                Tags = new HashSet<int>()
            };

            _dlcGame2 = new Game
            {
                Id = 3,
                GameMetadataId = 3,
                GameMetadata = new GameMetadata
                {
                    Id = 3,
                    IgdbId = 201,
                    SteamAppId = 2001,
                    Title = "Expansion Pack",
                    GameType = GameType.Expansion,
                    ParentGameId = 100
                },
                Tags = new HashSet<int>()
            };

            _mainGame = new Game
            {
                Id = 4,
                GameMetadataId = 4,
                GameMetadata = new GameMetadata
                {
                    Id = 4,
                    IgdbId = 300,
                    SteamAppId = 3000,
                    Title = "Another Main Game",
                    GameType = GameType.MainGame,
                    ParentGameId = null
                },
                Tags = new HashSet<int>()
            };

            _remakeGame = new Game
            {
                Id = 5,
                GameMetadataId = 5,
                GameMetadata = new GameMetadata
                {
                    Id = 5,
                    IgdbId = 400,
                    SteamAppId = 4000,
                    Title = "Remake Game",
                    GameType = GameType.Remake,
                    ParentGameId = null
                },
                Tags = new HashSet<int>()
            };
        }

        [Test]
        public void should_return_dlcs_with_matching_parent_game_id()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GetDlcsForGame(100))
                  .Returns(new List<Game> { _dlcGame1, _dlcGame2 });

            var result = Subject.GetDlcsForGame(100);

            result.Should().HaveCount(2);
            result.Should().Contain(g => g.GameMetadata.Value.Title == "DLC Pack 1");
            result.Should().Contain(g => g.GameMetadata.Value.Title == "Expansion Pack");
        }

        [Test]
        public void should_return_empty_when_no_dlcs_exist()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GetDlcsForGame(999))
                  .Returns(new List<Game>());

            var result = Subject.GetDlcsForGame(999);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_filter_out_dlc_games_for_main_games_only()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GetMainGamesOnly())
                  .Returns(new List<Game> { _parentGame, _mainGame, _remakeGame });

            var result = Subject.GetMainGamesOnly();

            result.Should().HaveCount(3);
            result.Should().NotContain(g => g.GameMetadata.Value.GameType == GameType.DlcAddon);
            result.Should().NotContain(g => g.GameMetadata.Value.GameType == GameType.Expansion);
        }

        [Test]
        public void should_include_games_with_no_parent_in_main_games()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GetMainGamesOnly())
                  .Returns(new List<Game> { _parentGame, _mainGame });

            var result = Subject.GetMainGamesOnly();

            result.Should().OnlyContain(g => g.GameMetadata.Value.ParentGameId == null);
        }

        [Test]
        public void should_return_all_steam_app_ids()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.AllGameSteamAppIds())
                  .Returns(new List<int> { 1000, 2000, 2001, 3000, 4000 });

            var result = Subject.AllGameSteamAppIds();

            result.Should().HaveCount(5);
            result.Should().Contain(1000);
            result.Should().Contain(3000);
        }

        [Test]
        public void should_find_game_by_igdb_id()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.FindByIgdbId(100))
                  .Returns(_parentGame);

            var result = Subject.FindByIgdbId(100);

            result.Should().NotBeNull();
            result.GameMetadata.Value.Title.Should().Be("Parent Game");
        }

        [Test]
        public void should_return_null_when_igdb_id_not_found()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.FindByIgdbId(999))
                  .Returns((Game)null);

            var result = Subject.FindByIgdbId(999);

            result.Should().BeNull();
        }

        [Test]
        public void should_find_game_by_steam_app_id()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.FindBySteamAppId(1000))
                  .Returns(_parentGame);

            var result = Subject.FindBySteamAppId(1000);

            result.Should().NotBeNull();
            result.GameMetadata.Value.SteamAppId.Should().Be(1000);
        }

        [Test]
        public void should_return_null_when_steam_app_id_not_found()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.FindBySteamAppId(99999))
                  .Returns((Game)null);

            var result = Subject.FindBySteamAppId(99999);

            result.Should().BeNull();
        }

        [Test]
        public void should_return_true_when_tags_changed()
        {
            var game = new Game
            {
                Id = 1,
                GameMetadata = new GameMetadata { Title = "Test Game" },
                Tags = new HashSet<int> { 1, 2 }
            };

            var changes = new AutoTaggingChanges
            {
                TagsToAdd = new HashSet<int> { 3 },
                TagsToRemove = new HashSet<int>()
            };

            Mocker.GetMock<IAutoTaggingService>()
                  .Setup(s => s.GetTagChanges(game))
                  .Returns(changes);

            var result = Subject.UpdateTags(game);

            result.Should().BeTrue();
            game.Tags.Should().Contain(3);
        }

        [Test]
        public void should_return_false_when_no_tags_changed()
        {
            var game = new Game
            {
                Id = 1,
                GameMetadata = new GameMetadata { Title = "Test Game" },
                Tags = new HashSet<int> { 1, 2 }
            };

            var changes = new AutoTaggingChanges
            {
                TagsToAdd = new HashSet<int>(),
                TagsToRemove = new HashSet<int>()
            };

            Mocker.GetMock<IAutoTaggingService>()
                  .Setup(s => s.GetTagChanges(game))
                  .Returns(changes);

            var result = Subject.UpdateTags(game);

            result.Should().BeFalse();
        }

        [Test]
        public void should_remove_tags_when_auto_tagging_says_to_remove()
        {
            var game = new Game
            {
                Id = 1,
                GameMetadata = new GameMetadata { Title = "Test Game" },
                Tags = new HashSet<int> { 1, 2, 3 }
            };

            var changes = new AutoTaggingChanges
            {
                TagsToAdd = new HashSet<int>(),
                TagsToRemove = new HashSet<int> { 2 }
            };

            Mocker.GetMock<IAutoTaggingService>()
                  .Setup(s => s.GetTagChanges(game))
                  .Returns(changes);

            var result = Subject.UpdateTags(game);

            result.Should().BeTrue();
            game.Tags.Should().NotContain(2);
            game.Tags.Should().Contain(1);
            game.Tags.Should().Contain(3);
        }

        [Test]
        public void should_not_duplicate_tag_when_already_present()
        {
            var game = new Game
            {
                Id = 1,
                GameMetadata = new GameMetadata { Title = "Test Game" },
                Tags = new HashSet<int> { 1, 2 }
            };

            var changes = new AutoTaggingChanges
            {
                TagsToAdd = new HashSet<int> { 2 },
                TagsToRemove = new HashSet<int>()
            };

            Mocker.GetMock<IAutoTaggingService>()
                  .Setup(s => s.GetTagChanges(game))
                  .Returns(changes);

            var result = Subject.UpdateTags(game);

            result.Should().BeFalse();
            game.Tags.Should().HaveCount(2);
        }

        [Test]
        public void should_return_all_dlcs()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GetAllDlcs())
                  .Returns(new List<Game> { _dlcGame1, _dlcGame2 });

            var result = Subject.GetAllDlcs();

            result.Should().HaveCount(2);
            result.Should().OnlyContain(g => g.GameMetadata.Value.GameType.IsDlc());
        }

        [Test]
        public void should_return_parent_game_by_igdb_id()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GetParentGame(100))
                  .Returns(_parentGame);

            var result = Subject.GetParentGame(100);

            result.Should().NotBeNull();
            result.GameMetadata.Value.IgdbId.Should().Be(100);
        }

        [Test]
        public void should_return_null_when_parent_game_not_found()
        {
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.GetParentGame(999))
                  .Returns((Game)null);

            var result = Subject.GetParentGame(999);

            result.Should().BeNull();
        }
    }
}
