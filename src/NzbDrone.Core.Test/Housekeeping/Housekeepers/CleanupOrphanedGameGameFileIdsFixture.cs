using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    public class CleanupOrphanedGameGameFileIdsFixture : DbTest<CleanupOrphanedGameGameFileIds, Game>
    {
        private Game GivenGame(int gameFileId)
        {
            var game = Builder<Game>.CreateNew()
                                    .With(e => e.GameFileId = gameFileId)
                                    .BuildNew();

            Db.Insert(game);

            return game;
        }

        private GameFile GivenFile(int gameId, string relativePath, int componentId = 0)
        {
            var file = Builder<GameFile>.CreateNew()
                                        .With(h => h.GameId = gameId)
                                        .With(h => h.RelativePath = relativePath)
                                        .With(h => h.ComponentId = componentId)
                                        .With(h => h.Quality = new QualityModel())
                                        .With(h => h.Languages = new List<Language> { Language.English })
                                        .BuildNew();

            Db.Insert(file);

            return file;
        }

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

        // #221: zeroing the pointer left the game reading as file-less until the
        // next disk scan even though its base row was still there.
        [Test]
        public void should_reanchor_a_dangling_pointer_at_the_surviving_base_file()
        {
            var game = GivenGame(9999);

            var baseFile = GivenFile(game.Id, "game.iso");
            GivenFile(game.Id, "Updates/1.2.1/update.iso", 1);

            Subject.Clean();

            Db.All<Game>().Single().GameFileId.Should().Be(baseFile.Id);
        }

        [Test]
        public void should_reanchor_at_a_folder_release_with_an_empty_relative_path()
        {
            var game = GivenGame(9999);

            var baseFile = GivenFile(game.Id, string.Empty);

            Subject.Clean();

            Db.All<Game>().Single().GameFileId.Should().Be(baseFile.Id);
        }

        [Test]
        public void should_not_reanchor_at_an_update_or_dlc_unit()
        {
            var game = GivenGame(9999);

            GivenFile(game.Id, "Updates/1.2.1/update.iso", 1);
            GivenFile(game.Id, "DLC/Season Pass/dlc.iso", 2);

            Subject.Clean();

            Db.All<Game>().Single().GameFileId.Should().Be(0);
        }

        [Test]
        public void should_not_reanchor_at_another_games_file()
        {
            var game = GivenGame(9999);

            var otherFile = GivenFile(game.Id + 500, "other.iso");

            Subject.Clean();

            Db.All<Game>().Single().GameFileId.Should().Be(0);
            otherFile.Id.Should().BeGreaterThan(0);
        }
    }
}
