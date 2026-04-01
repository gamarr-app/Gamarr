using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaInfo.MediaInfoFormatterTests
{
    [TestFixture]
    public class FormatAudioCodecFixture : TestBase
    {
        [Test]
        public void should_return_empty_string()
        {
            var mediaInfoModel = new MediaInfoModel
            {
                AudioFormat = "aac",
                AudioCodecID = "mp4a",
                AudioProfile = "LC"
            };

            MediaInfoFormatter.FormatAudioCodec(mediaInfoModel, "scene").Should().Be(string.Empty);
        }
    }
}
