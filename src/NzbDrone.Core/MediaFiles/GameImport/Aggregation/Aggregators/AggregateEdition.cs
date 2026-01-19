using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators
{
    public class AggregateEdition : IAggregateLocalGame
    {
        public int Order => 1;

        public LocalGame Aggregate(LocalGame localGame, DownloadClientItem downloadClientItem)
        {
            var gameEdition = localGame.DownloadClientGameInfo?.Edition;

            if (gameEdition.IsNullOrWhiteSpace())
            {
                gameEdition = localGame.FolderGameInfo?.Edition;
            }

            if (gameEdition.IsNullOrWhiteSpace())
            {
                gameEdition = localGame.FileGameInfo?.Edition;
            }

            localGame.Edition = gameEdition;

            return localGame;
        }
    }
}
