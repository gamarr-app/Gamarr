using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroComponentClassifier
    {
        NoIntroComponentClassification Classify(string relativePath, string fileName);
        NoIntroCatalogPlan BuildCatalogPlan(IEnumerable<NoIntroCatalogEntry> entries);
    }

    public class NoIntroComponentClassifier : INoIntroComponentClassifier
    {
        public NoIntroComponentClassification Classify(string relativePath, string fileName)
        {
            var path = relativePath ?? string.Empty;
            var name = fileName ?? string.Empty;
            var combined = $"{path}/{name}";

            if (Contains(combined, "[BIOS]") || Contains(combined, "/BIOS/") || EndsWithFolder(path, "BIOS"))
            {
                return Exact(NoIntroRomComponentType.Bios);
            }

            if (Contains(combined, "Romhack") || Contains(combined, "/Romhacks/"))
            {
                return Exact(NoIntroRomComponentType.RomhackOrUnverified);
            }

            if (Contains(path, "GBA (e-Reader)"))
            {
                return Exact(NoIntroRomComponentType.EReaderCards);
            }

            if (Contains(path, "GBA (Multiboot)") || Contains(path, "Download Play"))
            {
                return Exact(NoIntroRomComponentType.Multiboot);
            }

            if (Contains(path, "GBA (Video)") || Contains(path, "GBA (Play-Yan)") || Contains(path, "DSvision SD cards"))
            {
                return Exact(NoIntroRomComponentType.Video);
            }

            if (Contains(path, "GBA") || Contains(path, "GBA (by-id)") || Contains(path, "/zip/") || Contains(path, "/nds/") || EndsWithFolder(path, "zip") || EndsWithFolder(path, "nds"))
            {
                return Exact(NoIntroRomComponentType.RetailRom);
            }

            return Fallback(NoIntroRomComponentType.RomhackOrUnverified);
        }

        public NoIntroCatalogPlan BuildCatalogPlan(IEnumerable<NoIntroCatalogEntry> entries)
        {
            var plan = new NoIntroCatalogPlan();

            foreach (var entry in entries ?? Enumerable.Empty<NoIntroCatalogEntry>())
            {
                AddEntry(plan, entry);
            }

            return plan;
        }

        private static void AddEntry(NoIntroCatalogPlan plan, NoIntroCatalogEntry entry)
        {
            var canonicalName = entry.CanonicalName ?? string.Empty;

            if (IsDownloadPlay(canonicalName))
            {
                AddDownloadPlay(plan, entry, canonicalName);
                return;
            }

            if (IsStandaloneProduct(canonicalName))
            {
                plan.StandaloneGames.Add(new NoIntroCatalogStandalonePlan
                {
                    Title = canonicalName,
                    ComponentType = ClassifyStandaloneProduct(canonicalName)
                });

                return;
            }

            var region = TryParseRegion(canonicalName);

            if (region == null)
            {
                plan.StandaloneGames.Add(new NoIntroCatalogStandalonePlan
                {
                    Title = canonicalName,
                    ComponentType = NoIntroRomComponentType.RetailRom
                });

                return;
            }

            GetOrAddGame(plan, entry.SystemKey, region.GameTitle).RegionLanguageComponents.Add(new NoIntroCatalogComponentSlot
            {
                SlotLabel = region.SlotLabel,
                CanonicalName = canonicalName,
                ComponentType = NoIntroRomComponentType.RetailRom
            });
        }

        private static void AddDownloadPlay(NoIntroCatalogPlan plan, NoIntroCatalogEntry entry, string canonicalName)
        {
            if (string.IsNullOrWhiteSpace(entry.ParentCanonicalName))
            {
                plan.StandaloneGames.Add(new NoIntroCatalogStandalonePlan
                {
                    Title = canonicalName,
                    ComponentType = NoIntroRomComponentType.Multiboot
                });

                return;
            }

            GetOrAddGame(plan, entry.SystemKey, entry.ParentCanonicalName).DownloadPlayComponents.Add(new NoIntroCatalogComponentSlot
            {
                SlotLabel = "Download Play",
                CanonicalName = canonicalName,
                ComponentType = NoIntroRomComponentType.Multiboot
            });
        }

        private static NoIntroCatalogGamePlan GetOrAddGame(NoIntroCatalogPlan plan, string systemKey, string gameTitle)
        {
            var game = plan.Games.SingleOrDefault(x =>
                x.SystemKey.Equals(systemKey, StringComparison.OrdinalIgnoreCase) &&
                x.GameTitle.Equals(gameTitle, StringComparison.Ordinal));

            if (game != null)
            {
                return game;
            }

            game = new NoIntroCatalogGamePlan
            {
                SystemKey = systemKey,
                GameTitle = gameTitle
            };

            plan.Games.Add(game);
            return game;
        }

        private static RegionRelease TryParseRegion(string canonicalName)
        {
            var closeIndex = canonicalName.LastIndexOf(')');
            var openIndex = canonicalName.LastIndexOf('(');

            if (openIndex < 1 || closeIndex != canonicalName.Length - 1 || openIndex >= closeIndex)
            {
                return null;
            }

            var label = canonicalName.Substring(openIndex + 1, closeIndex - openIndex - 1);
            var title = canonicalName.Substring(0, openIndex).TrimEnd();

            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return new RegionRelease
            {
                GameTitle = title,
                SlotLabel = label
            };
        }

        private static bool IsDownloadPlay(string canonicalName)
        {
            return Contains(canonicalName, "Download Play");
        }

        private static bool IsStandaloneProduct(string canonicalName)
        {
            return Contains(canonicalName, "Game Boy Advance Video") ||
                   Contains(canonicalName, "Play-Yan") ||
                   Contains(canonicalName, "DSvision") ||
                   Contains(canonicalName, "[BIOS]") ||
                   Contains(canonicalName, "(BIOS)") ||
                   Contains(canonicalName, " Demo") ||
                   Contains(canonicalName, " Prototype") ||
                   Contains(canonicalName, "(Kiosk)") ||
                   Contains(canonicalName, "Not for Resale");
        }

        private static NoIntroRomComponentType ClassifyStandaloneProduct(string canonicalName)
        {
            if (Contains(canonicalName, "[BIOS]") || Contains(canonicalName, "(BIOS)"))
            {
                return NoIntroRomComponentType.Bios;
            }

            if (Contains(canonicalName, "Game Boy Advance Video") || Contains(canonicalName, "Play-Yan") || Contains(canonicalName, "DSvision"))
            {
                return NoIntroRomComponentType.Video;
            }

            return NoIntroRomComponentType.RomhackOrUnverified;
        }

        private static NoIntroComponentClassification Exact(NoIntroRomComponentType componentType)
        {
            return new NoIntroComponentClassification
            {
                ComponentType = componentType,
                IsFallback = false
            };
        }

        private static NoIntroComponentClassification Fallback(NoIntroRomComponentType componentType)
        {
            return new NoIntroComponentClassification
            {
                ComponentType = componentType,
                IsFallback = true
            };
        }

        private static bool Contains(string value, string pattern)
        {
            return value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EndsWithFolder(string path, string folderName)
        {
            return path.EndsWith($"/{folderName}", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith($"\\{folderName}", StringComparison.OrdinalIgnoreCase) ||
                   path.Equals(folderName, StringComparison.OrdinalIgnoreCase);
        }

        private class RegionRelease
        {
            public string GameTitle { get; set; }
            public string SlotLabel { get; set; }
        }
    }
}
