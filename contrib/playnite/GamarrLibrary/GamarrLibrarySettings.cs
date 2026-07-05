using System;
using System.Collections.Generic;
using Playnite.SDK;

namespace GamarrLibrary
{
    /// <summary>
    /// Persisted plugin settings.
    /// </summary>
    public class GamarrLibrarySettings
    {
        /// <summary>
        /// Base URL of the Gamarr server, e.g. http://localhost:6767
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gamarr API key (Settings -> General in the Gamarr UI, or
        /// &lt;ApiKey&gt; in config.xml).
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// When true, games monitored by Gamarr but not yet downloaded are
        /// imported too (as not-installed). Default: only downloaded games.
        /// </summary>
        public bool ImportNotDownloaded { get; set; }
    }

    public class GamarrLibrarySettingsViewModel : ObservableObject, ISettings
    {
        private readonly GamarrLibraryPlugin _plugin;
        private GamarrLibrarySettings _editingClone;
        private GamarrLibrarySettings _settings;

        public GamarrLibrarySettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public GamarrLibrarySettingsViewModel(GamarrLibraryPlugin plugin)
        {
            _plugin = plugin;
            Settings = plugin.LoadPluginSettings<GamarrLibrarySettings>() ?? new GamarrLibrarySettings();
        }

        public void BeginEdit()
        {
            _editingClone = new GamarrLibrarySettings
            {
                BaseUrl = Settings.BaseUrl,
                ApiKey = Settings.ApiKey,
                ImportNotDownloaded = Settings.ImportNotDownloaded
            };
        }

        public void CancelEdit()
        {
            Settings = _editingClone;
        }

        public void EndEdit()
        {
            _plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Settings.BaseUrl))
            {
                errors.Add("Gamarr URL is required (e.g. http://localhost:6767).");
            }
            else if (!Uri.TryCreate(Settings.BaseUrl.Trim(), UriKind.Absolute, out var uri)
                     || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add("Gamarr URL must be an absolute http(s) URL.");
            }

            if (string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                errors.Add("Gamarr API key is required.");
            }

            return errors.Count == 0;
        }
    }
}
