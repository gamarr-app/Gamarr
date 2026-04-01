using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaInfo.MediaInfoFormatterTests
{
    [TestFixture]
    public class FormatVideoDynamicRangeFixture : TestBase
    {
        [Test]
        public void should_return_empty_string()
        {
            var mediaInfo = new MediaInfoModel
            {
                VideoHdrFormat = HdrFormat.Hdr10,
                SchemaRevision = 8
            };

            MediaInfoFormatter.FormatVideoDynamicRange(mediaInfo).Should().Be(string.Empty);
        }
    }
}
