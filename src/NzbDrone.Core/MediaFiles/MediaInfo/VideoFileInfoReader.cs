using System;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public interface IVideoFileInfoReader
    {
        MediaInfoModel GetMediaInfo(string filename);
        TimeSpan? GetRunTime(string filename);
    }

    public class VideoFileInfoReader : IVideoFileInfoReader
    {
        public const int MINIMUM_MEDIA_INFO_SCHEMA_REVISION = 14;
        public const int CURRENT_MEDIA_INFO_SCHEMA_REVISION = 14;

        public MediaInfoModel GetMediaInfo(string filename)
        {
            return null;
        }

        public TimeSpan? GetRunTime(string filename)
        {
            return null;
        }
    }
}
