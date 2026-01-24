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
    public class GameModeSpecificationFixture : CoreTest<GameModeSpecification>
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
                    GameModes = new List<string> { "Single Player", "Multiplayer", "Co-op" }
                })
            };

            Subject.Value = new List<string> { "Single Player" };
        }

        [Test]
        public void should_return_true_when_game_has_matching_mode()
        {
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_game_has_any_matching_mode()
        {
            Subject.Value = new List<string> { "Co-op", "Battle Royale" };
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_game_has_no_matching_mode()
        {
            Subject.Value = new List<string> { "Battle Royale", "MMO" };
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_game_has_no_modes()
        {
            _game.GameMetadata.Value.GameModes = new List<string>();
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_game_modes_is_null()
        {
            _game.GameMetadata.Value.GameModes = null;
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
            Subject.Value = new List<string> { "single player" };
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
            Subject.Value = new List<string> { "MMO" };
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
            Subject.Value = new List<string> { "Multiplayer" };

            var result = Subject.Validate();
            result.IsValid.Should().BeTrue();
        }
    }
}
