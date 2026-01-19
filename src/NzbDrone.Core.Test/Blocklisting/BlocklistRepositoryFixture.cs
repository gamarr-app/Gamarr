using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Blocklisting
{
    [TestFixture]
    public class BlocklistRepositoryFixture : DbTest<BlocklistRepository, Blocklist>
    {
        private Blocklist _blocklist;
        private Game _game1;
        private Game _game2;

        [SetUp]
        public void Setup()
        {
            _blocklist = new Blocklist
            {
                GameId = 1234,
                Quality = new QualityModel(),
                Languages = new List<Language>(),
                SourceTitle = "game.title.1998",
                Date = DateTime.UtcNow
            };

            _game1 = Builder<Game>.CreateNew()
                         .With(s => s.Id = 7)
                         .Build();

            _game2 = Builder<Game>.CreateNew()
                                     .With(s => s.Id = 8)
                                     .Build();
        }

        [Test]
        public void should_be_able_to_write_to_database()
        {
            Subject.Insert(_blocklist);
            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_should_have_game_id()
        {
            Subject.Insert(_blocklist);

            Subject.All().First().GameId.Should().Be(_blocklist.GameId);
        }

        [Test]
        public void should_check_for_blocklisted_title_case_insensative()
        {
            Subject.Insert(_blocklist);

            Subject.BlocklistedByTitle(_blocklist.GameId, _blocklist.SourceTitle.ToUpperInvariant()).Should().HaveCount(1);
        }

        [Test]
        public void should_delete_blocklists_by_gameId()
        {
            var blocklistItems = Builder<Blocklist>.CreateListOfSize(5)
                .TheFirst(1)
                .With(c => c.GameId = _game2.Id)
                .TheRest()
                .With(c => c.GameId = _game1.Id)
                .All()
                .With(c => c.Quality = new QualityModel())
                .With(c => c.Languages = new List<Language>())
                .With(c => c.Id = 0)
                .BuildListOfNew();

            Db.InsertMany(blocklistItems);

            Subject.DeleteForGames(new List<int> { _game1.Id });

            var blocklist = Subject.All();
            var removedGameBlocklists = blocklist.Where(b => b.GameId == _game1.Id);
            var nonRemovedGameBlocklists = blocklist.Where(b => b.GameId == _game2.Id);

            removedGameBlocklists.Should().HaveCount(0);
            nonRemovedGameBlocklists.Should().HaveCount(1);
        }
    }
}
