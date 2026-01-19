using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MediaInfo;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookGameFileMediaInfo
    {
        public WebhookGameFileMediaInfo()
        {
        }

        public WebhookGameFileMediaInfo(GameFile gameFile)
        {
            AudioChannels = MediaInfoFormatter.FormatAudioChannels(gameFile.MediaInfo);
            AudioCodec = MediaInfoFormatter.FormatAudioCodec(gameFile.MediaInfo, gameFile.SceneName);
            AudioLanguages = gameFile.MediaInfo.AudioLanguages.Distinct().ToList();
            Height = gameFile.MediaInfo.Height;
            Width = gameFile.MediaInfo.Width;
            Subtitles = gameFile.MediaInfo.Subtitles.Distinct().ToList();
            VideoCodec = MediaInfoFormatter.FormatVideoCodec(gameFile.MediaInfo, gameFile.SceneName);
            VideoDynamicRange = MediaInfoFormatter.FormatVideoDynamicRange(gameFile.MediaInfo);
            VideoDynamicRangeType = MediaInfoFormatter.FormatVideoDynamicRangeType(gameFile.MediaInfo);
        }

        public decimal AudioChannels { get; set; }
        public string AudioCodec { get; set; }
        public List<string> AudioLanguages { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public List<string> Subtitles { get; set; }
        public string VideoCodec { get; set; }
        public string VideoDynamicRange { get; set; }
        public string VideoDynamicRangeType { get; set; }
    }
}
