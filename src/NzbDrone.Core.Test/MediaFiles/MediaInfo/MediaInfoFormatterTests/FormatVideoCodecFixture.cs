using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaInfo.MediaInfoFormatterTests
{
    [TestFixture]
    public class FormatVideoCodecFixture : TestBase
    {
        [Test]
        public void should_return_empty_string()
        {
            var mediaInfoModel = new MediaInfoModel
            {
                VideoFormat = "h264",
                VideoCodecID = "avc1"
            };

            MediaInfoFormatter.FormatVideoCodec(mediaInfoModel, "scene").Should().Be(string.Empty);
        }
    }
}
