using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Games.Events
{
    public class GamesDeletedEvent : IEvent
    {
        public List<Game> Games { get; private set; }
        public bool DeleteFiles { get; private set; }
        public bool AddImportListExclusion { get; private set; }

        public GamesDeletedEvent(List<Game> games, bool deleteFiles, bool addImportListExclusion)
        {
            Games = games;
            DeleteFiles = deleteFiles;
            AddImportListExclusion = addImportListExclusion;
        }
    }
}
