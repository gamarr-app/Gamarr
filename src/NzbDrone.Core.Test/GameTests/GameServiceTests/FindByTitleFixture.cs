using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
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

        private void GivenSameTitleDifferentYearInLibrary()
        {
            // _candidates[0] (2000) and _candidates[1] (1999) are both "batman".
            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.FindByTitles(It.IsAny<List<string>>()))
                  .Returns(_candidates.Take(2).ToList());
        }

        [Test]
        public void should_return_null_instead_of_throwing_when_title_without_year_matches_two_games()
        {
            GivenSameTitleDifferentYearInLibrary();

            Subject.FindByTitle("Batman").Should().BeNull();
        }

        [Test]
        public void should_use_year_to_pick_between_same_title_games()
        {
            GivenSameTitleDifferentYearInLibrary();

            var game = Subject.FindByTitle("Batman", 1999);

            game.Should().NotBeNull();
            game.Year.Should().Be(1999);
        }

        [Test]
        public void should_return_null_when_year_matches_neither_same_title_game()
        {
            GivenSameTitleDifferentYearInLibrary();

            Subject.FindByTitle("Batman", 2020).Should().BeNull();
        }

        [Test]
        public void should_still_throw_from_candidate_overload_when_ambiguous()
        {
            Assert.Throws<MultipleGamesFoundException>(() =>
                Subject.FindByTitle(new List<string> { "batman" }, null, new List<string>(), _candidates.Take(2).ToList()));
        }
    }
}
