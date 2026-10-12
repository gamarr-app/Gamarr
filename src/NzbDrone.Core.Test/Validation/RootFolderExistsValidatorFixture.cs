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
    public class RootFolderExistsValidatorFixture : CoreTest<RootFolderExistsValidator>
    {
        private TestValidator<Game> _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new TestValidator<Game>
            {
                v => v.RuleFor(s => s.RootFolderPath).Must(path => Subject.Validate(path))
            };

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder>());

            Mocker.GetMock<IPlatformRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<PlatformRootFolder>());
        }

        [Test]
        public void should_be_valid_when_path_is_null()
        {
            var game = new Game { RootFolderPath = null };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_path_is_not_a_root_folder_or_platform_root_folder()
        {
            var game = new Game { RootFolderPath = @"C:\Games".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_valid_when_path_is_a_root_folder()
        {
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder> { new RootFolder { Path = @"C:\Games".AsOsAgnostic() } });

            var game = new Game { RootFolderPath = @"C:\Games".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        // #186: a platform root folder mapping (e.g. Nintendo Switch ->
        // C:\Games\Switch) is a legitimate add destination even though it is
        // never itself registered in IRootFolderService, and even though
        // PlatformRootFolderService.Validate never required it to be a
        // subpath of one. Before this fix, resolving RootFolderPath from a
        // platform mapping always failed with "Root folder does not exist".
        [Test]
        public void should_be_valid_when_path_is_a_platform_root_folder()
        {
            Mocker.GetMock<IPlatformRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<PlatformRootFolder>
                  {
                      new PlatformRootFolder { Platform = PlatformFamily.NintendoSwitch, Path = @"C:\Games\Switch".AsOsAgnostic() }
                  });

            var game = new Game { RootFolderPath = @"C:\Games\Switch".AsOsAgnostic() };
            _validator.Validate(game).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_platform_root_folder_path_is_empty()
        {
            Mocker.GetMock<IPlatformRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<PlatformRootFolder>
                  {
                      new PlatformRootFolder { Platform = PlatformFamily.NintendoSwitch, Path = string.Empty }
                  });

            var game = new Game { RootFolderPath = string.Empty };
            _validator.Validate(game).IsValid.Should().BeFalse();
        }
    }
}
