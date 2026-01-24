using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
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
    }
}
