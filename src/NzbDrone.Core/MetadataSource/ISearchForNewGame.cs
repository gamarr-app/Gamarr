using System.Collections.Generic;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewGame
    {
        List<Game> SearchForNewGame(string title);

        GameMetadata MapGameToIgdbGame(GameMetadata game);
    }
}
