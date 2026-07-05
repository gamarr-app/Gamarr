namespace NzbDrone.Core.ImportLists.Gog
{
    /// <summary>
    /// A single product parsed from a public GOG profile (wishlist or library).
    /// </summary>
    public class GogProduct
    {
        public long GogId { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
    }
}
