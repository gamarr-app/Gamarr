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
    public class CleanupOrphanedGameGameFileIdsFixture : DbTest<CleanupOrphanedGameGameFileIds, Game>
    {
        [Test]
        public void should_remove_gamefileid_from_game_referencing_deleted_gamefile()
        {
            var removedId = 2;

            var game = Builder<Game>.CreateNew()
                                          .With(e => e.GameFileId = removedId)
                                          .BuildNew();

            Db.Insert(game);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            Db.All<Game>().Should().Contain(e => e.GameFileId == 0);
        }

        [Test]
        public void should_not_remove_gamefileid_from_game_referencing_valid_gamefile()
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
            Db.All<Game>().Should().Contain(e => e.GameFileId == gameFiles.First().Id);
        }
    }
}
