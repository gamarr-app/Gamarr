using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.RomCatalog
{
    public class GameFileHashCommand : Command
    {
        public int GameId { get; set; }

        public GameFileHashCommand()
        {
        }

        public GameFileHashCommand(int gameId)
        {
            GameId = gameId;
        }

        public override bool SendUpdatesToClient => false;

        // Hashing is the only genuinely expensive thing this command does, so
        // never let two of them (or a scan) grind the same disk in parallel —
        // five concurrent panel loads were what turned 43 s into 398 s (#205).
        public override bool RequiresDiskAccess => true;
        public override bool IsTypeExclusive => true;
        public override bool IsLongRunning => true;

        public override IEnumerable<string> GetValidationFailures()
        {
            if (GameId <= 0)
            {
                yield return "gameId must be greater than zero";
            }
        }
    }
}
