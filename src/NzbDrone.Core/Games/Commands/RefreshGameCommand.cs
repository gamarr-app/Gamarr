using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Games.Commands
{
    public class RefreshGameCommand : Command
    {
        public List<int> GameIds { get; set; }
        public bool IsNewGame { get; set; }

        public RefreshGameCommand()
        {
            GameIds = new List<int>();
        }

        public RefreshGameCommand(List<int> gameIds, bool isNewGame = false)
        {
            GameIds = gameIds;
            IsNewGame = isNewGame;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => GameIds == null || GameIds.Empty();

        public override bool IsLongRunning => true;

        public override string CompletionMessage => "Completed";

        public override IEnumerable<string> GetValidationFailures()
        {
            // The ctor defaults this to an empty list, but an explicit
            // "gameIds": null in the request body overwrites it.
            if (GameIds == null)
            {
                yield return "GameIds must not be null";
            }
        }
    }
}
