using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata.Consumers.MediaBrowser
{
    public class MediaBrowserMetadata : MetadataBase<MediaBrowserMetadataSettings>
    {
        private readonly ILocalizationService _localizationService;
        private readonly Logger _logger;

        public MediaBrowserMetadata(ILocalizationService localizationService, Logger logger)
        {
            _localizationService = localizationService;
            _logger = logger;
        }

        public override string Name => "Emby (Legacy)";

        public override ProviderMessage Message => new (_localizationService.GetLocalizedString("MetadataMediaBrowserDeprecated", new Dictionary<string, object> { { "version", "v6" } }), ProviderMessageType.Warning);

        public override MetadataFile FindMetadataFile(Game game, string path)
        {
            var filename = Path.GetFileName(path);

            if (filename == null)
            {
                return null;
            }

            var metadata = new MetadataFile
            {
                GameId = game.Id,
                Consumer = GetType().Name,
                RelativePath = game.Path.GetRelativePath(path)
            };

            if (filename.Equals("game.xml", StringComparison.InvariantCultureIgnoreCase))
            {
                metadata.Type = MetadataType.GameMetadata;
                return metadata;
            }

            return null;
        }

        public override MetadataFileResult GameMetadata(Game game, GameFile gameFile)
        {
            if (!Settings.GameMetadata)
            {
                return null;
            }

            _logger.Debug("Generating game.xml for: {0}", game.Title);
            var sb = new StringBuilder();
            var xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;
            xws.Indent = false;

            using (var xw = XmlWriter.Create(sb, xws))
            {
                var gameElement = new XElement("Game");

                gameElement.Add(new XElement("id", game.GameMetadata.Value.IgdbId));
                gameElement.Add(new XElement("Status", game.GameMetadata.Value.Status));

                gameElement.Add(new XElement("Added", game.Added.ToString("MM/dd/yyyy HH:mm:ss tt")));
                gameElement.Add(new XElement("LockData", "false"));
                gameElement.Add(new XElement("Overview", game.GameMetadata.Value.Overview));
                gameElement.Add(new XElement("LocalTitle", game.Title));

                // Convert from 0-100 scale to 0-10 for MediaBrowser/Jellyfin
                gameElement.Add(new XElement("Rating", (game.GameMetadata.Value.Ratings.Igdb?.Value ?? 0) / 10));
                gameElement.Add(new XElement("ProductionYear", game.Year));
                gameElement.Add(new XElement("RunningTime", game.GameMetadata.Value.Runtime));
                gameElement.Add(new XElement("IgdbId", game.GameMetadata.Value.IgdbId));
                gameElement.Add(new XElement("Genres", game.GameMetadata.Value.Genres.Select(genre => new XElement("Genre", genre))));

                var doc = new XDocument(gameElement);
                doc.Save(xw);

                _logger.Debug("Saving game.xml for {0}", game.Title);

                return new MetadataFileResult("game.xml", doc.ToString());
            }
        }

        public override List<ImageFileResult> GameImages(Game game)
        {
            return new List<ImageFileResult>();
        }

        private IEnumerable<ImageFileResult> ProcessGameImages(Game game)
        {
            return new List<ImageFileResult>();
        }
    }
}
