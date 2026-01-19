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

        public override bool UpdateScheduledTask => GameIds.Empty();

        public override bool IsLongRunning => true;

        public override string CompletionMessage => "Completed";
    }
}
