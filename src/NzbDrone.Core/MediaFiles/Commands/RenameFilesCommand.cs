using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RenameFilesCommand : Command
    {
        public int GameId { get; set; }
        public List<int> Files { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public RenameFilesCommand()
        {
        }

        public RenameFilesCommand(int gameId, List<int> files)
        {
            GameId = gameId;
            Files = files;
        }
    }
}
