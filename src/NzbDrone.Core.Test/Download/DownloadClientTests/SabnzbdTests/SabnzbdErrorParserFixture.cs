using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.Clients.Sabnzbd;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.SabnzbdTests
{
    [TestFixture]
    public class SabnzbdErrorParserFixture
    {
        // The strings below are what a real SABnzbd sends; they are the only contract the
        // client has, since the API offers no error codes.
        [TestCase("API Key Incorrect", SabnzbdAuthError.ApiKeyIncorrect)]
        [TestCase("api key incorrect", SabnzbdAuthError.ApiKeyIncorrect)]
        [TestCase("API Key Required", SabnzbdAuthError.ApiKeyRequired)]
        [TestCase("error: API Key Required", SabnzbdAuthError.ApiKeyRequired)]
        [TestCase("Some other failure", SabnzbdAuthError.None)]
        [TestCase("", SabnzbdAuthError.None)]
        [TestCase(null, SabnzbdAuthError.None)]
        public void should_parse_auth_error(string error, SabnzbdAuthError expected)
        {
            SabnzbdErrorParser.ParseAuthError(error).Should().Be(expected);
        }

        [TestCase("Unpacking failed, write error or disk is full?", true)]
        [TestCase("unpacking failed, write error or disk is full?", true)]
        [TestCase("Unpacking failed, archive is damaged", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void should_detect_unpack_write_error(string failMessage, bool expected)
        {
            SabnzbdErrorParser.IsUnpackWriteError(failMessage).Should().Be(expected);
        }
    }
}
