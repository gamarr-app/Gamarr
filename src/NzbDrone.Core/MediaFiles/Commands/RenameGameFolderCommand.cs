using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RenameGameFolderCommand : Command
    {
        public List<int> GameIds { get; set; }

        public override bool SendUpdatesToClient => false;

        public RenameGameFolderCommand()
        {
        }

        public RenameGameFolderCommand(List<int> ids)
        {
            GameIds = ids;
        }
    }
}
