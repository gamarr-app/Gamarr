using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.GameStats;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameStatsTests
{
    [TestFixture]
    public class GameStatisticsFixture : DbTest<GameStatisticsRepository, Game>
    {
        private Game _game;
        private GameFile _gameFile;

        [SetUp]
        public void Setup()
        {
            var gameMetadata = Builder<GameMetadata>.CreateNew()
                .With(h => h.IgdbId = 123456)
                .With(m => m.Runtime = 90)
                .BuildNew();
            Db.Insert(gameMetadata);

            _game = Builder<Game>.CreateNew()
                .With(m => m.GameMetadataId = gameMetadata.Id)
                .With(e => e.GameFileId = 0)
                .With(e => e.Monitored = false)
                .BuildNew();

            _game.Id = Db.Insert(_game).Id;

            _gameFile = Builder<GameFile>.CreateNew()
                .With(e => e.GameId = _game.Id)
                .With(e => e.Quality = new QualityModel(Quality.Repack))
                .With(e => e.Languages = new List<Language> { Language.English })
                .BuildNew();
        }

        private void GivenGameWithFile()
        {
            _game.GameFileId = 1;
        }

        private void GivenMonitoredGame()
        {
            _game.Monitored = true;
        }

        private void GivenGameFile()
        {
            Db.Insert(_gameFile);
        }

        [Test]
        public void should_get_stats_for_game()
        {
            GivenMonitoredGame();

            var stats = Subject.GameStatistics();

            stats.Should().HaveCount(1);
        }

        [Test]
        public void should_have_size_on_disk_of_zero_when_no_game_file()
        {
            var stats = Subject.GameStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(0);
        }

        [Test]
        public void should_have_size_on_disk_when_game_file_exists()
        {
            GivenGameWithFile();
            GivenGameFile();

            var stats = Subject.GameStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(_gameFile.Size);
        }

        private GameFile GivenUnit(string relativePath, int componentId)
        {
            var file = Builder<GameFile>.CreateNew()
                .With(e => e.GameId = _game.Id)
                .With(e => e.RelativePath = relativePath)
                .With(e => e.ComponentId = componentId)
                .With(e => e.Quality = new QualityModel(Quality.Repack))
                .With(e => e.Languages = new List<Language> { Language.English })
                .BuildNew();

            Db.Insert(file);

            return file;
        }

        // #221: GameFileCount came from the game's single GameFileId pointer, so
        // it was never anything but 0 or 1 however many units the game owned.
        [Test]
        public void should_count_every_unit_the_game_owns()
        {
            var baseFile = GivenUnit("game.iso", 0);
            GivenUnit("Updates/1.2.1/update.iso", 1);
            GivenUnit("DLC/Season Pass/dlc.iso", 2);

            _game.GameFileId = baseFile.Id;
            Db.Update(_game);

            var stats = Subject.GameStatistics();

            stats.Should().HaveCount(1);
            stats.First().GameFileCount.Should().Be(3);
            stats.First().PrimaryFileCount.Should().Be(1);
        }

        [Test]
        public void should_report_no_primary_file_when_the_pointer_is_unset()
        {
            GivenUnit("DLC/Season Pass/dlc.iso", 2);

            var stats = Subject.GameStatistics();

            stats.Should().HaveCount(1);
            stats.First().GameFileCount.Should().Be(1);
            stats.First().PrimaryFileCount.Should().Be(0);
        }

        [Test]
        public void should_have_file_count_of_zero_when_no_game_file()
        {
            var stats = Subject.GameStatistics();

            stats.Should().HaveCount(1);
            stats.First().GameFileCount.Should().Be(0);
        }

        // [Test]
        // public void should_not_duplicate_size_for_multi_game_files()
        // {
        //     GivenGameWithFile();
        //     GivenGameFile();
        //
        //     var game2 = _game.JsonClone();
        //
        //     var gameMetadata = Builder<GameMetadata>.CreateNew().With(h => h.IgdbId = 234567).BuildNew();
        //     Db.Insert(gameMetadata);
        //
        //     game2.Id = 0;
        //     game2.GameMetadataId = gameMetadata.Id;
        //
        //     Db.Insert(game2);
        //
        //     var stats = Subject.GameStatistics();
        //
        //     stats.Should().HaveCount(1);
        //     stats.First().SizeOnDisk.Should().Be(_gameFile.Size);
        // }
    }
}
