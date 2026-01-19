namespace NzbDrone.Core.Qualities
{
    public enum QualitySource
    {
        UNKNOWN = 0,
        SCENE,      // Scene releases (cracked/pirated)
        GOG,        // GOG.com DRM-free releases
        STEAM,      // Steam rips
        EPIC,       // Epic Games Store rips
        ORIGIN,     // EA Origin/EA App rips
        UPLAY,      // Ubisoft Connect rips
        REPACK,     // Repacked/compressed releases (FitGirl, DODI, etc.)
        ISO,        // Full uncompressed ISO/disk images
        RETAIL,     // Physical retail disc rips
        PORTABLE    // Portable/no-install versions
    }
}
