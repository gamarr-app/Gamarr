using NzbDrone.Core.Games;

namespace NzbDrone.Core.Notifications
{
    public class GameDeleteMessage
    {
        public string Message { get; set; }
        public Game Game { get; set; }
        public bool DeletedFiles { get; set; }
        public string DeletedFilesMessage { get; set; }

        public override string ToString()
        {
            return Message;
        }

        public GameDeleteMessage(Game game, bool deleteFiles)
        {
            Game = game;
            DeletedFiles = deleteFiles;
            DeletedFilesMessage = DeletedFiles ?
                "Game removed and all files were deleted" :
                "Game removed, files were not deleted";
            Message = game.Title + " - " + DeletedFilesMessage;
        }
    }
}
