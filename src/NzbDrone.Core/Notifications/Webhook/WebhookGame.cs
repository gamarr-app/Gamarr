using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookGame
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public string FilePath { get; set; }
        public string ReleaseDate { get; set; }
        public string FolderPath { get; set; }
        public int IgdbId { get; set; }
        public string ImdbId { get; set; }
        public string Overview { get; set; }
        public List<string> Genres { get; set; }
        public List<WebhookImage> Images { get; set; }
        public List<string> Tags { get; set; }
        public Language OriginalLanguage { get; set; }

        public WebhookGame()
        {
        }

        public WebhookGame(Game game, List<string> tags)
        {
            Id = game.Id;
            Title = game.Title;
            Year = game.Year;
            ReleaseDate = game.GameMetadata.Value.PhysicalReleaseDate().ToString("yyyy-MM-dd");
            FolderPath = game.Path;
            IgdbId = game.IgdbId;
            ImdbId = game.ImdbId;
            Overview = game.GameMetadata.Value.Overview;
            Genres = game.GameMetadata.Value.Genres;
            Images = game.GameMetadata.Value.Images.Select(i => new WebhookImage(i)).ToList();
            Tags = tags;
            OriginalLanguage = game.GameMetadata.Value.OriginalLanguage;
        }

        public WebhookGame(Game game, GameFile gameFile, List<string> tags)
            : this(game, tags)
        {
            FilePath = Path.Combine(game.Path, gameFile.RelativePath);
        }
    }
}
