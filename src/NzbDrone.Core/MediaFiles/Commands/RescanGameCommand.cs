using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RescanGameCommand : Command
    {
        public int? GameId { get; set; }

        public override bool SendUpdatesToClient => true;

        public RescanGameCommand()
        {
        }

        public RescanGameCommand(int gameId)
        {
            GameId = gameId;
        }
    }
}
