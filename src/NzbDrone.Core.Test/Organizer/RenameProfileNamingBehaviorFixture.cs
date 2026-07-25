using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
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
    }
}
