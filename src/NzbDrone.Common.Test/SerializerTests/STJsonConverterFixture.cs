using System;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Common.Test.SerializerTests
{
    [TestFixture]
    public class STJsonConverterFixture
    {
        public class Model
        {
            public DateTime Added { get; set; }
            public DateTime? LastSearch { get; set; }
            public TimeSpan Duration { get; set; }
        }

        [Test]
        public void should_read_null_for_non_nullable_values_as_default()
        {
            var result = STJson.Deserialize<Model>("{\"added\":null,\"lastSearch\":null,\"duration\":null}");

            result.Added.Should().Be(default(DateTime));
            result.LastSearch.Should().BeNull();
            result.Duration.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void should_read_valid_values()
        {
            var result = STJson.Deserialize<Model>("{\"added\":\"2026-11-23T16:23:09Z\",\"duration\":\"01:02:03\"}");

            result.Added.Should().Be(new DateTime(2026, 11, 23, 16, 23, 9, DateTimeKind.Utc));
            result.Duration.Should().Be(new TimeSpan(1, 2, 3));
        }

        [TestCase("{\"added\":\"not a date\"}")]
        [TestCase("{\"added\":12}")]
        [TestCase("{\"duration\":\"nope\"}")]
        public void should_throw_json_exception_for_bad_values(string json)
        {
            Assert.Throws<JsonException>(() => STJson.Deserialize<Model>(json));
        }
    }
}
