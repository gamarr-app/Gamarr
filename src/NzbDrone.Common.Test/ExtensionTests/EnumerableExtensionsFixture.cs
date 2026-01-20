using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Test.ExtensionTests
{
    [TestFixture]
    public class EnumerableExtensionsFixture
    {
        [Test]
        public void IntersectBy_should_return_matching_items()
        {
            var first = new[] { 1, 2, 3, 4 };
            var second = new[] { 2, 4, 6 };

            var result = first.IntersectBy(
                x => x,
                second,
                x => x,
                EqualityComparer<int>.Default).ToList();

            result.Should().HaveCount(2);
            result.Should().Contain(2);
            result.Should().Contain(4);
        }

        [Test]
        public void ExceptBy_should_return_non_matching_items()
        {
            var first = new[] { 1, 2, 3, 4 };
            var second = new[] { 2, 4, 6 };

            var result = first.ExceptBy(
                x => x,
                second,
                x => x,
                EqualityComparer<int>.Default).ToList();

            result.Should().HaveCount(2);
            result.Should().Contain(1);
            result.Should().Contain(3);
        }

        [Test]
        public void ToDictionaryIgnoreDuplicates_should_ignore_duplicates()
        {
            var source = new[] { "a", "b", "a", "c" };

            var result = source.ToDictionaryIgnoreDuplicates(x => x);

            result.Should().HaveCount(3);
        }

        [Test]
        public void ToDictionaryIgnoreDuplicates_with_value_selector_should_work()
        {
            var source = new[] { "a", "bb", "ccc" };

            var result = source.ToDictionaryIgnoreDuplicates(x => x, x => x.Length);

            result["a"].Should().Be(1);
            result["bb"].Should().Be(2);
            result["ccc"].Should().Be(3);
        }

        [Test]
        public void AddIfNotNull_should_add_non_null()
        {
            var list = new List<string>();
            list.AddIfNotNull("item");

            list.Should().HaveCount(1);
        }

        [Test]
        public void AddIfNotNull_should_not_add_null()
        {
            var list = new List<string>();
            list.AddIfNotNull(null);

            list.Should().BeEmpty();
        }

        [Test]
        public void Empty_should_return_true_for_empty()
        {
            var empty = Array.Empty<int>();

            empty.Empty().Should().BeTrue();
        }

        [Test]
        public void Empty_should_return_false_for_non_empty()
        {
            var items = new[] { 1, 2, 3 };

            items.Empty().Should().BeFalse();
        }

        [Test]
        public void None_should_return_true_when_no_match()
        {
            var items = new[] { 1, 2, 3 };

            items.None(x => x > 10).Should().BeTrue();
        }

        [Test]
        public void None_should_return_false_when_has_match()
        {
            var items = new[] { 1, 2, 3 };

            items.None(x => x > 2).Should().BeFalse();
        }

        [Test]
        public void NotAll_should_return_true_when_not_all_match()
        {
            var items = new[] { 1, 2, 3 };

            items.NotAll(x => x > 1).Should().BeTrue();
        }

        [Test]
        public void NotAll_should_return_false_when_all_match()
        {
            var items = new[] { 1, 2, 3 };

            items.NotAll(x => x > 0).Should().BeFalse();
        }

        [Test]
        public void SelectList_should_return_list()
        {
            var items = new[] { 1, 2, 3 };

            var result = items.SelectList(x => x * 2);

            result.Should().BeOfType<List<int>>();
            result.Should().ContainInOrder(2, 4, 6);
        }

        [Test]
        public void DropLast_should_drop_specified_number()
        {
            var items = new[] { 1, 2, 3, 4, 5 };

            var result = items.DropLast(2).ToList();

            result.Should().ContainInOrder(1, 2, 3);
        }

        [Test]
        public void DropLast_should_throw_for_null_source()
        {
            IEnumerable<int> items = null;

            Action action = () => items.DropLast(1).ToList();

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void DropLast_should_throw_for_negative_n()
        {
            var items = new[] { 1, 2, 3 };

            Action action = () => items.DropLast(-1).ToList();

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ConcatToString_should_join_with_separator()
        {
            var items = new[] { 1, 2, 3 };

            var result = items.ConcatToString();

            result.Should().Be("1, 2, 3");
        }

        [Test]
        public void ConcatToString_should_use_custom_separator()
        {
            var items = new[] { 1, 2, 3 };

            var result = items.ConcatToString("-");

            result.Should().Be("1-2-3");
        }

        [Test]
        public void ConcatToString_with_predicate_should_transform()
        {
            var items = new[] { "a", "bb", "ccc" };

            var result = items.ConcatToString(x => x.Length.ToString());

            result.Should().Be("1, 2, 3");
        }
    }
}
