using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RomCatalog;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.GameComponents
{
    public class GameComponentResource : RestResource
    {
        public int GameId { get; set; }
        public GameComponentType ComponentType { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public bool Monitored { get; set; }
        public int ExternalId { get; set; }

        // 0 inherits the game's quality profile.
        public int QualityProfileId { get; set; }

        // Derived: whether any imported file belongs to this slot. A monitored
        // component without a file is "missing".
        public bool HasFile { get; set; }
        public long SizeOnDisk { get; set; }
        public List<GameComponentNoIntroCatalogResource> NoIntroCatalogMatches { get; set; } = new List<GameComponentNoIntroCatalogResource>();
    }

    public class GameComponentNoIntroCatalogResource : RestResource
    {
        public int CatalogSourceId { get; set; }
        public string SourceName { get; set; }
        public string SystemKey { get; set; }
        public string CanonicalName { get; set; }
        public string CanonicalFileName { get; set; }
        public string CatalogVersion { get; set; }
        public string LastSyncError { get; set; }
        public string HashType { get; set; }
        public string HashValue { get; set; }
    }

    public class GameComponentNoIntroCatalogContext
    {
        public List<GameFile> GameFiles { get; set; } = new List<GameFile>();
        public List<NoIntroCatalogEntry> Entries { get; set; } = new List<NoIntroCatalogEntry>();
        public List<NoIntroCatalogSource> Sources { get; set; } = new List<NoIntroCatalogSource>();
        public List<NoIntroCatalogFileHashMatch> HashMatches { get; set; } = new List<NoIntroCatalogFileHashMatch>();
    }

    public class NoIntroCatalogFileHashMatch
    {
        public int GameFileId { get; set; }
        public int CatalogEntryId { get; set; }
        public string HashType { get; set; }
        public string HashValue { get; set; }
    }

    public static class GameComponentResourceMapper
    {
        public static GameComponentResource ToResource(this GameComponent model, GameComponentNoIntroCatalogContext context = null)
        {
            context ??= new GameComponentNoIntroCatalogContext();
            var files = context.GameFiles.Where(f => f.ComponentId == model.Id).ToList();

            return new GameComponentResource
            {
                Id = model.Id,
                GameId = model.GameId,
                ComponentType = model.ComponentType,
                Key = model.Key,
                Title = model.Title,
                Monitored = model.Monitored,
                ExternalId = model.ExternalId,
                QualityProfileId = model.QualityProfileId,
                HasFile = files.Any(),
                SizeOnDisk = files.Sum(f => f.Size),
                NoIntroCatalogMatches = GetNoIntroCatalogMatches(model, files, context)
            };
        }

        private static List<GameComponentNoIntroCatalogResource> GetNoIntroCatalogMatches(GameComponent component, List<GameFile> files, GameComponentNoIntroCatalogContext context)
        {
            if (context.Entries.Count == 0 || string.IsNullOrWhiteSpace(component.Title))
            {
                return new List<GameComponentNoIntroCatalogResource>();
            }

            var sourceById = context.Sources.ToDictionary(x => x.Id);
            var exactMatches = GetExactHashMatches(files, context);
            var entries = exactMatches.Count > 0 ? exactMatches.Select(x => x.Entry) : context.Entries.Where(entry => IsComponentCatalogMatch(component, entry));

            return entries
                .OrderBy(entry => entry.SystemKey)
                .ThenBy(entry => entry.CanonicalName)
                .Take(20)
                .Select(entry =>
                {
                    sourceById.TryGetValue(entry.CatalogSourceId, out var source);
                    var hashMatch = exactMatches
                        .Where(x => x.Entry.Id == entry.Id)
                        .Select(x => x.HashMatch)
                        .FirstOrDefault();

                    return new GameComponentNoIntroCatalogResource
                    {
                        Id = entry.Id,
                        CatalogSourceId = entry.CatalogSourceId,
                        SourceName = source?.Name,
                        SystemKey = entry.SystemKey,
                        CanonicalName = entry.CanonicalName,
                        CanonicalFileName = entry.CanonicalFileName,
                        CatalogVersion = source?.CatalogVersion,
                        LastSyncError = source?.LastSyncError,
                        HashType = hashMatch?.HashType,
                        HashValue = hashMatch?.HashValue
                    };
                })
                .ToList();
        }

        private static List<(NoIntroCatalogEntry Entry, NoIntroCatalogFileHashMatch HashMatch)> GetExactHashMatches(List<GameFile> files, GameComponentNoIntroCatalogContext context)
        {
            var fileIds = files.Select(x => x.Id).ToHashSet();
            var entryById = context.Entries.ToDictionary(x => x.Id);

            return context.HashMatches
                .Where(match => fileIds.Contains(match.GameFileId) && entryById.ContainsKey(match.CatalogEntryId))
                .Select(match => (entryById[match.CatalogEntryId], match))
                .ToList();
        }

        private static bool IsComponentCatalogMatch(GameComponent component, NoIntroCatalogEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.CanonicalName))
            {
                return false;
            }

            if (IsNoIntroComponent(component))
            {
                return component.Key?.EndsWith($":{NzbDrone.Core.Parser.Parser.ToUrlSlug(entry.CanonicalName, true)}", global::System.StringComparison.Ordinal) == true;
            }

            if (!string.IsNullOrWhiteSpace(entry.ParentCanonicalName) && entry.ParentCanonicalName == component.Title)
            {
                return true;
            }

            return entry.CanonicalName == component.Title || entry.CanonicalName.StartsWith($"{component.Title} (", global::System.StringComparison.Ordinal);
        }

        private static bool IsNoIntroComponent(GameComponent component)
        {
            return component.ComponentType is GameComponentType.NoIntroRetailRom or
                GameComponentType.NoIntroMultiboot or
                GameComponentType.NoIntroVideo or
                GameComponentType.NoIntroBios or
                GameComponentType.NoIntroRomhackOrUnverified;
        }
    }
}
