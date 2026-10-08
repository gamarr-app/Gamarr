using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class GamesSearchCommand : Command
    {
        public List<int> GameIds { get; set; }

        public override bool SendUpdatesToClient => true;

        public override IEnumerable<string> GetValidationFailures()
        {
            if (GameIds == null)
            {
                yield return "GameIds must be provided";
            }
        }
    }
}
