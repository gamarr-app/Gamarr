using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using GamarrLibrary.Mapping;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GamarrLibrary
{
    /// <summary>
    /// Playnite library plugin that imports the local game library managed by
    /// a Gamarr server (https://github.com/gamarr-app/Gamarr).
    /// </summary>
    public class GamarrLibraryPlugin : LibraryPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private readonly GamarrLibrarySettingsViewModel _settingsViewModel;

        public override Guid Id { get; } = Guid.Parse("0a571af3-a11a-48f5-bd02-5ef0068460f2");

        public override string Name => "Gamarr";

        public GamarrLibraryPlugin(IPlayniteAPI api)
            : base(api)
        {
            _settingsViewModel = new GamarrLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            var settings = _settingsViewModel.Settings;

            if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                Logger.Warn("Gamarr library: URL or API key not configured, skipping import.");
                return Enumerable.Empty<GameMetadata>();
            }

            List<GamarrGameDto> games;
            using (var client = new GamarrClient(settings.BaseUrl, settings.ApiKey))
            {
                games = client.GetGames();
            }

            var result = new List<GameMetadata>();
            foreach (var game in games)
            {
                if (args.CancelToken.IsCancellationRequested)
                {
                    break;
                }

                if (!settings.ImportNotDownloaded && !GamarrMapper.IsDownloaded(game))
                {
                    continue;
                }

                var mapped = GamarrMapper.Map(game, settings.BaseUrl, settings.ApiKey);
                result.Add(ToGameMetadata(mapped));
            }

            Logger.Info($"Gamarr library: imported {result.Count} of {games.Count} games.");
            return result;
        }

        /// <summary>
        /// Adapter from the SDK-free MappedGame to Playnite's GameMetadata.
        /// </summary>
        internal static GameMetadata ToGameMetadata(MappedGame mapped)
        {
            var metadata = new GameMetadata
            {
                Source = new MetadataNameProperty("Gamarr"),
                GameId = mapped.GameId,
                Name = mapped.Name,
                SortingName = mapped.SortingName,
                Description = mapped.Description,
                InstallDirectory = mapped.InstallDirectory,
                IsInstalled = mapped.IsInstalled,
                Platforms = new HashSet<MetadataProperty>(
                    mapped.Platforms.Select(p => (MetadataProperty)new MetadataNameProperty(p))),
                Genres = new HashSet<MetadataProperty>(
                    mapped.Genres.Select(g => (MetadataProperty)new MetadataNameProperty(g))),
                Links = mapped.Links.Select(l => new Link(l.Name, l.Url)).ToList()
            };

            if (mapped.ReleaseDate.HasValue)
            {
                metadata.ReleaseDate = new ReleaseDate(mapped.ReleaseDate.Value);
            }

            if (!string.IsNullOrEmpty(mapped.CoverUrl))
            {
                metadata.CoverImage = new MetadataFile(mapped.CoverUrl);
            }

            if (!string.IsNullOrEmpty(mapped.Developer))
            {
                metadata.Developers = new HashSet<MetadataProperty> { new MetadataNameProperty(mapped.Developer) };
            }

            if (!string.IsNullOrEmpty(mapped.Publisher))
            {
                metadata.Publishers = new HashSet<MetadataProperty> { new MetadataNameProperty(mapped.Publisher) };
            }

            return metadata;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return _settingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new GamarrLibrarySettingsView();
        }
    }
}
