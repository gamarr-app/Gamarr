namespace NzbDrone.Core.Games
{
    public class AddGameOptions : MonitoringOptions
    {
        public bool SearchForGame { get; set; }
        public AddGameMethod AddMethod { get; set; }
    }

    public enum AddGameMethod
    {
        Manual,
        List,
        Collection
    }
}
