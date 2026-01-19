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

            // Steam Store link
            if (linkType == MetadataLinkType.Steam && game.GameMetadata?.Value?.SteamId > 0)
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Steam, "Steam", $"https://store.steampowered.com/app/{game.GameMetadata.Value.SteamId}"));
            }

            // RAWG link
            if (linkType == MetadataLinkType.Rawg && game.GameMetadata?.Value?.RawgId > 0)
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Rawg, "RAWG", $"https://rawg.io/games/{game.GameMetadata.Value.RawgId}"));
            }
        }

        return links;
    }
}
