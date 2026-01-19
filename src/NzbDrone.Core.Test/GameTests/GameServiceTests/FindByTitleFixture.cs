using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.GameServiceTests
{
    [TestFixture]
    public class FindByTitleFixture : CoreTest<GameService>
    {
        private List<Game> _candidates;

        [SetUp]
        public void Setup()
        {
            _candidates = Builder<Game>.CreateListOfSize(3)
                                        .TheFirst(1)
                                        .With(x => x.GameMetadata.Value.CleanTitle = "batman")
                                        .With(x => x.Year = 2000)
                                        .TheNext(1)
                                        .With(x => x.GameMetadata.Value.CleanTitle = "batman")
                                        .With(x => x.Year = 1999)
                                        .TheRest()
                                        .With(x => x.GameMetadata.Value.CleanTitle = "darkknight")
                                        .With(x => x.Year = 2008)
                                        .With(x => x.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle>
                                        {
                                            new AlternativeTitle
                                            {
                                                CleanTitle = "batman"
                                            }
                                        })
                                        .Build()
                                        .ToList();
        }

        [Test]
        public void should_find_by_title_year()
        {
            var game = Subject.FindByTitle(new List<string> { "batman" }, 2000, new List<string>(), _candidates);

            game.Should().NotBeNull();
            game.Year.Should().Be(2000);
        }

        [Test]
        public void should_find_candidates_by_alt_titles()
        {
            var game = Subject.FindByTitle(new List<string> { "batman" }, 2008, new List<string>(), _candidates);
            game.Should().NotBeNull();
            game.Year.Should().Be(2008);
        }
    }
}
