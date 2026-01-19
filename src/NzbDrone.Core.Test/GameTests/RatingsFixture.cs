using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class RatingsFixture : CoreTest
    {
        [Test]
        public void should_create_empty_ratings()
        {
            var ratings = new Ratings();

            ratings.Should().NotBeNull();
            ratings.Igdb.Should().BeNull();
            ratings.Metacritic.Should().BeNull();
        }

        [Test]
        public void should_create_ratings_with_igdb()
        {
            var ratings = new Ratings
            {
                Igdb = new RatingChild
                {
                    Value = 85.5m,
                    Votes = 1000,
                    Type = RatingType.User
                }
            };

            ratings.Igdb.Should().NotBeNull();
            ratings.Igdb.Value.Should().Be(85.5m);
            ratings.Igdb.Votes.Should().Be(1000);
            ratings.Igdb.Type.Should().Be(RatingType.User);
        }

        [Test]
        public void should_create_ratings_with_metacritic()
        {
            var ratings = new Ratings
            {
                Metacritic = new RatingChild
                {
                    Value = 92m,
                    Votes = 50,
                    Type = RatingType.Critic
                }
            };

            ratings.Metacritic.Should().NotBeNull();
            ratings.Metacritic.Value.Should().Be(92m);
            ratings.Metacritic.Votes.Should().Be(50);
            ratings.Metacritic.Type.Should().Be(RatingType.Critic);
        }

        [Test]
        public void should_create_ratings_with_both_sources()
        {
            var ratings = new Ratings
            {
                Igdb = new RatingChild
                {
                    Value = 85m,
                    Votes = 5000,
                    Type = RatingType.User
                },
                Metacritic = new RatingChild
                {
                    Value = 90m,
                    Votes = 75,
                    Type = RatingType.Critic
                }
            };

            ratings.Igdb.Should().NotBeNull();
            ratings.Metacritic.Should().NotBeNull();
            ratings.Igdb.Value.Should().Be(85m);
            ratings.Metacritic.Value.Should().Be(90m);
        }

        [Test]
        public void rating_child_should_have_default_values()
        {
            var ratingChild = new RatingChild();

            ratingChild.Value.Should().Be(0);
            ratingChild.Votes.Should().Be(0);
            ratingChild.Type.Should().Be(RatingType.User);
        }

        [Test]
        public void ratings_should_be_equatable()
        {
            var ratings1 = new Ratings
            {
                Igdb = new RatingChild { Value = 85m, Votes = 100 },
                Metacritic = new RatingChild { Value = 90m, Votes = 50 }
            };

            var ratings2 = new Ratings
            {
                Igdb = new RatingChild { Value = 85m, Votes = 100 },
                Metacritic = new RatingChild { Value = 90m, Votes = 50 }
            };

            ratings1.Should().Be(ratings2);
        }

        [Test]
        public void different_ratings_should_not_be_equal()
        {
            var ratings1 = new Ratings
            {
                Igdb = new RatingChild { Value = 85m, Votes = 100 }
            };

            var ratings2 = new Ratings
            {
                Igdb = new RatingChild { Value = 90m, Votes = 100 }
            };

            ratings1.Should().NotBe(ratings2);
        }
    }
}
