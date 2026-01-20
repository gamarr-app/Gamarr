using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class PathValidatorFixture : CoreTest<PathValidator>
    {
        [Test]
        public void should_return_false_for_null_path()
        {
            var context = new ValidationTestContext(null);
            Subject.Validate(context).Should().BeFalse();
        }

        [Test]
        public void should_return_false_for_empty_path()
        {
            var context = new ValidationTestContext(string.Empty);
            Subject.Validate(context).Should().BeFalse();
        }

        [TestCase("/home/user/games")]
        [TestCase("/var/lib/gamarr")]
        [TestCase("/mnt/media/games")]
        public void should_return_true_for_valid_linux_path(string path)
        {
            var context = new ValidationTestContext(path);
            Subject.Validate(context).Should().BeTrue();
        }

        [Test]
        public void should_format_error_message_with_path()
        {
            var context = new ValidationTestContext(null);
            Subject.Validate(context);
            // The validator appends the path argument for formatting
            context.MessageFormatter.PlaceholderValues.Should().ContainKey("path");
        }
    }

    // Helper class for testing PropertyValidator
    public class ValidationTestContext : FluentValidation.Validators.PropertyValidatorContext
    {
        public ValidationTestContext(object propertyValue)
            : base(new FluentValidation.Internal.ValidationContext<object>(new object()),
                   FluentValidation.Internal.PropertyRule.Create<object, string>(x => string.Empty),
                   "TestProperty")
        {
            PropertyValue = propertyValue;
        }
    }
}
