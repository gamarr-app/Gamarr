using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Tags
{
    [TestFixture]
    public class TagServiceFixture : CoreTest<TagService>
    {
        private Tag _tag;

        [SetUp]
        public void Setup()
        {
            _tag = new Tag
            {
                Id = 1,
                Label = "test-tag"
            };

            Mocker.GetMock<ITagRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<Tag> { _tag });

            Mocker.GetMock<ITagRepository>()
                  .Setup(s => s.Get(1))
                  .Returns(_tag);
        }

        [Test]
        public void should_return_all_tags()
        {
            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_get_tag_by_id()
        {
            Subject.GetTag(1).Should().Be(_tag);
        }

        [Test]
        public void should_add_new_tag()
        {
            var newTag = new Tag { Label = "new-tag" };

            Mocker.GetMock<ITagRepository>()
                  .Setup(s => s.Insert(newTag))
                  .Returns(newTag);

            Subject.Add(newTag);

            Mocker.GetMock<ITagRepository>()
                  .Verify(s => s.Insert(newTag), Times.Once());
        }

        [Test]
        public void should_update_tag()
        {
            Subject.Update(_tag);

            Mocker.GetMock<ITagRepository>()
                  .Verify(s => s.Update(_tag), Times.Once());
        }

        [Test]
        public void should_delete_tag()
        {
            Subject.Delete(1);

            Mocker.GetMock<ITagRepository>()
                  .Verify(s => s.Delete(1), Times.Once());
        }

        [Test]
        public void should_get_tag_details()
        {
            var details = Subject.Details(1);

            details.Id.Should().Be(1);
            details.Label.Should().Be("test-tag");
        }

        [Test]
        public void should_get_all_tag_details()
        {
            var details = Subject.Details();

            details.Should().HaveCount(1);
        }
    }
}
