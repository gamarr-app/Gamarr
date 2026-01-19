namespace NzbDrone.Core.Qualities
{
    /// <summary>
    /// Resolution enum for custom format compatibility.
    /// For games, this is less relevant than for video content,
    /// but kept for backward compatibility with custom formats.
    /// </summary>
    public enum Resolution
    {
        Unknown = 0,
        R480p = 480,
        R576p = 576,
        R720p = 720,
        R1080p = 1080,
        R2160p = 2160
    }
}
