using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupDuplicateMetadataFilesFixture : DbTest<CleanupDuplicateMetadataFiles, MetadataFile>
    {
        [Test]
        public void should_not_delete_metadata_files_when_they_are_for_the_same_game_but_different_consumers()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameMetadata)
                                             .With(m => m.GameId = 1)
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(files.Count);
        }

        [Test]
        public void should_not_delete_metadata_files_for_different_game()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameMetadata)
                                             .With(m => m.Consumer = "XbmcMetadata")
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(files.Count);
        }

        [Test]
        public void should_delete_metadata_files_when_they_are_for_the_same_game_and_consumer()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameMetadata)
                                             .With(m => m.GameId = 1)
                                             .With(m => m.Consumer = "XbmcMetadata")
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(1);
        }

        [Test]
        public void should_not_delete_metadata_files_when_there_is_only_one_for_that_game_and_consumer()
        {
            var file = Builder<MetadataFile>.CreateNew()
                                         .BuildNew();

            Db.Insert(file);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(1);
        }

        [Test]
        public void should_not_delete_metadata_files_when_they_are_for_the_same_game_file_but_different_consumers()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameMetadata)
                                             .With(m => m.GameFileId = 1)
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(files.Count);
        }

        [Test]
        public void should_not_delete_metadata_files_for_different_game_file()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameMetadata)
                                             .With(m => m.Consumer = "XbmcMetadata")
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(files.Count);
        }

        [Test]
        public void should_delete_metadata_files_when_they_are_for_the_same_game_file_and_consumer()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameMetadata)
                                             .With(m => m.GameFileId = 1)
                                             .With(m => m.Consumer = "XbmcMetadata")
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(1);
        }

        [Test]
        public void should_not_delete_metadata_files_when_there_is_only_one_for_that_game_file_and_consumer()
        {
            var file = Builder<MetadataFile>.CreateNew()
                                            .BuildNew();

            Db.Insert(file);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(1);
        }

        [Test]
        public void should_not_delete_image_when_they_are_for_the_same_game_file_but_different_consumers()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameImage)
                                             .With(m => m.GameFileId = 1)
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(files.Count);
        }

        [Test]
        public void should_not_delete_image_for_different_game_file()
        {
            var files = Builder<MetadataFile>.CreateListOfSize(2)
                                             .All()
                                             .With(m => m.Type = MetadataType.GameImage)
                                             .With(m => m.Consumer = "XbmcMetadata")
                                             .BuildListOfNew();

            Db.InsertMany(files);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(files.Count);
        }

        [Test]
        public void should_not_delete_image_when_there_is_only_one_for_that_game_file_and_consumer()
        {
            var file = Builder<MetadataFile>.CreateNew()
                                            .BuildNew();

            Db.Insert(file);
            Subject.Clean();
            AllStoredModels.Count.Should().Be(1);
        }
    }
}
