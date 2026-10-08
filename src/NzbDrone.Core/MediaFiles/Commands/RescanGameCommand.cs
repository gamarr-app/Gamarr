using System.Collections.Generic;
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

        public override IEnumerable<string> GetValidationFailures()
        {
            // Omitting gameId scans everything, which is valid; supplying 0 is not.
            if (GameId is <= 0)
            {
                yield return "gameId must be greater than zero when provided";
            }
        }
    }
}
