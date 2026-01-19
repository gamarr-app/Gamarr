using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]

    public class TruncatedReleaseGroupFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private GameFile _gameFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                    .CreateNew()
                    .With(s => s.Title = "Game Title")
                    .With(s => s.Year = 2024)
                    .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _gameFile = new GameFile { Quality = new QualityModel(Quality.HDTV720p), ReleaseGroup = "GamarrTest" };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        private void GivenProper()
        {
            _gameFile.Quality.Revision.Version = 2;
        }

        [Test]
        public void should_truncate_from_beginning()
        {
            _game.Title = "The Fantastic Life of Mr. Sisko";

            _gameFile.Quality.Quality = Quality.Bluray1080p;
            _gameFile.ReleaseGroup = "IWishIWasALittleBitTallerIWishIWasABallerIWishIHadAGirlWhoLookedGoodIWouldCallHerIWishIHadARabbitInAHatWithABatAndASixFourImpala";
            _namingConfig.StandardGameFormat = "{Game Title} ({Release Year}) {Quality Full}-{ReleaseGroup:12}";

            var result = Subject.BuildFileName(_game, _gameFile);
            result.Length.Should().BeLessOrEqualTo(255);
            result.Should().Be("The Fantastic Life of Mr. Sisko (2024) Bluray-1080p-IWishIWas...");
        }

        [Test]
        public void should_truncate_from_from_end()
        {
            _game.Title = "The Fantastic Life of Mr. Sisko";

            _gameFile.Quality.Quality = Quality.Bluray1080p;
            _gameFile.ReleaseGroup = "IWishIWasALittleBitTallerIWishIWasABallerIWishIHadAGirlWhoLookedGoodIWouldCallHerIWishIHadARabbitInAHatWithABatAndASixFourImpala";
            _namingConfig.StandardGameFormat = "{Game Title} ({Release Year}) {Quality Full}-{ReleaseGroup:-17}";

            var result = Subject.BuildFileName(_game, _gameFile);
            result.Length.Should().BeLessOrEqualTo(255);
            result.Should().Be("The Fantastic Life of Mr. Sisko (2024) Bluray-1080p-...ASixFourImpala");
        }
    }
}
