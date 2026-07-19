using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RomCatalog;

namespace NzbDrone.Core.Games.Components
{
    public class NoIntroGameComponentPlanner
    {
        private readonly INoIntroComponentClassifier _componentClassifier;

        public NoIntroGameComponentPlanner(INoIntroComponentClassifier componentClassifier)
        {
            _componentClassifier = componentClassifier;
        }

        public List<NoIntroGameComponentSlot> GetSlots(Game game, List<NoIntroCatalogEntry> entries)
        {
            var platformEntries = entries
                .Where(entry => entry.PlatformFamily == game.Platform)
                .ToList();
            var plan = _componentClassifier.BuildCatalogPlan(platformEntries);
            var gamePlans = plan.Games.Where(x => x.GameTitle == game.Title).ToList();

            if (gamePlans.Count == 0)
            {
                return new List<NoIntroGameComponentSlot>();
            }

            var entriesByCanonicalName = platformEntries
                .GroupBy(entry => entry.CanonicalName)
                .ToDictionary(group => group.Key, group => group.ToList());

            return gamePlans.SelectMany(gamePlan => gamePlan.RegionLanguageComponents.Concat(gamePlan.DownloadPlayComponents))
                .Select(slot => ToSlot(slot, entriesByCanonicalName))
                .ToList();
        }

        public NoIntroGameComponentSlot FindSlotForFile(Game game, List<NoIntroCatalogEntry> entries, GameFile file)
        {
            if (string.IsNullOrWhiteSpace(file.RelativePath))
            {
                return null;
            }

            var fileName = Path.GetFileName(file.RelativePath.Replace('\\', '/'));

            return FindSlotForFileName(game, entries, fileName);
        }

        public NoIntroGameComponentSlot FindSlotForFileName(Game game, List<NoIntroCatalogEntry> entries, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return GetSlots(game, entries).FirstOrDefault(slot => slot.FileNames.Contains(fileName));
        }

        private static NoIntroGameComponentSlot ToSlot(NoIntroCatalogComponentSlot slot, Dictionary<string, List<NoIntroCatalogEntry>> entriesByCanonicalName)
        {
            var componentType = MapComponentType(slot.ComponentType);
            entriesByCanonicalName.TryGetValue(slot.CanonicalName, out var entries);

            return new NoIntroGameComponentSlot
            {
                ComponentType = componentType,
                Key = $"nointro:{ComponentKeyPrefix(componentType)}:{Parser.Parser.ToUrlSlug(slot.CanonicalName, true)}",
                Title = slot.SlotLabel,
                FileNames = BuildFileNames(slot.CanonicalName, entries ?? new List<NoIntroCatalogEntry>())
            };
        }

        private static HashSet<string> BuildFileNames(string canonicalName, List<NoIntroCatalogEntry> entries)
        {
            var fileNames = new HashSet<string>
            {
                $"{canonicalName}.gb",
                $"{canonicalName}.gbc",
                $"{canonicalName}.gba",
                $"{canonicalName}.nds",
                $"{canonicalName}.zip"
            };

            foreach (var entry in entries)
            {
                AddFileName(fileNames, entry.CanonicalFileName);
                AddFileName(fileNames, entry.NumberedCanonicalFileName);
            }

            return fileNames;
        }

        private static void AddFileName(HashSet<string> fileNames, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                fileNames.Add(fileName);
            }
        }

        private static GameComponentType MapComponentType(NoIntroRomComponentType componentType)
        {
            return componentType switch
            {
                NoIntroRomComponentType.RetailRom => GameComponentType.NoIntroRetailRom,
                NoIntroRomComponentType.EReaderCards => GameComponentType.NoIntroMultiboot,
                NoIntroRomComponentType.Multiboot => GameComponentType.NoIntroMultiboot,
                NoIntroRomComponentType.Video => GameComponentType.NoIntroVideo,
                NoIntroRomComponentType.Bios => GameComponentType.NoIntroBios,
                _ => GameComponentType.NoIntroRomhackOrUnverified
            };
        }

        private static string ComponentKeyPrefix(GameComponentType componentType)
        {
            return componentType switch
            {
                GameComponentType.NoIntroRetailRom => "retail",
                GameComponentType.NoIntroMultiboot => "multiboot",
                GameComponentType.NoIntroVideo => "video",
                GameComponentType.NoIntroBios => "bios",
                _ => "unverified"
            };
        }
    }

    public class NoIntroGameComponentSlot
    {
        public GameComponentType ComponentType { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public HashSet<string> FileNames { get; set; }
    }
}
