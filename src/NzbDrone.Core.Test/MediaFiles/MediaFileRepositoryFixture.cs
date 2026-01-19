using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class MediaFileRepositoryFixture : DbTest<MediaFileRepository, GameFile>
    {
        private Game _game1;
        private Game _game2;

        [SetUp]
        public void Setup()
        {
            _game1 = Builder<Game>.CreateNew()
                                    .With(s => s.Id = 7)
                                    .Build();

            _game2 = Builder<Game>.CreateNew()
                                    .With(s => s.Id = 8)
                                    .Build();
        }

        [Test]
        public void get_files_by_game()
        {
            var files = Builder<GameFile>.CreateListOfSize(10)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.Quality = new QualityModel())
                .With(c => c.Languages = new List<Language> { Language.English })
                .Random(4)
                .With(s => s.GameId = 12)
                .BuildListOfNew();

            Db.InsertMany(files);

            var gameFiles = Subject.GetFilesByGame(12);

            gameFiles.Should().HaveCount(4);
            gameFiles.Should().OnlyContain(c => c.GameId == 12);
        }

        [Test]
        public void should_delete_files_by_gameId()
        {
            var items = Builder<GameFile>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.GameId = _game2.Id)
                .TheRest()
                .With(c => c.GameId = _game1.Id)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.Quality = new QualityModel(Quality.GOG))
                .With(c => c.Languages = new List<Language> { Language.English })
                .BuildListOfNew();

            Db.InsertMany(items);

            Subject.DeleteForGames(new List<int> { _game1.Id });

            var removedItems = Subject.GetFilesByGame(_game1.Id);
            var nonRemovedItems = Subject.GetFilesByGame(_game2.Id);

            removedItems.Should().HaveCount(0);
            nonRemovedItems.Should().HaveCount(1);
        }
    }
}
