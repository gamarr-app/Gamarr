using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.MediaInfo
{
    [TestFixture]
    public class VideoFileInfoReaderFixture : CoreTest<VideoFileInfoReader>
    {
        [Test]
        public void get_runtime_should_return_null()
        {
            Subject.GetRunTime("any_path").Should().BeNull();
        }

        [Test]
        public void get_info_should_return_null()
        {
            Subject.GetMediaInfo("any_path").Should().BeNull();
        }
    }
}
