using System;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Notifications
{
    public enum MetadataLinkType
    {
        [FieldOption(Label = "IGDB")]
        Igdb = 0,

        /// <summary>
        /// DEPRECATED: IMDb is a movie database and does not apply to games.
        /// This option is kept for backwards compatibility but will not generate valid links for games.
        /// </summary>
        [Obsolete("IMDb is a movie database and does not apply to games.")]
        [FieldOption(Label = "IMDb (Deprecated - Movies Only)")]
        Imdb = 1,

        [FieldOption(Label = "Trakt")]
        Trakt = 2
    }
}
