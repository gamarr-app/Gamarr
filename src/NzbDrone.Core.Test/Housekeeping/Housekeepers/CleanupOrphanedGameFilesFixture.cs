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
    [TestFixture]
    public class CleanupOrphanedGameFilesFixture : DbTest<CleanupOrphanedGameFiles, GameFile>
    {
        private Game GivenGame()
        {
            var game = Builder<Game>.CreateNew()
                                    .With(e => e.GameFileId = 0)
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
        public void should_delete_file_rows_whose_game_is_gone()
        {
            GivenFile(42, "game.iso");

            Subject.Clean();

            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_delete_file_rows_with_an_unassigned_game_id()
        {
            GivenFile(0, "game.iso");

            Subject.Clean();

            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_file_rows_of_a_game_that_still_exists()
        {
            var game = GivenGame();
            var gameFile = GivenFile(game.Id, "game.iso");

            game.GameFileId = gameFile.Id;
            Db.Update(game);

            Subject.Clean();

            AllStoredModels.Should().HaveCount(1);
        }

        // #221: the join used to be GameFiles.Id = Games.GameFileId, so every unit
        // that was not the game's primary file was deleted on every housekeeping
        // run while the folders stayed on disk.
        [Test]
        public void should_not_delete_update_or_dlc_units_of_a_game_that_still_exists()
        {
            var game = GivenGame();

            var baseFile = GivenFile(game.Id, "game.iso");
            var update = GivenFile(game.Id, "Updates/1.2.1/update.iso", 1);
            var dlc = GivenFile(game.Id, "DLC/Season Pass/dlc.iso", 2);

            game.GameFileId = baseFile.Id;
            Db.Update(game);

            Subject.Clean();

            AllStoredModels.Should().HaveCount(3);
            AllStoredModels.Select(f => f.Id).Should().BeEquivalentTo(new[] { baseFile.Id, update.Id, dlc.Id });
        }

        [Test]
        public void should_delete_only_the_units_of_the_deleted_game()
        {
            var game = GivenGame();

            var baseFile = GivenFile(game.Id, "game.iso");
            var update = GivenFile(game.Id, "Updates/1.2.1/update.iso", 1);

            var strandedBase = GivenFile(game.Id + 1000, "gone.iso");
            var strandedUpdate = GivenFile(game.Id + 1000, "Updates/9.9/update.iso", 3);

            game.GameFileId = baseFile.Id;
            Db.Update(game);

            Subject.Clean();

            AllStoredModels.Select(f => f.Id).Should().BeEquivalentTo(new[] { baseFile.Id, update.Id });
            AllStoredModels.Should().NotContain(f => f.Id == strandedBase.Id);
            AllStoredModels.Should().NotContain(f => f.Id == strandedUpdate.Id);
        }
    }
}
