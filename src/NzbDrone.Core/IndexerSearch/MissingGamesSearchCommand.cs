using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class MissingGamesSearchCommand : Command
    {
        public override bool SendUpdatesToClient => true;
        public string FilterKey { get; set; }
        public string FilterValue { get; set; }
    }
}
