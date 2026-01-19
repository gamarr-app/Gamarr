using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.DownloadHistoryTests
{
    [TestFixture]
    public class DownloadHistoryRepositoryFixture : DbTest<DownloadHistoryRepository, DownloadHistory>
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
        public void should_delete_history_items_by_gameId()
        {
            var items = Builder<DownloadHistory>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game2.Id)
                .TheRest()
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game1.Id)
                .BuildListOfNew();

            Db.InsertMany(items);

            Subject.DeleteByGameIds(new List<int> { _game1.Id });

            var removedItems = Subject.All().Where(h => h.GameId == _game1.Id);
            var nonRemovedItems = Subject.All().Where(h => h.GameId == _game2.Id);

            removedItems.Should().HaveCount(0);
            nonRemovedItems.Should().HaveCount(1);
        }
    }
}
