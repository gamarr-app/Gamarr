using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Extras.Subtitles;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class SubtitleFileRepositoryFixture : DbTest<SubtitleFileRepository, SubtitleFile>
    {
        private Game _game;
        private GameFile _gameFile1;
        private GameFile _gameFile2;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Id = 7)
                                     .Build();

            _gameFile1 = Builder<GameFile>.CreateNew()
                                     .With(s => s.Id = 10)
                                     .With(s => s.GameId = _game.Id)
                                     .Build();

            _gameFile2 = Builder<GameFile>.CreateNew()
                                     .With(s => s.Id = 11)
                                     .With(s => s.GameId = _game.Id)
                                     .Build();
        }

        [Test]
        public void should_delete_files_by_gameId()
        {
            var files = Builder<SubtitleFile>.CreateListOfSize(5)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game.Id)
                .With(c => c.GameFileId = 11)
                .With(c => c.Language = Language.English)
                .BuildListOfNew();

            Db.InsertMany(files);

            Subject.DeleteForGames(new List<int> { _game.Id });

            var remainingFiles = Subject.GetFilesByGame(_game.Id);

            remainingFiles.Should().HaveCount(0);
        }

        [Test]
        public void should_delete_files_by_gameFileId()
        {
            var files = Builder<SubtitleFile>.CreateListOfSize(5)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game.Id)
                .With(c => c.GameFileId = _gameFile2.Id)
                .With(c => c.Language = Language.English)
                .Random(2)
                .With(c => c.GameFileId = _gameFile1.Id)
                .BuildListOfNew();

            Db.InsertMany(files);

            Subject.DeleteForGameFile(_gameFile2.Id);

            var remainingFiles = Subject.GetFilesByGame(_game.Id);

            remainingFiles.Should().HaveCount(2);
        }

        [Test]
        public void should_get_files_by_gameFileId()
        {
            var files = Builder<SubtitleFile>.CreateListOfSize(5)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game.Id)
                .With(c => c.GameFileId = _gameFile2.Id)
                .With(c => c.Language = Language.English)
                .Random(2)
                .With(c => c.GameFileId = _gameFile1.Id)
                .BuildListOfNew();

            Db.InsertMany(files);

            var remainingFiles = Subject.GetFilesByGameFile(_gameFile2.Id);

            remainingFiles.Should().HaveCount(3);
            remainingFiles.Should().OnlyContain(c => c.GameFileId == _gameFile2.Id);
        }

        [Test]
        public void should_get_files_by_gameId()
        {
            var files = Builder<SubtitleFile>.CreateListOfSize(5)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game.Id)
                .With(c => c.GameFileId = _gameFile2.Id)
                .With(c => c.Language = Language.English)
                .Random(2)
                .With(c => c.GameFileId = _gameFile1.Id)
                .BuildListOfNew();

            Db.InsertMany(files);

            var remainingFiles = Subject.GetFilesByGame(_game.Id);

            remainingFiles.Should().HaveCount(5);
            remainingFiles.Should().OnlyContain(c => c.GameId == _game.Id);
        }
    }
}
