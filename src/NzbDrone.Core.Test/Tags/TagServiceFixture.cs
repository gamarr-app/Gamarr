using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Games;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Releases;
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

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AllGameTags())
                  .Returns(new Dictionary<int, List<int>>());

            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.All())
                  .Returns(new List<DelayProfile>());

            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.AllForTag(It.IsAny<int>()))
                  .Returns(new List<DelayProfile>());

            Mocker.GetMock<IImportListFactory>()
                  .Setup(s => s.All())
                  .Returns(new List<ImportListDefinition>());

            Mocker.GetMock<IImportListFactory>()
                  .Setup(s => s.AllForTag(It.IsAny<int>()))
                  .Returns(new List<ImportListDefinition>());

            Mocker.GetMock<INotificationFactory>()
                  .Setup(s => s.All())
                  .Returns(new List<NotificationDefinition>());

            Mocker.GetMock<INotificationFactory>()
                  .Setup(s => s.AllForTag(It.IsAny<int>()))
                  .Returns(new List<NotificationDefinition>());

            Mocker.GetMock<IReleaseProfileService>()
                  .Setup(s => s.All())
                  .Returns(new List<ReleaseProfile>());

            Mocker.GetMock<IReleaseProfileService>()
                  .Setup(s => s.AllForTag(It.IsAny<int>()))
                  .Returns(new List<ReleaseProfile>());

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.All())
                  .Returns(new List<IndexerDefinition>());

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.AllForTag(It.IsAny<int>()))
                  .Returns(new List<IndexerDefinition>());

            Mocker.GetMock<IAutoTaggingService>()
                  .Setup(s => s.All())
                  .Returns(new List<AutoTag>());

            Mocker.GetMock<IAutoTaggingService>()
                  .Setup(s => s.AllForTag(It.IsAny<int>()))
                  .Returns(new List<AutoTag>());

            Mocker.GetMock<IDownloadClientFactory>()
                  .Setup(s => s.AllForTag(It.IsAny<int>()))
                  .Returns(new List<DownloadClientDefinition>());

            Mocker.GetMock<IDownloadClientFactory>()
                  .Setup(s => s.All())
                  .Returns(new List<DownloadClientDefinition>());
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
