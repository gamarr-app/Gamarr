using NzbDrone.Core.Notifications;

namespace Gamarr.Api.V3.Notifications
{
    public class NotificationBulkResource : ProviderBulkResource<NotificationBulkResource>
    {
    }

    public class NotificationBulkResourceMapper : ProviderBulkResourceMapper<NotificationBulkResource, NotificationDefinition>
    {
    }
}
