using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class FolderWritableValidatorFixture : CoreTest<FolderWritableValidator>
    {
        private TestValidator<Game> _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new TestValidator<Game>
            {
                v => v.RuleFor(s => s.Path).SetValidator(Subject)
            };

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(It.IsAny<string>()))
                  .Returns(true);
        }

        [Test]
        public void should_be_valid_when_folder_is_writable()
        {
            var game = new Game { Path = @"C:\Games\Game Title".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_folder_is_not_writable()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(It.IsAny<string>()))
                  .Returns(false);

            var game = new Game { Path = @"C:\Games\Game Title".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_path_is_null()
        {
            var game = new Game { Path = null };
            _validator.Validate(game).IsValid.Should().BeFalse();
        }
    }
}
