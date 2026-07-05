using System;
using System.Collections.Generic;
using System.Linq;

namespace GamarrLibrary.Mapping
{
    /// <summary>
    /// Pure Gamarr -> Playnite mapping logic. No PlayniteSDK dependency so it
    /// can be unit tested outside a Playnite host.
    /// </summary>
    public static class GamarrMapper
    {
        /// <summary>
        /// A game is considered part of the local library when Gamarr has an
        /// imported file for it.
        /// </summary>
        public static bool IsDownloaded(GamarrGameDto game)
        {
            if (game == null)
            {
                return false;
            }

            return game.HasFile == true || (game.SizeOnDisk ?? 0) > 0;
        }

        public static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            return baseUrl.Trim().TrimEnd('/');
        }

        public static string BuildCoverUrl(string baseUrl, int gameId, string apiKey)
        {
            var url = NormalizeBaseUrl(baseUrl);
            if (url == null)
            {
                return null;
            }

            return $"{url}/api/v3/mediacover/{gameId}/poster.jpg?apikey={Uri.EscapeDataString(apiKey ?? string.Empty)}";
        }

        public static MappedGame Map(GamarrGameDto game, string baseUrl, string apiKey)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            var url = NormalizeBaseUrl(baseUrl);
            var installed = IsDownloaded(game) && !string.IsNullOrWhiteSpace(game.Path);

            var mapped = new MappedGame
            {
                GameId = game.Id.ToString(),
                Name = string.IsNullOrWhiteSpace(game.Title) ? $"Gamarr game {game.Id}" : game.Title,
                SortingName = string.IsNullOrWhiteSpace(game.SortTitle) ? null : game.SortTitle,
                Description = string.IsNullOrWhiteSpace(game.Overview) ? null : game.Overview,
                ReleaseDate = ResolveReleaseDate(game),
                InstallDirectory = installed ? game.Path : null,
                IsInstalled = installed,
                CoverUrl = url == null ? null : BuildCoverUrl(url, game.Id, apiKey),
                Developer = string.IsNullOrWhiteSpace(game.Developer) ? null : game.Developer,
                Publisher = string.IsNullOrWhiteSpace(game.Publisher) ? null : game.Publisher,
                Genres = (game.Genres ?? new List<string>())
                    .Where(g => !string.IsNullOrWhiteSpace(g))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Platforms = (game.Platforms ?? new List<GamarrPlatformDto>())
                    .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                    .Select(p => p.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Links = BuildLinks(game, url)
            };

            return mapped;
        }

        /// <summary>
        /// Prefer the aggregated releaseDate; fall back to digital/physical
        /// release, then plain year.
        /// </summary>
        public static DateTime? ResolveReleaseDate(GamarrGameDto game)
        {
            if (game.ReleaseDate.HasValue)
            {
                return game.ReleaseDate;
            }

            if (game.DigitalRelease.HasValue)
            {
                return game.DigitalRelease;
            }

            if (game.PhysicalRelease.HasValue)
            {
                return game.PhysicalRelease;
            }

            if (game.Year > 0)
            {
                return new DateTime(game.Year, 1, 1);
            }

            return null;
        }

        public static List<MappedLink> BuildLinks(GamarrGameDto game, string normalizedBaseUrl)
        {
            var links = new List<MappedLink>();

            if (normalizedBaseUrl != null && !string.IsNullOrWhiteSpace(game.TitleSlug))
            {
                links.Add(new MappedLink("Gamarr", $"{normalizedBaseUrl}/game/{game.TitleSlug}"));
            }

            if (game.SteamAppId > 0)
            {
                links.Add(new MappedLink("Steam", $"https://store.steampowered.com/app/{game.SteamAppId}/"));
            }

            if (!string.IsNullOrWhiteSpace(game.IgdbSlug))
            {
                links.Add(new MappedLink("IGDB", $"https://www.igdb.com/games/{game.IgdbSlug}"));
            }

            if (game.RawgId > 0)
            {
                links.Add(new MappedLink("RAWG", $"https://rawg.io/games/{game.RawgId}"));
            }

            if (!string.IsNullOrWhiteSpace(game.Website))
            {
                links.Add(new MappedLink("Website", game.Website));
            }

            return links;
        }
    }
}
