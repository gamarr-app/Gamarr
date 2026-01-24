using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class PathValidatorFixture : CoreTest<PathValidator>
    {
        private TestValidator<Game> _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new TestValidator<Game>
            {
                v => v.RuleFor(s => s.Path).SetValidator(Subject)
            };
        }

        [Test]
        public void should_be_valid_for_valid_path()
        {
            var game = new Game { Path = @"C:\TV\Game".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_for_null_path()
        {
            var game = new Game { Path = null };
            _validator.Validate(game).IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_for_bad_path()
        {
            var game = new Game { Path = "BAD PATH" };
            _validator.Validate(game).IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_valid_for_unc_path()
        {
            WindowsOnly();

            var game = new Game { Path = @"\\server\share\folder" };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_for_unix_path()
        {
            PosixOnly();

            var game = new Game { Path = "/home/user/games" };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }
    }
}
