using System.Collections.Generic;
using NzbDrone.Common.Extensions;
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

            if (linkType == MetadataLinkType.Imdb && game.ImdbId.IsNotNullOrWhiteSpace())
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Imdb, "IMDb", $"https://www.imdb.com/title/{game.ImdbId}"));
            }

            if (linkType == MetadataLinkType.Igdb && game.IgdbId > 0)
            {
                links.Add(new NotificationMetadataLink(MetadataLinkType.Igdb, "TMDb", $"https://www.thegamedb.org/game/{game.IgdbId}"));
            }
        }

        return links;
    }
}
