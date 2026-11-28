using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.GameRepositoryTests
{
    [TestFixture]

    public class GameRepositoryFixture : DbTest<GameRepository, Game>
    {
        private IQualityProfileRepository _profileRepository;
        private IGameMetadataRepository _metadataRepository;

        [SetUp]
        public void Setup()
        {
            _profileRepository = Mocker.Resolve<QualityProfileRepository>();
            Mocker.SetConstant<IQualityProfileRepository>(_profileRepository);

            _metadataRepository = Mocker.Resolve<GameMetadataRepository>();

            Mocker.GetMock<ICustomFormatService>()
                .Setup(x => x.All())
                .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_load_quality_profile()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.GOG, Quality.Scene, Quality.Uplay),
                FormatItems = CustomFormatsTestHelpers.GetDefaultFormatItems(),
                MinFormatScore = 0,
                Cutoff = Quality.GOG.Id,
                Name = "TestProfile"
            };

            _profileRepository.Insert(profile);

            var game = Builder<Game>.CreateNew().BuildNew();
            game.QualityProfileId = profile.Id;

            Subject.Insert(game);

            Subject.All().Single().QualityProfile.Should().NotBeNull();
        }

        private QualityProfile CreateTestProfile()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.GOG, Quality.Scene, Quality.Uplay),
                FormatItems = CustomFormatsTestHelpers.GetDefaultFormatItems(),
                MinFormatScore = 0,
                Cutoff = Quality.GOG.Id,
                Name = "TestProfile"
            };

            return _profileRepository.Insert(profile);
        }

        [Test]
        public void should_get_dlcs_for_parent_game()
        {
            var profile = CreateTestProfile();

            // Create parent game
            var parentMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 1000, Title = "Parent Game", GameType = GameType.MainGame });
            var parentGame = Builder<Game>.CreateNew().BuildNew();
            parentGame.QualityProfileId = profile.Id;
            parentGame.GameMetadataId = parentMeta.Id;
            Subject.Insert(parentGame);

            // Create DLC game
            var dlcMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 2000, Title = "DLC Game", GameType = GameType.DlcAddon, ParentGameId = 1000 });
            var dlcGame = Builder<Game>.CreateNew().BuildNew();
            dlcGame.QualityProfileId = profile.Id;
            dlcGame.GameMetadataId = dlcMeta.Id;
            Subject.Insert(dlcGame);

            // Create unrelated game
            var otherMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 3000, Title = "Other Game", GameType = GameType.MainGame });
            var otherGame = Builder<Game>.CreateNew().BuildNew();
            otherGame.QualityProfileId = profile.Id;
            otherGame.GameMetadataId = otherMeta.Id;
            Subject.Insert(otherGame);

            var dlcs = Subject.GetDlcsForGame(1000);

            dlcs.Should().HaveCount(1);
            dlcs.First().GameMetadata.Value.Title.Should().Be("DLC Game");
        }

        [Test]
        public void should_get_parent_game_by_igdb_id()
        {
            var profile = CreateTestProfile();

            var parentMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 1000, Title = "Parent Game", GameType = GameType.MainGame });
            var parentGame = Builder<Game>.CreateNew().BuildNew();
            parentGame.QualityProfileId = profile.Id;
            parentGame.GameMetadataId = parentMeta.Id;
            Subject.Insert(parentGame);

            var result = Subject.GetParentGame(1000);

            result.Should().NotBeNull();
            result.GameMetadata.Value.Title.Should().Be("Parent Game");
        }

        [Test]
        public void should_get_all_dlcs()
        {
            var profile = CreateTestProfile();

            // Create main game
            var mainMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 1000, Title = "Main Game", GameType = GameType.MainGame });
            var mainGame = Builder<Game>.CreateNew().BuildNew();
            mainGame.QualityProfileId = profile.Id;
            mainGame.GameMetadataId = mainMeta.Id;
            Subject.Insert(mainGame);

            // Create DLC
            var dlcMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 2000, Title = "DLC Game", GameType = GameType.DlcAddon });
            var dlcGame = Builder<Game>.CreateNew().BuildNew();
            dlcGame.QualityProfileId = profile.Id;
            dlcGame.GameMetadataId = dlcMeta.Id;
            Subject.Insert(dlcGame);

            // Create Expansion
            var expMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 3000, Title = "Expansion Game", GameType = GameType.Expansion });
            var expansionGame = Builder<Game>.CreateNew().BuildNew();
            expansionGame.QualityProfileId = profile.Id;
            expansionGame.GameMetadataId = expMeta.Id;
            Subject.Insert(expansionGame);

            var dlcs = Subject.GetAllDlcs();

            dlcs.Should().HaveCount(2);
            dlcs.Select(d => d.GameMetadata.Value.Title).Should().Contain("DLC Game", "Expansion Game");
        }

        [Test]
        public void should_get_main_games_only()
        {
            var profile = CreateTestProfile();

            // Create main game
            var mainMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 1000, Title = "Main Game", GameType = GameType.MainGame });
            var mainGame = Builder<Game>.CreateNew().BuildNew();
            mainGame.QualityProfileId = profile.Id;
            mainGame.GameMetadataId = mainMeta.Id;
            Subject.Insert(mainGame);

            // Create DLC
            var dlcMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 2000, Title = "DLC Game", GameType = GameType.DlcAddon });
            var dlcGame = Builder<Game>.CreateNew().BuildNew();
            dlcGame.QualityProfileId = profile.Id;
            dlcGame.GameMetadataId = dlcMeta.Id;
            Subject.Insert(dlcGame);

            // Create Remaster (counts as main game)
            var remMeta = _metadataRepository.Insert(new GameMetadata { IgdbId = 3000, Title = "Remaster Game", GameType = GameType.Remaster });
            var remasterGame = Builder<Game>.CreateNew().BuildNew();
            remasterGame.QualityProfileId = profile.Id;
            remasterGame.GameMetadataId = remMeta.Id;
            Subject.Insert(remasterGame);

            var mainGames = Subject.GetMainGamesOnly();

            mainGames.Should().HaveCount(2);
            mainGames.Select(g => g.GameMetadata.Value.Title).Should().Contain("Main Game", "Remaster Game");
            mainGames.Select(g => g.GameMetadata.Value.Title).Should().NotContain("DLC Game");
        }

        private int _igdb = 5000;

        // Inserts a game (with the given file rows) whose primary pointer is the
        // file at pointerIndex, or unset when pointerIndex is -1.
        private Game AddGameWithFiles(QualityProfile profile, int pointerIndex, params (string Path, Quality Quality)[] files)
        {
            var meta = _metadataRepository.Insert(new GameMetadata { IgdbId = _igdb++, Title = "G" + _igdb, CleanTitle = "g" + _igdb, Year = 2020, GameType = GameType.MainGame });
            var game = Builder<Game>.CreateNew().BuildNew();
            game.QualityProfileId = profile.Id;
            game.GameMetadataId = meta.Id;
            game.GameFileId = 0;
            Subject.Insert(game);

            var fileRepo = Mocker.Resolve<MediaFileRepository>();
            var ids = new List<int>();

            foreach (var (path, quality) in files)
            {
                var file = fileRepo.Insert(new GameFile
                {
                    GameId = game.Id,
                    RelativePath = path,
                    Path = "/g/" + path,
                    Quality = new QualityModel(quality),
                    DateAdded = System.DateTime.UtcNow,
                    Languages = new List<NzbDrone.Core.Languages.Language>()
                });
                ids.Add(file.Id);
            }

            if (pointerIndex >= 0)
            {
                game.GameFileId = ids[pointerIndex];
                Subject.Update(game);
            }

            return game;
        }

        private static PagingSpec<Game> Paging() =>
            new PagingSpec<Game> { Page = 1, PageSize = 50, SortKey = "Id", SortDirection = SortDirection.Ascending };

        [Test]
        public void should_resolve_game_file_through_primary_pointer_on_every_path()
        {
            var profile = CreateTestProfile();
            var game = AddGameWithFiles(profile, 1, ("Game.exe", Quality.GOG), ("Updates/v2/up.exe", Quality.Scene), ("DLC/Extra/dlc.exe", Quality.Uplay));

            var viaAll = Subject.All().Single(g => g.Id == game.Id);
            var viaGet = Subject.Get(game.Id);
            var viaTitle = Subject.FindByTitles(new List<string> { "g" + _igdb });

            viaAll.GameFile.RelativePath.Should().Be("Updates/v2/up.exe");
            viaGet.GameFile.RelativePath.Should().Be("Updates/v2/up.exe");
            viaTitle.Should().ContainSingle().Which.GameFile.RelativePath.Should().Be("Updates/v2/up.exe");
        }

        [Test]
        public void missing_should_include_game_with_only_component_files_and_pointer_on_update()
        {
            var profile = CreateTestProfile();
            var game = AddGameWithFiles(profile, 0, ("Updates/v2/up.exe", Quality.Scene), ("DLC/Extra/dlc.exe", Quality.Uplay));

            Subject.GamesWithoutFiles(Paging()).Records.Select(g => g.Id).Should().Contain(game.Id);
        }

        [Test]
        public void missing_should_exclude_game_with_base_file_even_when_pointer_unset()
        {
            var profile = CreateTestProfile();
            var game = AddGameWithFiles(profile, -1, ("Game.exe", Quality.GOG), ("Updates/v2/up.exe", Quality.Scene));

            Subject.GamesWithoutFiles(Paging()).Records.Select(g => g.Id).Should().NotContain(game.Id);
        }

        [Test]
        public void missing_should_include_game_without_any_file()
        {
            var profile = CreateTestProfile();
            var game = AddGameWithFiles(profile, -1);

            Subject.GamesWithoutFiles(Paging()).Records.Select(g => g.Id).Should().Contain(game.Id);
        }

        [Test]
        public void cutoff_unmet_should_judge_base_file_not_primary_component()
        {
            var profile = CreateTestProfile();

            // Base meets cutoff, but the primary pointer sits on a low-quality update.
            var metBase = AddGameWithFiles(profile, 1, ("Game.exe", Quality.GOG), ("Updates/v2/up.exe", Quality.Uplay));

            // Base is below cutoff while the primary is a high-quality DLC.
            var unmetBase = AddGameWithFiles(profile, 1, ("Game.exe", Quality.Uplay), ("DLC/Extra/dlc.exe", Quality.GOG));

            var result = Subject.GamesWhereCutoffUnmet(Paging(), new List<QualitiesBelowCutoff> { new QualitiesBelowCutoff(profile.Id, new[] { Quality.Uplay.Id, Quality.Scene.Id }) });

            var ids = result.Records.Select(g => g.Id).ToList();
            ids.Should().Contain(unmetBase.Id);
            ids.Should().NotContain(metBase.Id);
            result.TotalRecords.Should().Be(1);
        }

        [Test]
        public void cutoff_unmet_should_ignore_game_without_base_file()
        {
            var profile = CreateTestProfile();
            var game = AddGameWithFiles(profile, 0, ("Updates/v2/up.exe", Quality.Uplay));

            var result = Subject.GamesWhereCutoffUnmet(Paging(), new List<QualitiesBelowCutoff> { new QualitiesBelowCutoff(profile.Id, new[] { Quality.Uplay.Id }) });

            result.Records.Select(g => g.Id).Should().NotContain(game.Id);
        }
    }
}
