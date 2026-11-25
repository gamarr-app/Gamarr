using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]
    public class FileNameValidationFixture
    {
        private class Holder
        {
            public string GameFormat { get; set; }
            public string FolderFormat { get; set; }
        }

        private class HolderValidator : AbstractValidator<Holder>
        {
            public HolderValidator()
            {
                RuleFor(c => c.GameFormat).ValidGameFormat();
                RuleFor(c => c.FolderFormat).ValidGameFolderFormat();
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void blank_formats_report_only_the_not_empty_error(string value)
        {
            var result = new HolderValidator().Validate(new Holder { GameFormat = value, FolderFormat = value });

            // One NotEmpty error per property, nothing from the other rules, and no throw.
            result.Errors.Should().HaveCount(2);
        }

        [Test]
        public void valid_formats_pass()
        {
            var result = new HolderValidator().Validate(new Holder
            {
                GameFormat = "{Game Title} ({Release Year})",
                FolderFormat = "{Game Title}"
            });

            result.IsValid.Should().BeTrue();
        }
    }
}
