namespace NzbDrone.Core.Games
{
    public enum GameStatusType
    {
        Deleted = -1,
        TBA = 0,       // Nothing yet announced, only rumors, but still IMDb page (this might not be used)
        Announced = 1, // Game is announced but Cinema date is in the future or unknown
        InDevelopment = 2, // Been in Cinemas for less than 3 months (since IGDB lacks complete information)
        Released = 3,  // Physical or Web Release or been in cinemas for > 3 months (since IGDB lacks complete information)
    }
}
