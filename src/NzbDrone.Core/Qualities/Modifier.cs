namespace NzbDrone.Core.Qualities
{
    public enum Modifier
    {
        NONE = 0,
        PRELOAD,        // Pre-release preload (before official unlock)
        CRACKED,        // Has crack applied
        DRM_FREE,       // DRM-free version
        MULTI_LANG,     // Multi-language version
        ALL_DLC,        // Includes all DLC
        UPDATE_ONLY     // Update/patch only (not full game)
    }
}
