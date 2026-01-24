using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.Steam.Resource;

namespace NzbDrone.Core.Test.MetadataSource.Steam
{
    [TestFixture]
    public class SteamRequirementsConverterFixture
    {
        private class TestWrapper
        {
            [JsonConverter(typeof(SteamRequirementsConverter))]
            public SteamPcRequirements Pc_Requirements { get; set; }
        }

        [Test]
        public void should_return_null_for_empty_array()
        {
            var json = "{\"Pc_Requirements\": []}";

            var result = JsonConvert.DeserializeObject<TestWrapper>(json);

            result.Pc_Requirements.Should().BeNull();
        }

        [Test]
        public void should_parse_object_with_requirements()
        {
            var json = "{\"Pc_Requirements\": {\"minimum\": \"Minimum: OS: Windows 10\", \"recommended\": \"Recommended: OS: Windows 11\"}}";

            var result = JsonConvert.DeserializeObject<TestWrapper>(json);

            result.Pc_Requirements.Should().NotBeNull();
            result.Pc_Requirements.Minimum.Should().Be("Minimum: OS: Windows 10");
            result.Pc_Requirements.Recommended.Should().Be("Recommended: OS: Windows 11");
        }

        [Test]
        public void should_return_null_for_null_value()
        {
            var json = "{\"Pc_Requirements\": null}";

            var result = JsonConvert.DeserializeObject<TestWrapper>(json);

            result.Pc_Requirements.Should().BeNull();
        }
    }
}
