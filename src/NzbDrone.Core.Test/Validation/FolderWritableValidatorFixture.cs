using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class FolderWritableValidatorFixture : CoreTest<FolderWritableValidator>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(It.IsAny<string>()))
                  .Returns(false);
        }

        private void GivenFolderWritable(string path)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(path))
                  .Returns(true);
        }

        [Test]
        public void should_return_false_for_null_path()
        {
            var context = new ValidationTestContext(null);
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_folder_is_not_writable()
        {
            var context = new ValidationTestContext("/path/to/readonly/folder");
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_folder_is_writable()
        {
            GivenFolderWritable("/path/to/writable/folder");

            var context = new ValidationTestContext("/path/to/writable/folder");
            Subject.Validate(context).Should().BeTrue();
        }

        [Test]
        public void should_check_folder_writable_with_disk_provider()
        {
            var path = "/test/path";
            var context = new ValidationTestContext(path);

            Subject.Validate(context);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FolderWritable(path), Times.Once());
        }

        [Test]
        public void should_format_error_message_with_path_and_user()
        {
            var context = new ValidationTestContext("/readonly/path");
            Subject.Validate(context);
            context.MessageFormatter.PlaceholderValues.Should().ContainKey("path");
            context.MessageFormatter.PlaceholderValues.Should().ContainKey("user");
        }
    }
}
