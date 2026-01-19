using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedGameFilesFixture : DbTest<CleanupOrphanedGameFiles, GameFile>
    {
        [Test]
        public void should_delete_orphaned_episode_files()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.Languages = new List<Language> { Language.English })
                                                  .BuildNew();

            Db.Insert(gameFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_game_files()
        {
            var gameFiles = Builder<GameFile>.CreateListOfSize(2)
                                                   .All()
                                                   .With(h => h.Quality = new QualityModel())
                                                   .With(h => h.Languages = new List<Language> { Language.English })
                                                   .BuildListOfNew();

            Db.InsertMany(gameFiles);

            var game = Builder<Game>.CreateNew()
                                          .With(e => e.GameFileId = gameFiles.First().Id)
                                          .BuildNew();

            Db.Insert(game);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            Db.All<Game>().Should().Contain(e => e.GameFileId == AllStoredModels.First().Id);
        }
    }
}
