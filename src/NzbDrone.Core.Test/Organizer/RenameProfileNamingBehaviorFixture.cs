using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Organizer
{
    [TestFixture]
    public class RenameProfileNamingBehaviorFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;
            _namingConfig.RenameProfile = RenameProfile.Gamarr;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(x => x.GetConfig())
                  .Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                  .Setup(x => x.Get(It.IsAny<Quality>()))
                  .Returns<Quality>(quality => Quality.DefaultQualityDefinitions.Single(x => x.Quality == quality));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(x => x.All())
                  .Returns(new System.Collections.Generic.List<CustomFormat>());

            Mocker.GetMock<IGameTranslationService>()
                  .Setup(x => x.GetAllTranslationsForGameMetadata(It.IsAny<int>()))
                  .Returns(new System.Collections.Generic.List<GameTranslation>());
        }

        [Test]
        public void RenameProfile_should_preserve_existing_default_file_name_builder_output_for_normal_gamarr_profile()
        {
            var game = new Game
            {
                Title = "South Park",
                Year = 1998
            };

            var gameFile = new GameFile
            {
                Quality = new QualityModel(Quality.Uplay)
            };

            Subject.BuildFileName(game, gameFile)
                   .Should().Be("South Park (1998) Uplay");
        }

        [Test]
        public void RenameProfile_should_preserve_original_nointro_variant_filename_for_gamarr_profile()
        {
            var game = new Game
            {
                Id = 5,
                Title = "Mega Man IV",
                Year = 1993
            };

            var gameFile = new GameFile
            {
                GameId = 5,
                Quality = new QualityModel(Quality.Retail),
                OriginalFilePath = "Nintendo - Game Boy/Mega Man IV (USA).zip",
                RelativePath = "Mega Man IV (1993) Retail - Gamarr.zip"
            };

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(x => x.GetByGame(5))
                  .Returns(new System.Collections.Generic.List<GameComponent>
                  {
                      new GameComponent
                      {
                          Id = 11,
                          GameId = 5,
                          ComponentType = GameComponentType.NoIntroRetailRom,
                          Key = "nointro:retail:mega-man-iv-usa",
                          Title = "USA"
                      }
                  });

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                  .Setup(x => x.All())
                  .Returns(new[]
                  {
                      new NoIntroCatalogEntry
                      {
                          SystemKey = "nintendo---game-boy",
                          CanonicalName = "Mega Man IV (USA)",
                          CanonicalFileName = "Mega Man IV (USA).zip"
                      }
                  });

            Subject.BuildFileName(game, gameFile)
                   .Should().Be("Mega Man IV (USA)");
        }
    }
}
