using NzbDrone.Core.Notifications;

namespace Gamarr.Api.V3.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public string Link { get; set; }
        public bool OnGrab { get; set; }
        public bool OnDownload { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnGameAdded { get; set; }
        public bool OnGameDelete { get; set; }
        public bool OnGameFileDelete { get; set; }
        public bool OnGameFileDeleteForUpgrade { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool OnHealthRestored { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool OnManualInteractionRequired { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool SupportsOnDownload { get; set; }
        public bool SupportsOnUpgrade { get; set; }
        public bool SupportsOnRename { get; set; }
        public bool SupportsOnGameAdded { get; set; }
        public bool SupportsOnGameDelete { get; set; }
        public bool SupportsOnGameFileDelete { get; set; }
        public bool SupportsOnGameFileDeleteForUpgrade { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool SupportsOnHealthRestored { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }
        public bool SupportsOnManualInteractionRequired { get; set; }
        public string TestCommand { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            if (definition == null)
            {
                return default(NotificationResource);
            }

            var resource = base.ToResource(definition);

            resource.OnGrab = definition.OnGrab;
            resource.OnDownload = definition.OnDownload;
            resource.OnUpgrade = definition.OnUpgrade;
            resource.OnRename = definition.OnRename;
            resource.OnGameAdded = definition.OnGameAdded;
            resource.OnGameDelete = definition.OnGameDelete;
            resource.OnGameFileDelete = definition.OnGameFileDelete;
            resource.OnGameFileDeleteForUpgrade = definition.OnGameFileDeleteForUpgrade;
            resource.OnHealthIssue = definition.OnHealthIssue;
            resource.IncludeHealthWarnings = definition.IncludeHealthWarnings;
            resource.OnHealthRestored = definition.OnHealthRestored;
            resource.OnApplicationUpdate = definition.OnApplicationUpdate;
            resource.OnManualInteractionRequired = definition.OnManualInteractionRequired;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.SupportsOnDownload = definition.SupportsOnDownload;
            resource.SupportsOnUpgrade = definition.SupportsOnUpgrade;
            resource.SupportsOnRename = definition.SupportsOnRename;
            resource.SupportsOnGameAdded = definition.SupportsOnGameAdded;
            resource.SupportsOnGameDelete = definition.SupportsOnGameDelete;
            resource.SupportsOnGameFileDelete = definition.SupportsOnGameFileDelete;
            resource.SupportsOnGameFileDeleteForUpgrade = definition.SupportsOnGameFileDeleteForUpgrade;
            resource.SupportsOnHealthIssue = definition.SupportsOnHealthIssue;
            resource.SupportsOnHealthRestored = definition.SupportsOnHealthRestored;
            resource.SupportsOnApplicationUpdate = definition.SupportsOnApplicationUpdate;
            resource.SupportsOnManualInteractionRequired = definition.SupportsOnManualInteractionRequired;

            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource, NotificationDefinition existingDefinition)
        {
            if (resource == null)
            {
                return default(NotificationDefinition);
            }

            var definition = base.ToModel(resource, existingDefinition);

            definition.OnGrab = resource.OnGrab;
            definition.OnDownload = resource.OnDownload;
            definition.OnUpgrade = resource.OnUpgrade;
            definition.OnRename = resource.OnRename;
            definition.OnGameAdded = resource.OnGameAdded;
            definition.OnGameDelete = resource.OnGameDelete;
            definition.OnGameFileDelete = resource.OnGameFileDelete;
            definition.OnGameFileDeleteForUpgrade = resource.OnGameFileDeleteForUpgrade;
            definition.OnHealthIssue = resource.OnHealthIssue;
            definition.IncludeHealthWarnings = resource.IncludeHealthWarnings;
            definition.OnHealthRestored = resource.OnHealthRestored;
            definition.OnApplicationUpdate = resource.OnApplicationUpdate;
            definition.OnManualInteractionRequired = resource.OnManualInteractionRequired;
            definition.SupportsOnGrab = resource.SupportsOnGrab;
            definition.SupportsOnDownload = resource.SupportsOnDownload;
            definition.SupportsOnUpgrade = resource.SupportsOnUpgrade;
            definition.SupportsOnRename = resource.SupportsOnRename;
            definition.SupportsOnGameAdded = resource.SupportsOnGameAdded;
            definition.SupportsOnGameDelete = resource.SupportsOnGameDelete;
            definition.SupportsOnGameFileDelete = resource.SupportsOnGameFileDelete;
            definition.SupportsOnGameFileDeleteForUpgrade = resource.SupportsOnGameFileDeleteForUpgrade;
            definition.SupportsOnHealthIssue = resource.SupportsOnHealthIssue;
            definition.SupportsOnHealthRestored = resource.SupportsOnHealthRestored;
            definition.SupportsOnApplicationUpdate = resource.SupportsOnApplicationUpdate;
            definition.SupportsOnManualInteractionRequired = resource.SupportsOnManualInteractionRequired;

            return definition;
        }
    }
}
