using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.EnsureThat;

namespace NzbDrone.Common.Test.EnsureTests
{
    [TestFixture]
    public class EnsureCollectionExtensionsFixture
    {
        [Test]
        public void HasItems_List_should_pass_with_items()
        {
            var list = new List<string> { "item" };
            Action action = () => Ensure.That(list, () => list).HasItems();

            action.Should().NotThrow();
        }

        [Test]
        public void HasItems_List_should_throw_for_empty_list()
        {
            var list = new List<string>();
            Action action = () => Ensure.That(list, () => list).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_List_should_throw_for_null()
        {
            List<string> list = null;
            Action action = () => Ensure.That(list, () => list).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_Array_should_pass_with_items()
        {
            var array = new[] { "item" };
            Action action = () => Ensure.That(array, () => array).HasItems();

            action.Should().NotThrow();
        }

        [Test]
        public void HasItems_Array_should_throw_for_empty_array()
        {
            var array = Array.Empty<string>();
            Action action = () => Ensure.That(array, () => array).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_Array_should_throw_for_null()
        {
            string[] array = null;
            Action action = () => Ensure.That(array, () => array).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_IEnumerable_should_pass_with_items()
        {
            IEnumerable<string> enumerable = new[] { "item" };
            Action action = () => Ensure.That(enumerable, () => enumerable).HasItems();

            action.Should().NotThrow();
        }

        [Test]
        public void HasItems_IEnumerable_should_throw_for_empty()
        {
            IEnumerable<string> enumerable = Array.Empty<string>();
            Action action = () => Ensure.That(enumerable, () => enumerable).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_IEnumerable_should_throw_for_null()
        {
            IEnumerable<string> enumerable = null;
            Action action = () => Ensure.That(enumerable, () => enumerable).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_Collection_should_pass_with_items()
        {
            var collection = new Collection<string> { "item" };
            Action action = () => Ensure.That(collection, () => collection).HasItems();

            action.Should().NotThrow();
        }

        [Test]
        public void HasItems_Collection_should_throw_for_empty()
        {
            var collection = new Collection<string>();
            Action action = () => Ensure.That(collection, () => collection).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_Collection_should_throw_for_null()
        {
            Collection<string> collection = null;
            Action action = () => Ensure.That(collection, () => collection).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_Dictionary_should_pass_with_items()
        {
            IDictionary<string, int> dict = new Dictionary<string, int> { { "key", 1 } };
            Action action = () => Ensure.That(dict, () => dict).HasItems();

            action.Should().NotThrow();
        }

        [Test]
        public void HasItems_Dictionary_should_throw_for_empty()
        {
            IDictionary<string, int> dict = new Dictionary<string, int>();
            Action action = () => Ensure.That(dict, () => dict).HasItems();

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasItems_Dictionary_should_throw_for_null()
        {
            IDictionary<string, int> dict = null;
            Action action = () => Ensure.That(dict, () => dict).HasItems();

            action.Should().Throw<ArgumentException>();
        }
    }
}
