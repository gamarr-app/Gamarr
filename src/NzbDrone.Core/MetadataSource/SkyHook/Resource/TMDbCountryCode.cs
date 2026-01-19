using System;

namespace NzbDrone.Core.MetadataSource.SkyHook.Resource
{
    /// <summary>
    /// Country codes used for game ratings and certifications.
    /// Renamed from TMDbCountryCode since TMDb is movie-specific.
    /// These are standard ISO country codes used for ESRB, PEGI, and other game rating systems.
    /// </summary>
    public enum RatingCountry
    {
        AU, // Australia (ACB)
        BR, // Brazil (DJCTQ)
        CA, // Canada (ESRB/OFLC)
        FR, // France (PEGI)
        DE, // Germany (USK)
        GB, // Great Britain (PEGI/BBFC)
        IN, // India
        IE, // Ireland (PEGI)
        IT, // Italy (PEGI)
        NZ, // New Zealand (OFLC)
        RO, // Romania (PEGI)
        ES, // Spain (PEGI)
        US, // United States (ESRB)
    }

    /// <summary>
    /// DEPRECATED: Use RatingCountry instead.
    /// This type alias is provided for backwards compatibility.
    /// </summary>
    [Obsolete("Use RatingCountry instead. TMDb is a movie database and does not apply to games.")]
    public enum TMDbCountryCode
    {
        AU = RatingCountry.AU,
        BR = RatingCountry.BR,
        CA = RatingCountry.CA,
        FR = RatingCountry.FR,
        DE = RatingCountry.DE,
        GB = RatingCountry.GB,
        IN = RatingCountry.IN,
        IE = RatingCountry.IE,
        IT = RatingCountry.IT,
        NZ = RatingCountry.NZ,
        RO = RatingCountry.RO,
        ES = RatingCountry.ES,
        US = RatingCountry.US,
    }
}
