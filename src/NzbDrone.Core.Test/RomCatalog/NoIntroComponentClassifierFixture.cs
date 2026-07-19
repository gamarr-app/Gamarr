using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.RomCatalog;

namespace NzbDrone.Core.Test.RomCatalog
{
    [TestFixture]
    public class NoIntroComponentClassifierFixture
    {
        private NoIntroComponentClassifier _subject;

        [SetUp]
        public void Setup()
        {
            _subject = new NoIntroComponentClassifier();
        }

        [TestCase("GBA (by-id)/0001 - F-Zero for Game Boy Advance (Japan).zip", "0001 - F-Zero for Game Boy Advance (Japan).zip", NoIntroRomComponentType.RetailRom, false)]
        [TestCase("Nintendo 3DS/3ds/Mario Kart 7 (USA) (En,Fr,Es).3ds", "Mario Kart 7 (USA) (En,Fr,Es).3ds", NoIntroRomComponentType.RetailRom, false)]
        [TestCase("GBA (e-Reader)/Animal Crossing-e - Series 1 - A-001 - K.K. Slider (USA).zip", "Animal Crossing-e - Series 1 - A-001 - K.K. Slider (USA).zip", NoIntroRomComponentType.EReaderCards, false)]
        [TestCase("GBA (Multiboot)/Animal Crossing - Balloon Fight (USA, Europe).gba", "Animal Crossing - Balloon Fight (USA, Europe).gba", NoIntroRomComponentType.Multiboot, false)]
        [TestCase("GBA (Play-Yan)/Nintendo - Game Boy Advance (Play-Yan).zip", "Nintendo - Game Boy Advance (Play-Yan).zip", NoIntroRomComponentType.Video, false)]
        [TestCase("GBA (Video)/Game Boy Advance Video - Cartoon Network Collection - Volume 1 (USA, Europe).gba", "Game Boy Advance Video - Cartoon Network Collection - Volume 1 (USA, Europe).gba", NoIntroRomComponentType.Video, false)]
        [TestCase("Download Play/Mario Kart DS Demo.nds", "Mario Kart DS Demo.nds", NoIntroRomComponentType.Multiboot, false)]
        [TestCase("DSvision SD cards/Media Title.nds", "Media Title.nds", NoIntroRomComponentType.Video, false)]
        [TestCase("GBA (by-id)/xB02 - [BIOS] Game Boy Advance (World).zip", "xB02 - [BIOS] Game Boy Advance (World).zip", NoIntroRomComponentType.Bios, false)]
        [TestCase("Unknown Folder/Prototype Build.nds", "Prototype Build.nds", NoIntroRomComponentType.RomhackOrUnverified, true)]
        public void should_classify_known_component_shapes(string relativePath, string fileName, NoIntroRomComponentType expectedType, bool expectedFallback)
        {
            var result = _subject.Classify(relativePath, fileName);

            result.ComponentType.Should().Be(expectedType);
            result.IsFallback.Should().Be(expectedFallback);
        }

