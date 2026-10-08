using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class GameComponentSearchCommand : Command
    {
        public int GameId { get; set; }
        public int ComponentId { get; set; }

        public override bool SendUpdatesToClient => true;

        public override IEnumerable<string> GetValidationFailures()
        {
            if (ComponentId <= 0)
            {
                yield return "A componentId must be provided";
            }
        }
    }
}
