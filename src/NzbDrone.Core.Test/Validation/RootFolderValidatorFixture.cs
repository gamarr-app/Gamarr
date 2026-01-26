using System.Collections.Generic;
using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class RootFolderValidatorFixture : CoreTest<RootFolderValidator>
    {
        private TestValidator<Game> _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new TestValidator<Game>
            {
                v => v.RuleFor(s => s.Path).Must(path => Subject.Validate(path))
            };

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder>());
        }

        [Test]
        public void should_be_valid_when_path_is_not_a_root_folder()
        {
            var game = new Game { Path = @"C:\Games\Game Title".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_path_is_a_root_folder()
        {
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder> { new RootFolder { Path = @"C:\Games\Game Title".AsOsAgnostic() } });

            var game = new Game { Path = @"C:\Games\Game Title".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_valid_when_path_is_null()
        {
            var game = new Game { Path = null };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_no_root_folders_exist()
        {
            var game = new Game { Path = @"C:\Games\Game Title".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }
    }
}
