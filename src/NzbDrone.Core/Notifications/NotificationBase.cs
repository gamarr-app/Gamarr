using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public abstract class NotificationBase<TSettings> : INotification
        where TSettings : NotificationSettingsBase<TSettings>, new()
    {
        protected const string GAME_GRABBED_TITLE = "Game Grabbed";
        protected const string GAME_DOWNLOADED_TITLE = "Game Downloaded";
        protected const string GAME_UPGRADED_TITLE = "Game Upgraded";
        protected const string GAME_ADDED_TITLE = "Game Added";
        protected const string GAME_DELETED_TITLE = "Game Deleted";
        protected const string GAME_FILE_DELETED_TITLE = "Game File Deleted";
        protected const string HEALTH_ISSUE_TITLE = "Health Check Failure";
        protected const string HEALTH_RESTORED_TITLE = "Health Check Restored";
        protected const string APPLICATION_UPDATE_TITLE = "Application Updated";
        protected const string MANUAL_INTERACTION_REQUIRED_TITLE = "Manual Interaction";

        protected const string GAME_GRABBED_TITLE_BRANDED = "Gamarr - " + GAME_GRABBED_TITLE;
        protected const string GAME_ADDED_TITLE_BRANDED = "Gamarr - " + GAME_ADDED_TITLE;
        protected const string GAME_DOWNLOADED_TITLE_BRANDED = "Gamarr - " + GAME_DOWNLOADED_TITLE;
        protected const string GAME_UPGRADED_TITLE_BRANDED = "Gamarr - " + GAME_UPGRADED_TITLE;
        protected const string GAME_DELETED_TITLE_BRANDED = "Gamarr - " + GAME_DELETED_TITLE;
        protected const string GAME_FILE_DELETED_TITLE_BRANDED = "Gamarr - " + GAME_FILE_DELETED_TITLE;
        protected const string HEALTH_ISSUE_TITLE_BRANDED = "Gamarr - " + HEALTH_ISSUE_TITLE;
        protected const string HEALTH_RESTORED_TITLE_BRANDED = "Gamarr - " + HEALTH_RESTORED_TITLE;
        protected const string APPLICATION_UPDATE_TITLE_BRANDED = "Gamarr - " + APPLICATION_UPDATE_TITLE;
        protected const string MANUAL_INTERACTION_REQUIRED_TITLE_BRANDED = "Gamarr - " + MANUAL_INTERACTION_REQUIRED_TITLE;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        public abstract string Link { get; }

        public virtual void OnGrab(GrabMessage grabMessage)
        {
        }

        public virtual void OnDownload(DownloadMessage message)
        {
        }

        public virtual void OnGameRename(Game game, List<RenamedGameFile> renamedFiles)
        {
        }

        public virtual void OnGameAdded(Game game)
        {
        }

        public virtual void OnGameFileDelete(GameFileDeleteMessage deleteMessage)
        {
        }

        public virtual void OnGameDelete(GameDeleteMessage deleteMessage)
        {
        }

        public virtual void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
        }

        public virtual void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
        }

        public virtual void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
        }

        public virtual void OnManualInteractionRequired(ManualInteractionRequiredMessage message)
        {
        }

        public virtual void ProcessQueue()
        {
        }

        public bool SupportsOnGrab => HasConcreteImplementation("OnGrab");
        public bool SupportsOnRename => HasConcreteImplementation("OnGameRename");
        public bool SupportsOnDownload => HasConcreteImplementation("OnDownload");
        public bool SupportsOnUpgrade => SupportsOnDownload;
        public bool SupportsOnGameAdded => HasConcreteImplementation("OnGameAdded");
        public bool SupportsOnGameDelete => HasConcreteImplementation("OnGameDelete");
        public bool SupportsOnGameFileDelete => HasConcreteImplementation("OnGameFileDelete");
        public bool SupportsOnGameFileDeleteForUpgrade => SupportsOnGameFileDelete;
        public bool SupportsOnHealthIssue => HasConcreteImplementation("OnHealthIssue");
        public bool SupportsOnHealthRestored => HasConcreteImplementation("OnHealthRestored");
        public bool SupportsOnApplicationUpdate => HasConcreteImplementation("OnApplicationUpdate");
        public bool SupportsOnManualInteractionRequired => HasConcreteImplementation("OnManualInteractionRequired");

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        private bool HasConcreteImplementation(string methodName)
        {
            var method = GetType().GetMethod(methodName);

            if (method == null)
            {
                throw new MissingMethodException(GetType().Name, Name);
            }

            return !method.DeclaringType.IsAbstract;
        }
    }
}
