using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging.Specifications;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AutoTagging.Specifications
{
    [TestFixture]
    public class GenreSpecificationFixture : CoreTest<GenreSpecification>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 1,
                GameMetadata = new LazyLoaded<GameMetadata>(new GameMetadata
                {
                    Genres = new List<string> { "Action", "Adventure", "RPG" }
                })
            };

            Subject.Value = new List<string> { "Action" };
        }

        [Test]
        public void should_return_true_when_game_has_matching_genre()
        {
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_game_has_any_matching_genre()
        {
            Subject.Value = new List<string> { "RPG", "Strategy" };
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_game_has_no_matching_genre()
        {
            Subject.Value = new List<string> { "Strategy", "Puzzle" };
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_game_has_no_genres()
        {
            _game.GameMetadata.Value.Genres = new List<string>();
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_game_metadata_is_null()
        {
            _game.GameMetadata = null;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_be_case_insensitive()
        {
            Subject.Value = new List<string> { "action" };
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_negate_result_when_negate_is_true()
        {
            Subject.Negate = true;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_true_for_non_match_when_negate_is_true()
        {
            Subject.Negate = true;
            Subject.Value = new List<string> { "Strategy" };
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_fail_validation_when_value_is_empty()
        {
            Subject.Value = new List<string>();

            var result = Subject.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_pass_validation_when_value_is_not_empty()
        {
            Subject.Value = new List<string> { "Action" };

            var result = Subject.Validate();
            result.IsValid.Should().BeTrue();
        }
    }
}
