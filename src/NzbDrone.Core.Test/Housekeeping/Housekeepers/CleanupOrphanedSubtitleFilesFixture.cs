using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Extras.Subtitles;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedSubtitleFilesFixture : DbTest<CleanupOrphanedSubtitleFiles, SubtitleFile>
    {
        [Test]
        public void should_delete_subtitle_files_that_dont_have_a_coresponding_game()
        {
            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.GameFileId = 0)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_subtitle_files_that_have_a_coresponding_game()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            Db.Insert(game);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.GameId = game.Id)
                                                    .With(m => m.GameFileId = 0)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_delete_subtitle_files_that_dont_have_a_coresponding_game_file()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            Db.Insert(game);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.GameId = game.Id)
                                                    .With(m => m.GameFileId = 10)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_subtitle_files_that_have_a_coresponding_game_file()
        {
            var game = Builder<Game>.CreateNew()
                                      .BuildNew();

            var gameFile = Builder<GameFile>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.Languages = new List<Language>())
                                                  .BuildNew();

            Db.Insert(game);
            Db.Insert(gameFile);

            var subtitleFile = Builder<SubtitleFile>.CreateNew()
                                                    .With(m => m.GameId = game.Id)
                                                    .With(m => m.GameFileId = gameFile.Id)
                                                    .With(m => m.Language = Language.English)
                                                    .BuildNew();

            Db.Insert(subtitleFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
