using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class PathExistsValidatorFixture : CoreTest<PathExistsValidator>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);
        }

        private void GivenFolderExists(string path)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(path))
                  .Returns(true);
        }

        [Test]
        public void should_return_false_for_null_path()
        {
            var context = new ValidationTestContext(null);
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_folder_does_not_exist()
        {
            var context = new ValidationTestContext("/path/to/missing/folder");
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_folder_exists()
        {
            GivenFolderExists("/path/to/existing/folder");

            var context = new ValidationTestContext("/path/to/existing/folder");
            Subject.Validate(context).Should().BeTrue();
        }

        [Test]
        public void should_check_folder_existence_with_disk_provider()
        {
            var path = "/test/path";
            var context = new ValidationTestContext(path);

            Subject.Validate(context);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FolderExists(path), Times.Once());
        }

        [Test]
        public void should_format_error_message_with_path()
        {
            var context = new ValidationTestContext("/missing/path");
            Subject.Validate(context);
            context.MessageFormatter.PlaceholderValues.Should().ContainKey("path");
        }
    }
}
