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

            if (Contains(path, "GBA") ||
                Contains(path, "GBA (by-id)") ||
                Contains(path, "3DS") ||
                Contains(path, "PSP") ||
                Contains(path, "PlayStation Portable") ||
                Contains(path, "PlayStation Vita") ||
                Contains(path, "PlayStation 3") ||
                Contains(path, "/zip/") ||
                Contains(path, "/nds/") ||
                Contains(path, "/3ds/") ||
                Contains(path, "/cia/") ||
                Contains(path, "/iso/") ||
                Contains(path, "/pkg/") ||
                Contains(path, "/vpk/") ||
                EndsWithFolder(path, "zip") ||
                EndsWithFolder(path, "nds") ||
                EndsWithFolder(path, "3ds") ||
                EndsWithFolder(path, "cia") ||
                EndsWithFolder(path, "iso") ||
                EndsWithFolder(path, "pkg") ||
                EndsWithFolder(path, "vpk"))
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

            if (IsDownloadPlaySource(entry.SystemKey))
            {
                AddDownloadPlaySourceEntry(plan, entry, canonicalName);
                return;
            }

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

            var region = TryParseRegionRelease(canonicalName);

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

        private static RegionRelease TryParseRegionRelease(string canonicalName)
        {
            var tags = ParseTrailingTags(canonicalName);

            if (tags.Count == 0)
            {
                return null;
            }

            var title = canonicalName.Substring(0, tags[0].OpenIndex).TrimEnd();

            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return new RegionRelease
            {
                GameTitle = NormalizeGameTitle(title, tags),
                SlotLabel = BuildSlotLabel(tags)
            };
        }

        private static string NormalizeGameTitle(string title, List<ReleaseTag> tags)
        {
            if (tags.Any(tag => Contains(tag.Label, "Kiosk")) && title.EndsWith(" Demo", StringComparison.Ordinal))
            {
                return title.Substring(0, title.Length - " Demo".Length);
            }

            return title;
        }

        private static List<ReleaseTag> ParseTrailingTags(string canonicalName)
        {
            var tags = new List<ReleaseTag>();
            var cursor = canonicalName.Length;

            while (cursor > 0 && canonicalName[cursor - 1] == ')')
            {
                var openIndex = canonicalName.LastIndexOf('(', cursor - 1);

                if (openIndex < 1 || openIndex >= cursor - 1)
                {
                    break;
                }

                var label = canonicalName.Substring(openIndex + 1, cursor - openIndex - 2);

                if (string.IsNullOrWhiteSpace(label))
                {
                    break;
                }

                tags.Insert(0, new ReleaseTag
                {
                    OpenIndex = openIndex,
                    Label = label
                });

                cursor = openIndex;

                while (cursor > 0 && canonicalName[cursor - 1] == ' ')
                {
                    cursor--;
                }
            }

            return tags;
        }

        private static string BuildSlotLabel(List<ReleaseTag> tags)
        {
            var first = tags[0].Label;
            var rest = tags.Skip(1).Select(tag => $"({tag.Label})");

            return string.Join(" ", new[] { first }.Concat(rest));
        }

        private static bool IsDownloadPlay(string canonicalName)
        {
            return Contains(canonicalName, "Download Play");
        }

        private static bool IsDownloadPlaySource(string systemKey)
        {
            return Contains(systemKey ?? string.Empty, "download-play");
        }

        private static void AddDownloadPlaySourceEntry(NoIntroCatalogPlan plan, NoIntroCatalogEntry entry, string canonicalName)
        {
            var region = TryParseRegionRelease(canonicalName);

            if (region == null)
            {
                plan.StandaloneGames.Add(new NoIntroCatalogStandalonePlan
                {
                    Title = canonicalName,
                    ComponentType = NoIntroRomComponentType.Multiboot
                });

                return;
            }

            GetOrAddGame(plan, entry.SystemKey, region.GameTitle).RegionLanguageComponents.Add(new NoIntroCatalogComponentSlot
            {
                SlotLabel = region.SlotLabel,
                CanonicalName = canonicalName,
                ComponentType = NoIntroRomComponentType.Multiboot
            });
        }

        private static bool IsStandaloneProduct(string canonicalName)
        {
            if (Contains(canonicalName, "(Kiosk)"))
            {
                return false;
            }

            return Contains(canonicalName, "Game Boy Advance Video") ||
                   Contains(canonicalName, "Play-Yan") ||
                   Contains(canonicalName, "DSvision") ||
                   Contains(canonicalName, "[BIOS]") ||
                   Contains(canonicalName, "(BIOS)") ||
                   Contains(canonicalName, " Demo") ||
                   Contains(canonicalName, " Prototype") ||
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

        private class ReleaseTag
        {
            public int OpenIndex { get; set; }
            public string Label { get; set; }
        }
    }
}
