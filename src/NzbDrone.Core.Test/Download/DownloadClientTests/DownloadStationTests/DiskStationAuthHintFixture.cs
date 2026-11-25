using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.Clients.DownloadStation;
using NzbDrone.Core.Download.Clients.DownloadStation.Responses;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.DownloadStationTests
{
    [TestFixture]
    public class DiskStationAuthHintFixture
    {
        [TestCase(400)]
        [TestCase(401)]
        [TestCase(402)]
        public void should_not_mention_two_step_for_account_errors(int code)
        {
            var hint = DiskStationError.GetAuthHint(code);

            hint.Should().NotBeNullOrEmpty();
            hint.Should().NotContain("2-step");
            new DiskStationError { Code = code }.GetMessage(DiskStationApi.Auth).Should().NotContain("2-step");
        }

        [TestCase(403)]
        [TestCase(404)]
        [TestCase(406)]
        public void should_mention_two_step_for_otp_errors(int code)
        {
            DiskStationError.GetAuthHint(code).Should().Contain("2-step");
            new DiskStationError { Code = code }.GetMessage(DiskStationApi.Auth).Should().Contain("2-step");
        }

        [Test]
        public void should_have_distinct_messages_for_each_documented_code()
        {
            var codes = new[] { 400, 401, 402, 403, 404, 406 };
            var messages = new System.Collections.Generic.HashSet<string>();

            foreach (var code in codes)
            {
                messages.Add(new DiskStationError { Code = code }.GetMessage(DiskStationApi.Auth)).Should().BeTrue();
            }
        }
    }
}
