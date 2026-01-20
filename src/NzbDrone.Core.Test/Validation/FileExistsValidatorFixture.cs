using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class FileExistsValidatorFixture : CoreTest<FileExistsValidator>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(false);
        }

        private void GivenFileExists(string path)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(path))
                  .Returns(true);
        }

        [Test]
        public void should_return_false_for_null_path()
        {
            var context = new ValidationTestContext(null);
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_file_does_not_exist()
        {
            var context = new ValidationTestContext("/path/to/missing/file.txt");
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_file_exists()
        {
            GivenFileExists("/path/to/existing/file.txt");

            var context = new ValidationTestContext("/path/to/existing/file.txt");
            Subject.Validate(context).Should().BeTrue();
        }

        [Test]
        public void should_check_file_existence_with_disk_provider()
        {
            var path = "/test/path/file.txt";
            var context = new ValidationTestContext(path);

            Subject.Validate(context);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FileExists(path), Times.Once());
        }

        [Test]
        public void should_format_error_message_with_file_path()
        {
            var context = new ValidationTestContext("/missing/file.txt");
            Subject.Validate(context);
            context.MessageFormatter.PlaceholderValues.Should().ContainKey("file");
        }
    }
}
