using System;
using System.Collections.Generic;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookGameFile
    {
        public WebhookGameFile()
        {
        }

        public WebhookGameFile(GameFile gameFile)
        {
            Id = gameFile.Id;
            RelativePath = gameFile.RelativePath;
            Path = System.IO.Path.Combine(gameFile.Game.Path, gameFile.RelativePath);
            Quality = gameFile.Quality.Quality.Name;
            QualityVersion = gameFile.Quality.Revision.Version;
            ReleaseGroup = gameFile.ReleaseGroup;
            SceneName = gameFile.SceneName;
            IndexerFlags = gameFile.IndexerFlags.ToString();
            Size = gameFile.Size;
            DateAdded = gameFile.DateAdded;
            Languages = gameFile.Languages;

            if (gameFile.MediaInfo != null)
            {
                MediaInfo = new WebhookGameFileMediaInfo(gameFile);
            }
        }

        public int Id { get; set; }
        public string RelativePath { get; set; }
        public string Path { get; set; }
        public string Quality { get; set; }
        public int QualityVersion { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }
        public string IndexerFlags { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
        public List<Language> Languages { get; set; }
        public WebhookGameFileMediaInfo MediaInfo { get; set; }
        public string SourcePath { get; set; }
        public string RecycleBinPath { get; set; }
    }
}
