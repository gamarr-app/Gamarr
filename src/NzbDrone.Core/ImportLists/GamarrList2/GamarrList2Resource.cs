namespace NzbDrone.Core.ImportLists.GamarrList2
{
    /// <summary>
    /// Simple resource class for parsing Gamarr list responses.
    /// Contains the minimum fields needed to import games from list services.
    /// </summary>
    public class GamarrList2Resource
    {
        public int IgdbId { get; set; }
        public string Title { get; set; }
        public int? Year { get; set; }
    }
}
