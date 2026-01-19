using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedMetadataFilesFixture : DbTest<CleanupOrphanedMetadataFiles, MetadataFile>
    {
        [Test]
        public void should_delete_metadata_files_that_dont_have_a_coresponding_game()
        {
            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.GameFileId = null)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_metadata_files_that_have_a_coresponding_game()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            Db.Insert(game);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.GameId = game.Id)
                                                    .With(m => m.GameFileId = null)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_delete_metadata_files_that_dont_have_a_coresponding_game_file()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            Db.Insert(game);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.GameId = game.Id)
                                                    .With(m => m.GameFileId = 10)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_metadata_files_that_have_a_coresponding_game_file()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            var gameFile = Builder<GameFile>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.Languages = new List<Language>())
                                                  .BuildNew();

            Db.Insert(game);
            Db.Insert(gameFile);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.GameId = game.Id)
                                                    .With(m => m.GameFileId = gameFile.Id)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_delete_game_metadata_files_that_have_gamefileid_of_zero()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            Db.Insert(game);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                 .With(m => m.GameId = game.Id)
                                                 .With(m => m.Type = MetadataType.GameMetadata)
                                                 .With(m => m.GameFileId = 0)
                                                 .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(0);
        }

        [Test]
        public void should_delete_game_image_files_that_have_gamefileid_of_zero()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            Db.Insert(game);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.GameId = game.Id)
                                                    .With(m => m.Type = MetadataType.GameImage)
                                                    .With(m => m.GameFileId = 0)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(0);
        }
    }
}
