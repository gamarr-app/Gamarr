using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.OAuth;

namespace NzbDrone.Common.Test.OAuthTests
{
    [TestFixture]
    public class WebParameterCollectionFixture
    {
        [Test]
        public void should_create_empty_collection()
        {
            var collection = new WebParameterCollection();

            collection.Count.Should().Be(0);
        }

        [Test]
        public void should_create_collection_with_capacity()
        {
            var collection = new WebParameterCollection(10);

            collection.Count.Should().Be(0);
        }

        [Test]
        public void should_add_parameter_with_name_and_value()
        {
            var collection = new WebParameterCollection();
            collection.Add("name", "value");

            collection.Count.Should().Be(1);
            collection[0].Name.Should().Be("name");
            collection[0].Value.Should().Be("value");
        }

        [Test]
        public void should_add_web_parameter()
        {
            var collection = new WebParameterCollection();
            collection.Add(new WebParameter("name", "value"));

            collection.Count.Should().Be(1);
        }

        [Test]
        public void should_get_parameter_by_name()
        {
            var collection = new WebParameterCollection();
            collection.Add("name", "value");

            var param = collection["name"];

            param.Should().NotBeNull();
            param.Value.Should().Be("value");
        }

        [Test]
        public void should_return_null_for_nonexistent_name()
        {
            var collection = new WebParameterCollection();

            var param = collection["nonexistent"];

            param.Should().BeNull();
        }

        [Test]
        public void should_combine_values_for_duplicate_names()
        {
            var collection = new WebParameterCollection();
            collection.Add("name", "value1");
            collection.Add("name", "value2");

            var param = collection["name"];

            param.Value.Should().Contain("value1");
            param.Value.Should().Contain("value2");
        }

        [Test]
        public void should_get_names()
        {
            var collection = new WebParameterCollection();
            collection.Add("name1", "value1");
            collection.Add("name2", "value2");

            var names = collection.Names.ToList();

            names.Should().Contain("name1");
            names.Should().Contain("name2");
        }

        [Test]
        public void should_get_values()
        {
            var collection = new WebParameterCollection();
            collection.Add("name1", "value1");
            collection.Add("name2", "value2");

            var values = collection.Values.ToList();

            values.Should().Contain("value1");
            values.Should().Contain("value2");
        }

        [Test]
        public void should_create_from_enumerable()
        {
            var parameters = new List<WebParameter>
            {
                new WebParameter("name1", "value1"),
                new WebParameter("name2", "value2")
            };

            var collection = new WebParameterCollection(parameters);

            collection.Count.Should().Be(2);
        }

        [Test]
        public void should_create_from_name_value_collection()
        {
            var nvc = new NameValueCollection
            {
                { "name1", "value1" },
                { "name2", "value2" }
            };

            var collection = new WebParameterCollection(nvc);

            collection.Count.Should().Be(2);
        }

        [Test]
        public void should_create_from_dictionary()
        {
            var dict = new Dictionary<string, string>
            {
                { "name1", "value1" },
                { "name2", "value2" }
            };

            var collection = new WebParameterCollection(dict);

            collection.Count.Should().Be(2);
        }

        [Test]
        public void should_add_range_from_name_value_collection()
        {
            var collection = new WebParameterCollection();
            var nvc = new NameValueCollection
            {
                { "name1", "value1" }
            };

            collection.AddRange(nvc);

            collection.Count.Should().Be(1);
        }

        [Test]
        public void should_add_range_from_web_parameter_collection()
        {
            var collection = new WebParameterCollection();
            var other = new WebParameterCollection();
            other.Add("name1", "value1");

            collection.AddRange(other);

            collection.Count.Should().Be(1);
        }

        [Test]
        public void should_add_range_from_enumerable()
        {
            var collection = new WebParameterCollection();
            var parameters = new List<WebParameter>
            {
                new WebParameter("name1", "value1")
            };

            collection.AddRange(parameters);

            collection.Count.Should().Be(1);
        }

        [Test]
        public void should_add_collection_from_dictionary()
        {
            var collection = new WebParameterCollection();
            var dict = new Dictionary<string, string>
            {
                { "name1", "value1" }
            };

            collection.AddCollection(dict);

            collection.Count.Should().Be(1);
        }

        [Test]
        public void should_sort_parameters()
        {
            var collection = new WebParameterCollection();
            collection.Add("z_name", "value");
            collection.Add("a_name", "value");

            collection.Sort((x, y) => x.Name.CompareTo(y.Name));

            collection[0].Name.Should().Be("a_name");
            collection[1].Name.Should().Be("z_name");
        }

        [Test]
        public void should_remove_all_matching_parameters()
        {
            var collection = new WebParameterCollection();
            var param1 = new WebParameter("name1", "value1");
            var param2 = new WebParameter("name2", "value2");
            collection.Add(param1);
            collection.Add(param2);

            var result = collection.RemoveAll(new[] { param1 });

            result.Should().BeTrue();
            collection.Count.Should().Be(1);
        }

        [Test]
        public void should_clear_collection()
        {
            var collection = new WebParameterCollection();
            collection.Add("name", "value");

            collection.Clear();

            collection.Count.Should().Be(0);
        }

        [Test]
        public void should_check_contains()
        {
            var collection = new WebParameterCollection();
            var param = new WebParameter("name", "value");
            collection.Add(param);

            collection.Contains(param).Should().BeTrue();
        }

        [Test]
        public void should_copy_to_array()
        {
            var collection = new WebParameterCollection();
            collection.Add("name", "value");
            var array = new WebParameter[1];

            collection.CopyTo(array, 0);

            array[0].Should().NotBeNull();
        }

        [Test]
        public void should_remove_parameter()
        {
            var collection = new WebParameterCollection();
            var param = new WebParameter("name", "value");
            collection.Add(param);

            var result = collection.Remove(param);

            result.Should().BeTrue();
            collection.Count.Should().Be(0);
        }

        [Test]
        public void should_report_not_readonly()
        {
            var collection = new WebParameterCollection();

            collection.IsReadOnly.Should().BeFalse();
        }

        [Test]
        public void should_get_index_of()
        {
            var collection = new WebParameterCollection();
            var param = new WebParameter("name", "value");
            collection.Add(param);

            collection.IndexOf(param).Should().Be(0);
        }

        [Test]
        public void should_insert_at_index()
        {
            var collection = new WebParameterCollection();
            collection.Add("name2", "value2");

            collection.Insert(0, new WebParameter("name1", "value1"));

            collection[0].Name.Should().Be("name1");
        }

        [Test]
        public void should_remove_at_index()
        {
            var collection = new WebParameterCollection();
            collection.Add("name1", "value1");
            collection.Add("name2", "value2");

            collection.RemoveAt(0);

            collection.Count.Should().Be(1);
            collection[0].Name.Should().Be("name2");
        }

        [Test]
        public void should_get_and_set_by_index()
        {
            var collection = new WebParameterCollection();
            collection.Add("name1", "value1");

            collection[0] = new WebParameter("name2", "value2");

            collection[0].Name.Should().Be("name2");
        }

        [Test]
        public void should_enumerate()
        {
            var collection = new WebParameterCollection();
            collection.Add("name1", "value1");
            collection.Add("name2", "value2");

            var count = 0;
            foreach (var param in collection)
            {
                count++;
                param.Should().NotBeNull();
            }

            count.Should().Be(2);
        }
    }
}
