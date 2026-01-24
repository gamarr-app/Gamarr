using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Profiles.Delay
{
    [TestFixture]
    public class DelayProfileServiceFixture : CoreTest<DelayProfileService>
    {
        private List<DelayProfile> _profiles;

        [SetUp]
        public void Setup()
        {
            _profiles = new List<DelayProfile>
            {
                new DelayProfile { Id = 1, Order = 0, Tags = new HashSet<int>() },
                new DelayProfile { Id = 2, Order = 1, Tags = new HashSet<int> { 1 } },
                new DelayProfile { Id = 3, Order = 2, Tags = new HashSet<int> { 2 } },
                new DelayProfile { Id = 4, Order = 3, Tags = new HashSet<int> { 3 } }
            };

            Mocker.GetMock<IDelayProfileRepository>()
                  .Setup(s => s.All())
                  .Returns(_profiles);

            Mocker.GetMock<ICacheManager>()
                  .Setup(s => s.GetCache<DelayProfile>(It.IsAny<System.Type>(), It.IsAny<string>()))
                  .Returns(new Cached<DelayProfile>());
        }

        [Test]
        public void should_reorder_profiles()
        {
            var result = Subject.Reorder(4, 1);

            result.Should().NotBeNull();
        }

        [Test]
        public void should_return_all_when_moving_unknown_id()
        {
            var result = Subject.Reorder(999, 1);

            result.Should().HaveCount(4);
        }

        [Test]
        public void should_add_profile_with_correct_order()
        {
            Mocker.GetMock<IDelayProfileRepository>()
                  .Setup(s => s.Count())
                  .Returns(4);

            Mocker.GetMock<IDelayProfileRepository>()
                  .Setup(s => s.Insert(It.IsAny<DelayProfile>()))
                  .Returns<DelayProfile>(p => p);

            var newProfile = new DelayProfile { Tags = new HashSet<int> { 5 } };

            var result = Subject.Add(newProfile);

            result.Order.Should().Be(4);
        }

        [Test]
        public void should_delete_profile_and_reorder()
        {
            Subject.Delete(2);

            Mocker.GetMock<IDelayProfileRepository>()
                  .Verify(s => s.Delete(2), Times.Once());

            Mocker.GetMock<IDelayProfileRepository>()
                  .Verify(s => s.UpdateMany(It.IsAny<List<DelayProfile>>()), Times.Once());
        }

        [Test]
        public void should_get_best_for_tags()
        {
            var result = Subject.BestForTags(new HashSet<int> { 1 });

            result.Should().NotBeNull();
        }

        [Test]
        public void should_get_all_for_tag()
        {
            var result = Subject.AllForTag(1);

            result.Should().HaveCount(1);
            result.First().Id.Should().Be(2);
        }

        [Test]
        public void should_get_all_for_tags_including_untagged()
        {
            var result = Subject.AllForTags(new HashSet<int> { 1 });

            result.Should().HaveCount(2);
        }
    }
}
