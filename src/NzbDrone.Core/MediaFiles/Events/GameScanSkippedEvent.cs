using NzbDrone.Common.Messaging;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class GameScanSkippedEvent : IEvent
    {
        public Game Game { get; private set; }
        public GameScanSkippedReason Reason { get; set; }

        public GameScanSkippedEvent(Game game, GameScanSkippedReason reason)
        {
            Game = game;
            Reason = reason;
        }
    }

    public enum GameScanSkippedReason
    {
        RootFolderDoesNotExist,
        RootFolderIsEmpty,
        GameFolderDoesNotExist,
        NeverRescanAfterRefresh,
        RescanAfterManualRefreshOnly
    }
}
