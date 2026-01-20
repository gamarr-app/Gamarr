using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Test.ExtensionTests
{
    [TestFixture]
    public class DictionaryExtensionsFixture
    {
        [Test]
        public void Merge_should_combine_two_dictionaries()
        {
            var first = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            var second = new Dictionary<string, int> { { "c", 3 }, { "d", 4 } };

            var result = first.Merge(second);

            result.Should().HaveCount(4);
            result["a"].Should().Be(1);
            result["c"].Should().Be(3);
        }

        [Test]
        public void Merge_should_overwrite_duplicate_keys()
        {
            var first = new Dictionary<string, int> { { "key", 1 } };
            var second = new Dictionary<string, int> { { "key", 2 } };

            var result = first.Merge(second);

            result.Should().HaveCount(1);
            result["key"].Should().Be(2);
        }

        [Test]
        public void Merge_should_throw_for_null_first()
        {
            Dictionary<string, int> first = null;
            var second = new Dictionary<string, int>();

            Action action = () => first.Merge(second);

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Merge_should_throw_for_null_second()
        {
            var first = new Dictionary<string, int>();
            Dictionary<string, int> second = null;

            Action action = () => first.Merge(second);

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Merge_should_return_new_dictionary()
        {
            var first = new Dictionary<string, int> { { "a", 1 } };
            var second = new Dictionary<string, int> { { "b", 2 } };

            var result = first.Merge(second);

            result.Should().NotBeSameAs(first);
            result.Should().NotBeSameAs(second);
        }

        [Test]
        public void Add_should_add_key_value_pair()
        {
            ICollection<KeyValuePair<string, int>> collection = new List<KeyValuePair<string, int>>();

            collection.Add("key", 42);

            collection.Should().HaveCount(1);
        }

        [Test]
        public void SelectDictionary_should_transform_keys_and_values()
        {
            var source = new Dictionary<int, string>
            {
                { 1, "one" },
                { 2, "two" }
            };

            var result = source.SelectDictionary(kv => (kv.Key.ToString(), kv.Value.Length));

            result.Should().HaveCount(2);
            result["1"].Should().Be(3);
            result["2"].Should().Be(3);
        }

        [Test]
        public void SelectDictionary_with_selectors_should_transform()
        {
            var source = new Dictionary<int, string>
            {
                { 1, "one" },
                { 2, "two" }
            };

            var result = source.SelectDictionary(
                kv => kv.Key * 10,
                kv => kv.Value.ToUpper());

            result.Should().HaveCount(2);
            result[10].Should().Be("ONE");
            result[20].Should().Be("TWO");
        }
    }
}
