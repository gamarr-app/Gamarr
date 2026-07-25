namespace NzbDrone.Core.Games
{
    /// <summary>
    /// Reference to a DLC/expansion with its metadata-source ID and name.
    /// Source distinguishes the ID space ("igdb" or "steam") — the numeric
    /// IDs overlap between providers, so component slot keys carry the prefix.
    /// </summary>
    public class DlcReference
    {
        public const string IgdbSource = "igdb";
        public const string SteamSource = "steam";

        public int Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; } = IgdbSource;

        public DlcReference()
        {
        }

        public DlcReference(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public DlcReference(int id, string name, string source)
        {
            Id = id;
            Name = name;
            Source = source;
        }
    }
}
