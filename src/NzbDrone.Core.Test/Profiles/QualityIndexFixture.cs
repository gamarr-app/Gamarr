using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Test.Profiles
{
    [TestFixture]
    public class QualityIndexFixture
    {
        [Test]
        public void default_constructor_should_set_zero_values()
        {
            var index = new QualityIndex();

            index.Index.Should().Be(0);
            index.GroupIndex.Should().Be(0);
        }

        [Test]
        public void constructor_with_index_should_set_index()
        {
            var index = new QualityIndex(5);

            index.Index.Should().Be(5);
            index.GroupIndex.Should().Be(0);
        }

        [Test]
        public void constructor_with_both_should_set_both_values()
        {
            var index = new QualityIndex(5, 3);

            index.Index.Should().Be(5);
            index.GroupIndex.Should().Be(3);
        }

        [Test]
        public void CompareTo_should_return_positive_when_greater()
        {
            var left = new QualityIndex(5);
            var right = new QualityIndex(3);

            left.CompareTo(right).Should().BePositive();
        }

        [Test]
        public void CompareTo_should_return_negative_when_less()
        {
            var left = new QualityIndex(3);
            var right = new QualityIndex(5);

            left.CompareTo(right).Should().BeNegative();
        }

        [Test]
        public void CompareTo_should_return_zero_when_equal()
        {
            var left = new QualityIndex(5);
            var right = new QualityIndex(5);

            left.CompareTo(right).Should().Be(0);
        }

        [Test]
        public void CompareTo_should_compare_group_index_when_index_equal()
        {
            var left = new QualityIndex(5, 3);
            var right = new QualityIndex(5, 1);

            left.CompareTo(right, true).Should().BePositive();
        }

        [Test]
        public void CompareTo_should_not_compare_group_index_when_not_respecting()
        {
            var left = new QualityIndex(5, 3);
            var right = new QualityIndex(5, 1);

            left.CompareTo(right, false).Should().Be(0);
        }

        [Test]
        public void CompareTo_should_return_positive_when_compared_to_null()
        {
            var left = new QualityIndex(5);

            left.CompareTo(null).Should().BePositive();
        }

        [Test]
        public void CompareTo_object_should_work()
        {
            var left = new QualityIndex(5);
            object right = new QualityIndex(3);

            left.CompareTo(right).Should().BePositive();
        }

        [Test]
        public void should_be_able_to_set_properties()
        {
            var index = new QualityIndex
            {
                Index = 10,
                GroupIndex = 5
            };

            index.Index.Should().Be(10);
            index.GroupIndex.Should().Be(5);
        }
    }
}
