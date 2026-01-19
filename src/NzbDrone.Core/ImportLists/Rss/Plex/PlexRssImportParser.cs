#pragma warning disable CS0618

using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.ImportLists.Rss.Plex
{
    public class PlexRssImportParser : RssImportBaseParser
    {
        private readonly Logger _logger;

        public PlexRssImportParser(Logger logger)
            : base(logger)
        {
            _logger = logger;
        }

        protected override ImportListGame ProcessItem(XElement item)
        {
            var category = item.TryGetValue("category");

            if (category != "game")
            {
                return null;
            }

            var info = new ImportListGame
            {
                Title = item.TryGetValue("title", "Unknown")
            };

            var guid = item.TryGetValue("guid", string.Empty);

            if (guid.IsNotNullOrWhiteSpace())
            {
                if (guid.StartsWith("imdb://"))
                {
                    info.ImdbId = Parser.Parser.ParseImdbId(guid.Replace("imdb://", ""));
                }

                if (int.TryParse(guid.Replace("igdb://", ""), out var igdbId))
                {
                    info.IgdbId = igdbId;
                }
            }

            if (info.ImdbId.IsNullOrWhiteSpace() && info.IgdbId == 0)
            {
                _logger.Warn("Each item in the RSS feed must have a guid element with a IMDB ID or IGDB ID: '{0}'", info.Title);

                return null;
            }

            return info;
        }
    }
}
