using System.Collections.Generic;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications;

public static class NotificationMetadataLinkGenerator
{
    public static List<NotificationMetadataLink> GenerateLinks(Game game, IEnumerable<int> metadataLinks)
    {
        var links = new List<NotificationMetadataLink>();

        if (game == null)
        {
            return links;
        }

        foreach (var type in metadataLinks)
        {
            var linkType = (MetadataLinkType)type;

            // IGDB - Internet Game Database (primary link for games)
            if (linkType == MetadataLinkType.Igdb && game.IgdbId > 0)
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Igdb, "IGDB", $"https://www.igdb.com/games/{game.IgdbId}"));
            }

            // IMDb links are deprecated for games - IMDb is a movie database
            // Keeping for backwards compatibility but not generating links
            // if (linkType == MetadataLinkType.Imdb) { /* Deprecated - no-op */ }

            // Trakt link for games
            if (linkType == MetadataLinkType.Trakt && game.IgdbId > 0)
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Trakt, "Trakt", $"https://trakt.tv/search/igdb/{game.IgdbId}?id_type=game"));
            }
        }

        return links;
    }
}
