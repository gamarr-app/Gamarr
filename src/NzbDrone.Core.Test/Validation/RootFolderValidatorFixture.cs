using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class RootFolderValidatorFixture : CoreTest<RootFolderValidator>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder>());
        }

        private void GivenExistingRootFolder(string path)
        {
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder>
                  {
                      new RootFolder { Path = path }
                  });
        }

        [Test]
        public void should_return_true_for_null_path()
        {
            var context = new ValidationTestContext(null);
            Subject.Validate(context).Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_path_is_not_a_root_folder()
        {
            GivenExistingRootFolder("/games/existing");

            var context = new ValidationTestContext("/games/new");
            Subject.Validate(context).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_path_is_already_a_root_folder()
        {
            GivenExistingRootFolder("/games/existing");

            var context = new ValidationTestContext("/games/existing");
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_no_root_folders_exist()
        {
            var context = new ValidationTestContext("/games/new");
            Subject.Validate(context).Should().BeTrue();
        }

        [Test]
        public void should_format_error_message_with_path()
        {
            var context = new ValidationTestContext("/test/path");
            Subject.Validate(context);
            context.MessageFormatter.PlaceholderValues.Should().ContainKey("path");
        }
    }
}
