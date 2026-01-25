namespace NzbDrone.Core.Games
{
    /// <summary>
    /// Reference to a DLC/expansion with its IGDB ID and name
    /// </summary>
    public class DlcReference
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public DlcReference()
        {
        }

        public DlcReference(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
