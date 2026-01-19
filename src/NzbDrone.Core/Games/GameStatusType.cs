namespace NzbDrone.Core.Games
{
    public enum GameStatusType
    {
        Deleted = -1,
        TBA = 0,           // Nothing yet announced, only rumors
        Announced = 1,     // Game is announced but release date is in the future or unknown
        EarlyAccess = 2,   // Game is in Early Access / Beta (since IGDB may lack complete info)
        Released = 3,      // Game has been fully released (physical or digital)
    }
}
