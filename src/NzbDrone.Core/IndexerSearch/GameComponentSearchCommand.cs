using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class GameComponentSearchCommand : Command
    {
        public int GameId { get; set; }
        public int ComponentId { get; set; }

        public override bool SendUpdatesToClient => true;
    }
}
