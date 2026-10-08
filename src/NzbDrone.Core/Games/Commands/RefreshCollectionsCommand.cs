using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Games.Commands
{
    public class RefreshCollectionsCommand : Command
    {
        public List<int> CollectionIds { get; set; }

        public RefreshCollectionsCommand()
        {
            CollectionIds = new List<int>();
        }

        public RefreshCollectionsCommand(List<int> collectionIds)
        {
            CollectionIds = collectionIds;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => CollectionIds == null || CollectionIds.Empty();

        public override bool IsLongRunning => true;

        public override string CompletionMessage => "Completed";

        public override IEnumerable<string> GetValidationFailures()
        {
            // The ctor defaults this to an empty list, but an explicit
            // "collectionIds": null in the request body overwrites it.
            if (CollectionIds == null)
            {
                yield return "CollectionIds must not be null";
            }
        }
    }
}