        [Test]
        public void should_build_RegionLanguageComponents_from_catalog_confirmed_entries_only()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-gba", "Pokemon Emerald Version (USA)"),
                Entry("nintendo-gba", "Pokemon Emerald Version (Germany)"),
                Entry("nintendo-gba", "Pokemon Emerald Version (France)"),
                Entry("nintendo-gba", "Pokemon Emerald Version (Spain)"),
                Entry("nintendo-gba", "Pokemon Emerald Version (Italy)")
            });

            var game = plan.Games.Should().ContainSingle(x => x.SystemKey == "nintendo-gba" && x.GameTitle == "Pokemon Emerald Version").Subject;

            game.RegionLanguageComponents.Should().HaveCount(5);
            game.RegionLanguageComponents.Select(x => x.SlotLabel).Should().BeEquivalentTo("USA", "Germany", "France", "Spain", "Italy");
            game.DownloadPlayComponents.Should().BeEmpty();
            plan.StandaloneGames.Should().BeEmpty();
        }

        [Test]
        public void should_group_multi_parenthetical_region_language_releases_under_one_game()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-ds", "Mario Kart DS (Europe) (En,Fr,De,Es,It)"),
                Entry("nintendo-ds", "Mario Kart DS (USA, Australia) (En,Fr,De,Es,It)"),
                Entry("nintendo-ds", "Mario Kart DS (Japan)"),
                Entry("nintendo-ds", "Mario Kart DS (Korea)")
            });

            var game = plan.Games.Should().ContainSingle(x => x.SystemKey == "nintendo-ds" && x.GameTitle == "Mario Kart DS").Subject;

            game.RegionLanguageComponents.Select(x => x.SlotLabel).Should().BeEquivalentTo(
                "Europe (En,Fr,De,Es,It)",
                "USA, Australia (En,Fr,De,Es,It)",
                "Japan",
                "Korea");
        }

        [Test]
        public void should_keep_revision_tags_on_the_region_component_label()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-gba", "Example Game (USA)"),
                Entry("nintendo-gba", "Example Game (USA) (Rev 1)"),
                Entry("nintendo-gba", "Example Game (USA) (Rev 2)")
            });

            var game = plan.Games.Should().ContainSingle(x => x.SystemKey == "nintendo-gba" && x.GameTitle == "Example Game").Subject;

            game.RegionLanguageComponents.Select(x => x.SlotLabel).Should().BeEquivalentTo(
                "USA",
                "USA (Rev 1)",
                "USA (Rev 2)");
        }

        [Test]
        public void should_build_DownloadPlayComponents_only_when_parent_mapping_is_explicit()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-ds", "Mario Kart DS (USA)"),
                Entry("nintendo-ds", "Mario Kart DS (Download Play)", parentCanonicalName: "Mario Kart DS")
            });

            var game = plan.Games.Should().ContainSingle(x => x.SystemKey == "nintendo-ds" && x.GameTitle == "Mario Kart DS").Subject;

            game.RegionLanguageComponents.Should().ContainSingle(x => x.SlotLabel == "USA");
            game.DownloadPlayComponents.Should().ContainSingle(x => x.SlotLabel == "Download Play");
            plan.StandaloneGames.Should().BeEmpty();
        }

        [Test]
        public void should_build_download_play_source_entries_as_multiboot_game_components()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo---nintendo-ds--download-play", "Mario Kart DS (Europe) (Demo) (Download Station Vol. 1)"),
                Entry("nintendo---nintendo-ds--download-play", "Mario Kart DS (USA) (Demo) (Nintendo Channel)")
            });

            var game = plan.Games.Should().ContainSingle(x => x.SystemKey == "nintendo---nintendo-ds--download-play" && x.GameTitle == "Mario Kart DS").Subject;

            game.RegionLanguageComponents.Select(x => x.SlotLabel).Should().BeEquivalentTo(
                "Europe (Demo) (Download Station Vol. 1)",
                "USA (Demo) (Nintendo Channel)");
            game.RegionLanguageComponents.Should().OnlyContain(x => x.ComponentType == NoIntroRomComponentType.Multiboot);
            plan.StandaloneGames.Should().BeEmpty();
        }

        [Test]
        public void should_build_kiosk_releases_as_game_variants()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-ds", "New Super Mario Bros. Demo (Kiosk)"),
                Entry("nintendo-ds", "Pokemon Distribution 2011 (USA) (Wi-Fi Kiosk) (Save Data)")
            });

            plan.Games.Should().ContainSingle(x => x.GameTitle == "New Super Mario Bros.")
                .Subject.RegionLanguageComponents.Should().ContainSingle(x => x.SlotLabel == "Kiosk");
            plan.Games.Should().ContainSingle(x => x.GameTitle == "Pokemon Distribution 2011")
                .Subject.RegionLanguageComponents.Should().ContainSingle(x => x.SlotLabel == "USA (Wi-Fi Kiosk) (Save Data)");
            plan.StandaloneGames.Should().BeEmpty();
        }

        [Test]
        public void should_build_NoIntroStandaloneGames_for_clean_standalone_products()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-gba", "Game Boy Advance Video - Cartoon Network Collection - Volume 1 (USA)"),
                Entry("nintendo-gba", "Nintendo - Game Boy Advance (Play-Yan)"),
                Entry("nintendo-ds", "DSvision SD cards - Aquarium Tour (Japan)")
            });

            plan.Games.Should().BeEmpty();
            plan.StandaloneGames.Select(x => x.Title).Should().BeEquivalentTo(
                "Game Boy Advance Video - Cartoon Network Collection - Volume 1 (USA)",
                "Nintendo - Game Boy Advance (Play-Yan)",
                "DSvision SD cards - Aquarium Tour (Japan)");
        }

        [Test]
        public void should_build_NoPhantomRegionComponents_when_catalog_release_is_missing()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-gba", "Advance Wars (USA)"),
                Entry("nintendo-gba", "Advance Wars (Germany)")
            });

            var game = plan.Games.Should().ContainSingle(x => x.GameTitle == "Advance Wars").Subject;
            var labels = game.RegionLanguageComponents.Select(x => x.SlotLabel).ToList();

            labels.Should().BeEquivalentTo("USA", "Germany");
            labels.Should().NotContain(new[] { "Europe", "Japan", "France", "Italy", "Spain" });
        }

        [Test]
        public void should_build_NoUnmappedDownloadPlayComponents_as_children()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-ds", "Mario Party DS (Download Play)")
            });

            plan.Games.Should().BeEmpty();
            plan.StandaloneGames.Should().ContainSingle(x => x.Title == "Mario Party DS (Download Play)" && x.ComponentType == NoIntroRomComponentType.Multiboot);
        }

        [Test]
        public void should_build_StandaloneProductsNotBaseGameVariants()
        {
            var plan = _subject.BuildCatalogPlan(new[]
            {
                Entry("nintendo-ds", "Pokemon Dash (USA)"),
                Entry("nintendo-gba", "Nintendo - Game Boy Advance (BIOS) (World)"),
                Entry("nintendo-ds", "New Super Mario Bros. Demo (Kiosk)"),
                Entry("nintendo-ds", "DSvision SD cards - Aquarium Tour (Japan)")
            });

            var game = plan.Games.Should().ContainSingle(x => x.GameTitle == "Pokemon Dash").Subject;

            game.RegionLanguageComponents.Should().ContainSingle(x => x.SlotLabel == "USA");
            game.DownloadPlayComponents.Should().BeEmpty();

            plan.StandaloneGames.Select(x => x.Title).Should().BeEquivalentTo(
                "Nintendo - Game Boy Advance (BIOS) (World)",
                "DSvision SD cards - Aquarium Tour (Japan)");
        }

        private static NoIntroCatalogEntry Entry(string systemKey, string canonicalName, string parentCanonicalName = null)
        {
            return new NoIntroCatalogEntry
            {
                SystemKey = systemKey,
                CanonicalName = canonicalName,
                CanonicalFileName = $"{canonicalName}.zip",
                ParentCanonicalName = parentCanonicalName
            };
        }
    }
}
