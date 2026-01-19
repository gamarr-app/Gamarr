using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Notifications
{
    public enum MetadataLinkType
    {
        [FieldOption(Label = "IGDB")]
        Igdb = 0,

        [FieldOption(Label = "Steam")]
        Steam = 1,

        [FieldOption(Label = "RAWG")]
        Rawg = 2
    }
}
