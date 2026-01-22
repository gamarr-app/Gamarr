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
            var igdbSlug = game.GameMetadata?.Value?.IgdbSlug;
            if (linkType == MetadataLinkType.Igdb && !string.IsNullOrEmpty(igdbSlug))
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Igdb, "IGDB", $"https://www.igdb.com/games/{igdbSlug}"));
            }

            // Steam Store link
            if (linkType == MetadataLinkType.Steam && game.GameMetadata?.Value?.SteamAppId > 0)
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Steam, "Steam", $"https://store.steampowered.com/app/{game.GameMetadata.Value.SteamAppId}"));
            }

            // RAWG link
            {
            }
        }

        return links;
    }
}
