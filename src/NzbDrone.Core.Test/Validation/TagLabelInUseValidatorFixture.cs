using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class TagLabelInUseValidatorFixture : CoreTest<TagLabelInUseValidator>
    {
        private void GivenTags(params Tag[] tags)
        {
            Mocker.GetMock<ITagService>()
                  .Setup(s => s.All())
                  .Returns(new List<Tag>(tags));
        }

        [SetUp]
        public void Setup()
        {
            GivenTags(new Tag { Id = 1, Label = "hdr" },
                      new Tag { Id = 2, Label = "retro" });
        }

        [Test]
        public void should_be_valid_when_label_is_not_used()
        {
            Subject.Validate(0, "indie").Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_renaming_onto_another_tags_label()
        {
            Subject.Validate(1, "retro").Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_existing_label_differs_only_by_case()
        {
            // TagService lowercases on write, so a differently cased label still
            // collides with the unique index.
            Subject.Validate(1, "RETRO").Should().BeFalse();
        }

        [Test]
        public void should_be_valid_when_tag_keeps_its_own_label()
        {
            Subject.Validate(1, "hdr").Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_label_is_empty()
        {
            Subject.Validate(1, "").Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_no_tags_exist()
        {
            GivenTags();

            Subject.Validate(1, "retro").Should().BeTrue();
        }
    }
}
