using System;
using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Games.Commands
{
    public class BulkMoveGameCommand : Command
    {
        public List<BulkMoveGame> Games { get; set; }
        public string DestinationRootFolder { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }

    public class BulkMoveGame : IEquatable<BulkMoveGame>
    {
        public int GameId { get; set; }
        public string SourcePath { get; set; }

        public bool Equals(BulkMoveGame other)
        {
            if (other == null)
            {
                return false;
            }

            return GameId.Equals(other.GameId);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return GameId.Equals(((BulkMoveGame)obj).GameId);
        }

        public override int GetHashCode()
        {
            return GameId.GetHashCode();
        }
    }
}
