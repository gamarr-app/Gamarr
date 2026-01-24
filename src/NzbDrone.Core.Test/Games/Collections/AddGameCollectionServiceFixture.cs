using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games.Collections
{
    [TestFixture]
    public class AddGameCollectionServiceFixture : CoreTest<AddGameCollectionService>
    {
        private GameCollection _newCollection;
        private GameCollection _existingCollection;
        private GameCollection _collectionInfo;

        [SetUp]
        public void Setup()
        {
            _newCollection = new GameCollection
            {
                IgdbId = 123,
                Title = "Test Collection",
                Monitored = true,
                SearchOnAdd = true,
                QualityProfileId = 1,
                RootFolderPath = "/games"
            };

            _existingCollection = new GameCollection
            {
                Id = 1,
                IgdbId = 123,
                Title = "Existing Collection",
                CleanTitle = "existingcollection",
                SortTitle = "existing collection",
                Added = DateTime.UtcNow.AddDays(-30)
            };

            _collectionInfo = new GameCollection
            {
                IgdbId = 123,
                Title = "Test Collection From Metadata"
            };

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.FindByIgdbId(It.IsAny<int>()))
                  .Returns((GameCollection)null);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(It.IsAny<int>()))
                  .Returns(_collectionInfo);

            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.AddCollection(It.IsAny<GameCollection>()))
                  .Returns<GameCollection>(c => c);
        }

        [Test]
        public void should_return_existing_collection_when_found()
        {
            Mocker.GetMock<IGameCollectionService>()
                  .Setup(s => s.FindByIgdbId(_newCollection.IgdbId))
                  .Returns(_existingCollection);

            var result = Subject.AddGameCollection(_newCollection);

            result.Should().BeSameAs(_existingCollection);

            Mocker.GetMock<IProvideGameInfo>()
                  .Verify(s => s.GetCollectionInfo(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_return_null_when_game_not_found_exception()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(_newCollection.IgdbId))
                  .Throws(new GameNotFoundException(_newCollection.IgdbId));

            var result = Subject.AddGameCollection(_newCollection);

            result.Should().BeNull();

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(s => s.AddCollection(It.IsAny<GameCollection>()), Times.Never());
        }

        [Test]
        public void should_return_null_when_collection_info_returns_null()
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetCollectionInfo(_newCollection.IgdbId))
                  .Returns((GameCollection)null);

            var result = Subject.AddGameCollection(_newCollection);

            result.Should().BeNull();

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(s => s.AddCollection(It.IsAny<GameCollection>()), Times.Never());
        }

        [Test]
        public void should_add_new_collection_when_not_exists()
        {
            var result = Subject.AddGameCollection(_newCollection);

            result.Should().NotBeNull();
            result.IgdbId.Should().Be(_newCollection.IgdbId);

            Mocker.GetMock<IGameCollectionService>()
                  .Verify(s => s.AddCollection(It.IsAny<GameCollection>()), Times.Once());
        }

        [Test]
        public void should_set_clean_title_and_added_date()
        {
            var result = Subject.AddGameCollection(_newCollection);

            result.Should().NotBeNull();
            result.CleanTitle.Should().NotBeNullOrEmpty();
            result.SortTitle.Should().NotBeNullOrEmpty();
            result.Added.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
