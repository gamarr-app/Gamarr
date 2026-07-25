using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MetadataSource.Steam;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.Steam
{
    [TestFixture]
    public class SteamDlcForAppParserFixture : CoreTest
    {
        [Test]
        public void should_parse_dlc_ids_and_names()
        {
            var json = @"{""status"":1,""appid"":""1145350"",""name"":""Hades II"",""dlc"":[
                {""id"":2950840,""name"":""Hades II Original Soundtrack"",""header_image"":""https://example/img.jpg""},
                {""id"":2950841,""name"":""Hades II Artbook""}]}";

            var result = SteamStoreProxy.ParseDlcForApp(json);

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(2950840);
            result[0].Name.Should().Be("Hades II Original Soundtrack");
            result[0].Source.Should().Be(DlcReference.SteamSource);
        }

        [Test]
        public void should_return_empty_for_failed_status()
        {
            SteamStoreProxy.ParseDlcForApp(@"{""status"":0}").Should().BeEmpty();
        }

        [Test]
        public void should_return_empty_when_dlc_array_missing()
        {
            SteamStoreProxy.ParseDlcForApp(@"{""status"":1,""name"":""Some Game""}").Should().BeEmpty();
        }

        [Test]
        public void should_skip_entries_without_id_or_name()
        {
            var json = @"{""status"":1,""dlc"":[
                {""id"":0,""name"":""Broken""},
                {""name"":""No Id""},
                {""id"":123},
                {""id"":456,""name"":""Valid DLC""}]}";

            var result = SteamStoreProxy.ParseDlcForApp(json);

            result.Should().ContainSingle(d => d.Id == 456 && d.Name == "Valid DLC");
        }
    }
}
