using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Profiles.Delay
{
    [TestFixture]
    public class DelayProfileServiceFixture : CoreTest<DelayProfileService>
    {
        private List<DelayProfile> _delayProfiles;
        private DelayProfile _first;
        private DelayProfile _last;

        [SetUp]
        public void Setup()
        {
            _delayProfiles = Builder<DelayProfile>.CreateListOfSize(4)
                                                  .TheFirst(1)
                                                  .With(d => d.Order = int.MaxValue)
                                                  .TheNext(1)
                                                  .With(d => d.Order = 1)
                                                  .TheNext(1)
                                                  .With(d => d.Order = 2)
                                                  .TheNext(1)
                                                  .With(d => d.Order = 3)
                                                  .Build()
                                                  .ToList();

            _first = _delayProfiles[1];
            _last = _delayProfiles.Last();

            Mocker.GetMock<IDelayProfileRepository>()
                  .Setup(s => s.All())
                  .Returns(_delayProfiles);
        }

        [Test]
        public void should_move_to_first_if_afterId_is_null()
        {
            var moving = _last;
            var result = Subject.Reorder(moving.Id, null).OrderBy(d => d.Order).ToList();
            var moved = result.First();

            moved.Id.Should().Be(moving.Id);
            moved.Order.Should().Be(1);
        }

        [Test]
        public void should_move_after_if_afterId_is_not_null()
        {
            var after = _first;
            var moving = _last;
            var result = Subject.Reorder(moving.Id, _first.Id).OrderBy(d => d.Order).ToList();
            var moved = result[1];

            moved.Id.Should().Be(moving.Id);
            moved.Order.Should().Be(after.Order + 1);
        }

        [Test]
        public void should_reorder_delay_profiles_that_are_after_moved()
        {
            var moving = _last;
            var result = Subject.Reorder(moving.Id, null).OrderBy(d => d.Order).ToList();

            for (var i = 1; i < result.Count; i++)
            {
                var delayProfile = result[i];

                if (delayProfile.Id == 1)
                {
                    delayProfile.Order.Should().Be(int.MaxValue);
                }
                else
                {
                    delayProfile.Order.Should().Be(i + 1);
                }
            }
        }

        [Test]
        public void should_not_change_afters_order_if_moving_was_after()
        {
            var after = _first;
            var afterOrder = after.Order;
            var moving = _last;
            var result = Subject.Reorder(moving.Id, _first.Id).OrderBy(d => d.Order).ToList();
            var afterMove = result.First();

            afterMove.Id.Should().Be(after.Id);
            afterMove.Order.Should().Be(afterOrder);
        }

        [Test]
        public void should_change_afters_order_if_moving_was_before()
        {
            var after = _last;
            var afterOrder = after.Order;
            var moving = _first;

            var result = Subject.Reorder(moving.Id, after.Id).OrderBy(d => d.Order).ToList();
            var afterMove = result.Single(d => d.Id == after.Id);

            afterMove.Order.Should().BeLessThan(afterOrder);
        }

        [Test]
        public void should_return_all_profiles()
        {
            Subject.All().Should().HaveCount(4);
        }

        [Test]
        public void should_return_profiles_in_order()
        {
            var profiles = Subject.All();

            profiles.Should().BeInAscendingOrder(p => p.Order);
        }

        [Test]
        public void should_get_profile_by_id()
        {
            Mocker.GetMock<IDelayProfileRepository>()
                  .Setup(s => s.Get(2))
                  .Returns(_first);

            Subject.Get(2).Should().Be(_first);
        }

        [Test]
        public void should_get_best_for_tags_returns_default_when_no_match()
        {
            // All tags should match the default profile (order = int.MaxValue)
            var result = Subject.BestForTags(new HashSet<int> { 999 });

            result.Order.Should().Be(int.MaxValue);
        }

        [Test]
        public void should_add_profile()
        {
            var newProfile = new DelayProfile
            {
                PreferredProtocol = Indexers.DownloadProtocol.Usenet,
                Tags = new HashSet<int>()
            };

            Mocker.GetMock<IDelayProfileRepository>()
                  .Setup(s => s.Insert(newProfile))
                  .Returns(newProfile);

            Subject.Add(newProfile);

            Mocker.GetMock<IDelayProfileRepository>()
                  .Verify(s => s.Insert(newProfile), Moq.Times.Once());
        }

        [Test]
        public void should_update_profile()
        {
            Subject.Update(_first);

            Mocker.GetMock<IDelayProfileRepository>()
                  .Verify(s => s.Update(_first), Moq.Times.Once());
        }

        [Test]
        public void should_delete_profile()
        {
            Subject.Delete(2);

            Mocker.GetMock<IDelayProfileRepository>()
                  .Verify(s => s.Delete(2), Moq.Times.Once());
        }
    }
}
