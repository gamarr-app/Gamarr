namespace NzbDrone.Core.RomCatalog
{
    public enum NoIntroVerificationStatus
    {
        Verified = 0,
        NameMismatch = 1,
        Unknown = 2,
        BadDump = 3
    }
}
