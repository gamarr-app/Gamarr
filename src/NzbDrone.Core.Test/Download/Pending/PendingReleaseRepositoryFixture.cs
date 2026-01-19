using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class PendingReleaseRepositoryFixture : DbTest<PendingReleaseRepository, PendingRelease>
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
        public void should_delete_files_by_gameId()
        {
            var files = Builder<PendingRelease>.CreateListOfSize(5)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game1.Id)
                .With(c => c.Release = new ReleaseInfo())
                .BuildListOfNew();

            Db.InsertMany(files);

            Subject.DeleteByGameIds(new List<int> { _game1.Id });

            var remainingFiles = Subject.AllByGameId(_game1.Id);

            remainingFiles.Should().HaveCount(0);
        }

        [Test]
        public void should_get_files_by_gameId()
        {
            var files = Builder<PendingRelease>.CreateListOfSize(5)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.GameId = _game1.Id)
                .With(c => c.Release = new ReleaseInfo())
                .Random(2)
                .With(c => c.GameId = _game2.Id)
                .BuildListOfNew();

            Db.InsertMany(files);

            var remainingFiles = Subject.AllByGameId(_game1.Id);

            remainingFiles.Should().HaveCount(3);
            remainingFiles.Should().OnlyContain(c => c.GameId == _game1.Id);
        }
    }
}
