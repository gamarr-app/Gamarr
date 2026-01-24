using System.Collections.Generic;
using System.Data.SQLite;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Converters
{
    [TestFixture]
    public class PlatformFamilyListConverterFixture : CoreTest<EmbeddedDocumentConverter<List<PlatformFamily>>>
    {
        private SQLiteParameter _param;

        [SetUp]
        public void Setup()
        {
            _param = new SQLiteParameter();
        }

        [Test]
        public void should_serialize_platform_family_list()
        {
            var platforms = new List<PlatformFamily>
            {
                PlatformFamily.PC,
                PlatformFamily.PlayStation,
                PlatformFamily.Xbox
            };

            Subject.SetValue(_param, platforms);

            var result = (string)_param.Value;

            result.Should().Contain("pc");
            result.Should().Contain("playStation");
            result.Should().Contain("xbox");
        }

        [Test]
        public void should_deserialize_platform_family_list()
        {
            var json = "[\"pc\",\"playStation\",\"xbox\"]";

            var result = Subject.Parse(json);

            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain(PlatformFamily.PC);
            result.Should().Contain(PlatformFamily.PlayStation);
            result.Should().Contain(PlatformFamily.Xbox);
        }

        [Test]
        public void should_deserialize_empty_array()
        {
            var json = "[]";

            var result = Subject.Parse(json);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void should_deserialize_all_platform_families()
        {
            var json = "[\"pc\",\"playStation\",\"xbox\",\"nintendo\",\"linux\",\"mac\",\"mobile\"]";

            var result = Subject.Parse(json);

            result.Should().HaveCount(7);
            result.Should().Contain(PlatformFamily.PC);
            result.Should().Contain(PlatformFamily.PlayStation);
            result.Should().Contain(PlatformFamily.Xbox);
            result.Should().Contain(PlatformFamily.Nintendo);
            result.Should().Contain(PlatformFamily.Linux);
            result.Should().Contain(PlatformFamily.Mac);
            result.Should().Contain(PlatformFamily.Mobile);
        }

        [Test]
        public void should_round_trip_platform_family_list()
        {
            var platforms = new List<PlatformFamily>
            {
                PlatformFamily.Linux,
                PlatformFamily.Mac,
                PlatformFamily.Nintendo
            };

            Subject.SetValue(_param, platforms);

            var serialized = (string)_param.Value;
            var deserialized = Subject.Parse(serialized);

            deserialized.Should().BeEquivalentTo(platforms);
        }
    }
}
