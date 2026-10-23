using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    /// <summary>
    /// Drains the set of games whose base file just landed and that are still
    /// owed an update search (#210). Deliberately carries no payload: the games
    /// live in <see cref="GameUpdateSearchService"/>, so one command drains
    /// however many a single import or library scan produced, one game at a
    /// time, instead of one command (and one executor thread) per game.
    /// </summary>
    public class GameUpdateSearchCommand : Command
    {
        public override bool SendUpdatesToClient => true;
    }
}
