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
    public class PlatformSpecificationFixture : CoreTest<PlatformSpecification>
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
                    Platforms = new List<GamePlatform>
                    {
                        new GamePlatform { Name = "PC (Windows)", Abbreviation = "Win", Family = PlatformFamily.PC },
                        new GamePlatform { Name = "PlayStation 5", Abbreviation = "PS5", Family = PlatformFamily.PlayStation }
                    }
                })
            };

            Subject.Value = new List<string> { "PC" };
        }

        [Test]
        public void should_return_true_when_game_has_matching_platform_by_family()
        {
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_game_has_matching_platform_by_name()
        {
            Subject.Value = new List<string> { "PC (Windows)" };
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_game_has_matching_platform_by_abbreviation()
        {
            Subject.Value = new List<string> { "PS5" };
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_game_has_no_matching_platform()
        {
            Subject.Value = new List<string> { "Xbox", "Nintendo" };
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_game_has_no_platforms()
        {
            _game.GameMetadata.Value.Platforms = new List<GamePlatform>();
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_game_metadata_is_null()
        {
            _game.GameMetadata = null;
            Subject.IsSatisfiedBy(_game).Should().BeFalse();
        }

        [Test]
        public void should_be_case_insensitive_for_name()
        {
            Subject.Value = new List<string> { "pc (windows)" };
            Subject.IsSatisfiedBy(_game).Should().BeTrue();
        }

        [Test]
        public void should_be_case_insensitive_for_abbreviation()
        {
            Subject.Value = new List<string> { "ps5" };
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
            Subject.Value = new List<string> { "Nintendo" };
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
            Subject.Value = new List<string> { "PC" };

            var result = Subject.Validate();
            result.IsValid.Should().BeTrue();
        }
    }
}
