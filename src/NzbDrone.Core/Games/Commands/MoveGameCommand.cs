using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Games.Commands
{
    public class MoveGameCommand : Command
    {
        public int GameId { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string DestinationRootFolder { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public override IEnumerable<string> GetValidationFailures()
        {
            if (GameId <= 0)
            {
                yield return "A gameId must be provided";
            }
        }
    }
}
