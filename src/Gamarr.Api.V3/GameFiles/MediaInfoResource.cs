using NzbDrone.Core.MediaFiles.MediaInfo;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.GameFiles
{
    public class MediaInfoResource : RestResource
    {
        public long AudioBitrate { get; set; }
        public decimal AudioChannels { get; set; }
        public string AudioCodec { get; set; }
        public string AudioLanguages { get; set; }
        public int AudioStreamCount { get; set; }
        public int VideoBitDepth { get; set; }
        public long VideoBitrate { get; set; }
        public string VideoCodec { get; set; }
        public decimal VideoFps { get; set; }
        public string VideoDynamicRange { get; set; }
        public string VideoDynamicRangeType { get; set; }
        public string Resolution { get; set; }
        public string RunTime { get; set; }
        public string ScanType { get; set; }
        public string Subtitles { get; set; }
    }

    public static class MediaInfoResourceMapper
    {
        public static MediaInfoResource ToResource(this MediaInfoModel model, string sceneName)
        {
            if (model == null)
            {
                return null;
            }

            // Return empty/default values - media info scanning is not applicable for game files
            return new MediaInfoResource
            {
                AudioBitrate = 0,
                AudioChannels = 0,
                AudioLanguages = string.Empty,
                AudioStreamCount = 0,
                AudioCodec = string.Empty,
                VideoBitDepth = 0,
                VideoBitrate = 0,
                VideoCodec = string.Empty,
                VideoFps = 0,
                VideoDynamicRange = string.Empty,
                VideoDynamicRangeType = string.Empty,
                Resolution = string.Empty,
                RunTime = string.Empty,
                ScanType = string.Empty,
                Subtitles = string.Empty
            };
        }
    }
}
