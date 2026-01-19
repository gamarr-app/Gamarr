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
    public class CollectionTheFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private GameFile _gameFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                    .CreateNew()
                    .With(e => e.Title = "Game Title")
                    .Build();

            _gameFile = new GameFile { Quality = new QualityModel(Quality.Uplay), ReleaseGroup = "GamarrTest" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                .Setup(v => v.All())
                .Returns(new List<CustomFormat>());
        }

        [TestCase("The Badger Collection", "Badger Collection, The")]
        [TestCase("The Mover Collection", "Mover Collection, The")]
        [TestCase("A Stupid Collection", "Stupid Collection, A")]
        [TestCase("An Astounding Collection", "Astounding Collection, An")]
        [TestCase("The Amazing Animal-Hero Collection (2001)", "Amazing Animal-Hero Collection, The (2001)")]
        [TestCase("A Different Game (AU)", "Different Game, A (AU)")]
        [TestCase("The Repairer (ZH) (2015)", "Repairer, The (ZH) (2015)")]
        [TestCase("The Eighth Sense 2 (Thai)", "Eighth Sense 2, The (Thai)")]
        [TestCase("The Astonishing Jog (Latin America)", "Astonishing Jog, The (Latin America)")]
        [TestCase("The Hampster Pack (B&F)", "Hampster Pack, The (B&F)")]
        [TestCase("The Gasm: I (Almost) Got Away With It (1900)", "Gasm - I (Almost) Got Away With It, The (1900)")]
        public void should_get_expected_title_back(string collection, string expected)
        {
            SetCollectionName(_game, collection);
            _namingConfig.StandardGameFormat = "{Game CollectionThe}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be(expected);
        }

        [TestCase("A")]
        [TestCase("Anne")]
        [TestCase("Theodore")]
        [TestCase("3%")]
        public void should_not_change_title(string collection)
        {
            SetCollectionName(_game, collection);
            _namingConfig.StandardGameFormat = "{Game CollectionThe}";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be(collection);
        }

        private void SetCollectionName(Game game, string collectionName)
        {
            var metadata = new GameMetadata()
            {
                CollectionTitle = collectionName,
            };
            game.GameMetadata = new Core.Datastore.LazyLoaded<GameMetadata>(metadata);
            game.GameMetadata.Value.CollectionTitle = collectionName;
        }
    }
}
