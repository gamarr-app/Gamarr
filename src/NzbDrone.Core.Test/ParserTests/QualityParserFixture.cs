using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class QualityParserFixture : CoreTest
    {
        [SetUp]
        public void Setup()
        {
        }

        // Scene releases
        [TestCase("Game.Name-CODEX")]
        [TestCase("Game.Name-PLAZA")]
        [TestCase("Game.Name-SKIDROW")]
        [TestCase("Game.Name-CPY")]
        [TestCase("Game.Name-EMPRESS")]
        [TestCase("Game.Name-FLT")]
        [TestCase("Game.Name-HOODLUM")]
        [TestCase("Game.Name-RELOADED")]
        [TestCase("Game.Name-TiNYiSO")]
        public void should_parse_scene_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Scene cracked releases
        [TestCase("Game.Name-CRACKED")]
        [TestCase("Game.Name.CRACK.ONLY-CODEX")]
        [TestCase("Game.Name.NO.DRM-GROUP")]
        public void should_parse_scene_cracked_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.SceneCracked);
        }

        // GOG releases (DRM-free)
        [TestCase("Game.Name.GOG")]
        [TestCase("Game.Name-GOG")]
        [TestCase("Game.Name.GOG.RIP")]
        public void should_parse_gog_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.GOG);
        }

        // Repack releases
        [TestCase("Game.Name-FitGirl.Repack")]
        [TestCase("Game.Name.FitGirl")]
        [TestCase("Game.Name-DODI")]
        [TestCase("Game.Name.DODI.Repack")]
        [TestCase("Game.Name-XATAB")]
        [TestCase("Game.Name.ElAmigos")]
        [TestCase("Game.Name.R.G.Mechanics")]
        [TestCase("Game.Name.REPACK")]
        public void should_parse_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Repack with all DLC
        [TestCase("Game.Name.Incl.All.DLC-FitGirl")]
        [TestCase("Game.Name.Complete.Edition-DODI")]
        [TestCase("Game.Name.GOTY-Repack")]
        [TestCase("Game.Name.Ultimate.Edition-FitGirl")]
        public void should_parse_repack_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Steam releases
        [TestCase("Game.Name.Steam.Rip")]
        [TestCase("Game.Name.SteamRip")]
        [TestCase("Game.Name.STEAM.UNLOCKED")]
        public void should_parse_steam_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Steam);
        }

        // Portable releases
        [TestCase("Game.Name.Portable")]
        [TestCase("Game.Name.No.Install")]
        public void should_parse_portable_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Portable);
        }

        // ISO releases
        [TestCase("Game.Name.ISO")]
        [TestCase("Game.Name.Disc.Image")]
        public void should_parse_iso_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.ISO);
        }

        // Preload releases
        [TestCase("Game.Name.Preload")]
        [TestCase("Game.Name.Pre.Release")]
        public void should_parse_preload_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Preload);
        }

        // Update only releases
        [TestCase("Game.Name.Update.Only")]
        [TestCase("Game.Name.Patch.Only")]
        public void should_parse_update_only_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        // Multi-language releases
        [TestCase("Game.Name.MULTI10")]
        [TestCase("Game.Name.MULTi9")]
        [TestCase("Game.Name.MULTiLANGUAGE")]
        public void should_parse_multilang_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.MultiLang);
        }

        // Unknown releases
        [TestCase("Game.Name.Unknown.Format")]
        [TestCase("Random.Title.2024")]
        public void should_parse_unknown_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Proper releases
        [TestCase("Game.Name.PROPER-CODEX")]
        public void should_parse_proper_release(string title)
        {
            var result = QualityParser.ParseQuality(title);
            result.Revision.Version.Should().Be(2);
        }

        // ====== REAL-WORLD QUALITY PARSING TESTS ======

        // Real-world FitGirl Repack releases (basic - no DLC keywords)
        [TestCase("God of War v1 0 1 Day 1 Patch Build 8008283 Bonus OST MULTi18 FitGirl Repack Selective Download from 26-GB")]
        [TestCase("Red Dead Redemption 2 Build 1311.23 MULTi13 FitGirl Repack")]
        [TestCase("Hollow Knight Silksong v1.0.28324 MULTi10 FitGirl Repack")]
        [TestCase("Call of Duty Black Ops 6 v11 1 Campaign Only 4 Bonus OSTs MULTi14 FitGirl Repack")]
        public void should_parse_fitgirl_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world FitGirl Repack releases with DLCs (should be RepackAllDLC)
        [TestCase("ELDEN RING Shadow of the Erdtree Deluxe Edition v1 12 v1 12 1 9 DLCs Bonuses Windows 7 Fix MULTi14 FitGirl Repack Selective Download from 47 4-GB")]
        [TestCase("Starfield v1 7 23 0 2 DLCs Bonus Artbook OST MULTi9 FitGirl Repack Selective Download from 62 2-GB")]
        [TestCase("Hogwarts Legacy Digital Deluxe Edition Build 10461750 All DLCs MULTi14 FitGirl Repack")]
        [TestCase("FINAL FANTASY VII REBIRTH Digital Deluxe Edition All DLCs Bonus Content Unlocker Fixes MULTi11 FitGirl Monkey Repack")]
        [TestCase("Elden Ring Deluxe Edition v 1.10 DLC 2022 PC RePack от FitGirl")]
        public void should_parse_fitgirl_repack_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Real-world DODI Repack releases (basic - no DLC keywords)
        [TestCase("Hollow Knight - DODI Repack")]
        [TestCase("Hogwarts Legacy DODI Repack")]
        [TestCase("Red Dead Redemption Undead Nightmare 2024 PC v1 0 40 57107 Bonus Content MULTi13 From 7 6 GB DODI-Repack")]
        public void should_parse_dodi_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world DODI Repack releases with DLCs (should be RepackAllDLC)
        [TestCase("ELDEN RING Deluxe Edition Shadow of the Erdtree Premium Bundle v1 12 1 12 1 All DLCs Bonus Content MULTi15 From 49 9 GB DODI-Repack")]
        [TestCase("God of War Ragnarok Digital Deluxe Edition All DLCs Bonus Content 6 GB VRam Fix MULTi22 From 68 5 GB DODI-Repack")]
        public void should_parse_dodi_repack_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Real-world XATAB releases (basic repack)
        [TestCase("FIFA 17 Super Deluxe Edition 2016 PC RePack от xatab")]
        [TestCase("Call of Duty WWII 2017 PC Rip от xatab")]
        public void should_parse_xatab_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world XATAB releases with DLC/Complete/GOTY (should be RepackAllDLC)
        [TestCase("Resident Evil 7 Biohazard v 1.03u5 DLC 2017 PC RePack от xatab")]
        [TestCase("Hollow Knight v 1.2.2.1 2 DLC 2017 PC RePack от xatab")]
        [TestCase("Horizon Zero Dawn Complete Edition v.1.06 DLC RePack from xatab")]
        [TestCase("Sekiro Shadows Die Twice GOTY Edition v 1.06 2019 PC Repack-xatab")]
        public void should_parse_xatab_repack_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Real-world R.G. Механики releases (basic repack)
        [TestCase("God of War v 1.0.8 1.0.447.8 2022 PC RePack от R.G. Механики")]
        [TestCase("GTA 5 Grand Theft Auto V v 1.0.1180.1 2015 PC RePack от R.G. Механики")]
        public void should_parse_rg_mechanics_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world R.G. Механики releases with DLC (should be RepackAllDLC)
        [TestCase("Elden Ring Deluxe Edition v 1.02.3 DLC 2022 PC RePack от R.G. Механики")]
        public void should_parse_rg_mechanics_repack_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // "Ultimate Team Edition" doesn't match "Ultimate Edition" pattern, so it's just Repack
        [TestCase("FIFA 15 Ultimate Team Edition Update 8 2014 PC RePack от R.G. Механики")]
        public void should_parse_ultimate_team_as_repack_not_repack_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world scene releases (recognized scene groups: CODEX, PLAZA, SKIDROW, CPY, EMPRESS, FLT, etc.)
        [TestCase("Hogwarts Legacy Deluxe Edition EMPRESS")]
        [TestCase("God of War-FLT")]
        [TestCase("Death Stranding-CPY")]
        [TestCase("Horizon Zero Dawn-CODEX")]
        [TestCase("Sekiro Shadows Die Twice-CODEX")]
        [TestCase("DEATH STRANDING DIRECTORS CUT FLT")]
        [TestCase("Hollow Knight Silksong-FLT")]
        [TestCase("FINAL FANTASY VII REBIRTH FLT")]
        [TestCase("Starfield Shattered Space FLT")]
        public void should_parse_scene_group_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // RUNE releases - RUNE is now recognized as a scene group
        [TestCase("Starfield RUNE")]
        [TestCase("ELDEN RING Shadow of the Erdtree RUNE")]
        [TestCase("Hades II RUNE")]
        [TestCase("God of War Ragnarok-RUNE")]
        public void should_parse_rune_scene_group(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Real-world GOG releases
        [TestCase("DL Hollow Knight L RUS ENG 8 2017 Arcade 1.5.78.11833a 2 DLC GOG")]
        [TestCase("DL The Witcher 3 Wild Hunt Complete Edition L RUS ENG 14 RUS ENG 6 2015 RPG 4.04a redkit update 2 18 DLC GOG")]
        [TestCase("Cyberpunk 2077 GOG")]
        [TestCase("Baldurs Gate 3 live v4 1 1 5932596 76418 win gog")]
        [TestCase("Baldurs Gate 3-GOG")]
        [TestCase("The Witcher 3 Wild Hunt-GOG")]
        [TestCase("Hollow Knight Silksong L RUS ENG 8 2025 Arcade 1.0.29315 GOG")]
        public void should_parse_real_world_gog_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.GOG);
        }

        // Real-world Steam-Rip releases
        [TestCase("Elden Ring Deluxe Edition v 1.03.2 DLC 2022 PC Steam-Rip")]
        [TestCase("GTA 5 Grand Theft Auto V v 1.0.678.1 2015 PC Steam-Rip от Let'sPlay")]
        [TestCase("Final Fantasy XIII Update 3 2014 PC Steam-Rip от Let'sРlay")]
        [TestCase("Hades-Steam-Rip")]
        public void should_parse_steam_rip_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Steam);
        }

        // Real-world portable releases
        [TestCase("DL Elden Ring P RUS ENG 12 ENG 2022 RPG 1.16.0 6 DLC Portable")]
        [TestCase("ELDEN RING NIGHTREIGN RUNE PORTABLE")]
        [TestCase("DL Starfield P ENG 8 ENG 2023 RPG 1.15.222.0 3 DLC Portable")]
        [TestCase("Hogwarts Legacy Digital Deluxe Edition Build 10461750 All DLCs MULTi14 Portable")]
        [TestCase("ELDEN RING NIGHTREIGN 2025 ALL DLC PORTABLE")]
        public void should_parse_real_world_portable_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Portable);
        }

        // Real-world ElAmigos releases
        [TestCase("Hogwarts Legacy Deluxe Edition MULTi11 -ElAmigos")]
        [TestCase("Cyberpunk 2077 MULTi19-ElAmigos")]
        [TestCase("God of War 2022 MULTi19-ElAmigos")]
        [TestCase("Elden Ring v1 16 Deluxe Edition REPACK KaOs")]
        public void should_parse_elamigos_and_kaos_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world update/patch-only releases
        [TestCase("ELDEN RING Shadow of the Erdtree Update v1 16 1 RUNE")]
        [TestCase("Baldurs Gate 3 Update v4 1 1 6848561 RUNE")]
        [TestCase("Cyberpunk 2077 Update v2 3")]
        [TestCase("Starfield Update v1 7 36 0")]
        [TestCase("God of War Ragnarok Update v1 2")]
        [TestCase("Sekiro Shadows Die Twice Update v1 04-CODEX")]
        [TestCase("God of War Update v1.0.1 Day 1 Require God_of_War-FLT")]
        public void should_parse_real_world_update_only_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        // Language pack releases without scene groups return Unknown
        [TestCase("Cyberpunk 2077 Languages Pack v2 10")]
        public void should_return_unknown_for_language_packs_without_scene_group(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Language pack releases with RUNE scene group return Scene
        // Note: Scene group detection takes precedence over "Language Pack" content type
        [TestCase("Baldurs Gate 3 Language Pack v4 1 1 6072089 RUNE")]
        [TestCase("Starfield Language Pack RUNE")]
        public void should_return_scene_for_language_packs_with_rune(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Real-world multi-language releases
        [TestCase("Elden Ring MULTi14")]
        [TestCase("God of War MULTi18")]
        [TestCase("Red Dead Redemption 2 MULTi13")]
        [TestCase("Hogwarts Legacy MULTi14")]
        [TestCase("Starfield MULTi9")]
        [TestCase("FINAL FANTASY VII REBIRTH MULTi11")]
        [TestCase("FIFA 23 MULTi21")]
        public void should_parse_multilang_indicator(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.MultiLang);
        }

        // Real-world Deluxe/Complete/GOTY/Ultimate Edition releases with all DLCs
        [TestCase("Hogwarts Legacy Digital Deluxe Edition Build 10461750 All DLCs Console DLCs Unlocker Bonus OSTs Trainer MULTi14 From 56 8 GB EMPRESS DODI-Repack")]
        [TestCase("ELDEN RING Deluxe Edition Shadow of the Erdtree Premium Bundle v1 12 1 12 1 All DLCs Bonus Content MULTi15 DODI-Repack")]
        [TestCase("Sekiro Shadows Die Twice Game of the Year Edition v1 06 Bonus Content MULTi13 FitGirl Repack")]
        [TestCase("God of War Ragnarok Digital Deluxe Edition All DLCs Bonuses 6 GB VRAM Bypass MULTi22 FitGirl Repack")]
        [TestCase("Red Dead Redemption 2 Ultimate Edition Build 1491 50 UE Unlocker MULTi13 FitGirl Repack")]
        [TestCase("The Witcher 3 Wild Hunt Complete Edition v 4.04 DLCs 2015 PC RePack")]
        [TestCase("FINAL FANTASY XVI Complete Edition v1 03 All DLCs Bonuses MULTi14 FitGirl Repack")]
        public void should_parse_complete_edition_with_all_dlcs(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // GOG takes precedence over RepackAllDLC when both patterns match
        [TestCase("Horizon Zero Dawn Complete Edition v1 11 2 GOG Epic Steam VR Mod Bonus OSTs MULTi20 FitGirl Repack")]
        public void should_parse_gog_over_repack_all_dlc_when_gog_in_title(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.GOG);
        }

        // Real-world crack-only releases
        [TestCase("Hogwarts Legacy Deluxe Edition CRACK ONLY EMPRESS")]
        [TestCase("Hogwarts.Legacy.Deluxe.Edition.CRACK.ONLY-EMPRESS")]
        public void should_parse_crack_only_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.SceneCracked);
        }

        // Real-world Decepticon/Chovka releases (basic repack)
        [TestCase("Call of Duty Modern Warfare II v 9.7 2022 PC RePack от Decepticon")]
        [TestCase("God of War Ragnarok v 1 0 614 134 2024 Decepticon RePack")]
        public void should_parse_russian_repackers_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world Decepticon/Chovka releases with DLCs (should be RepackAllDLC)
        [TestCase("Starfield v 1.8.88.0 DLCs 2023 PC RePack от Chovka Digital Premium Edition")]
        [TestCase("Call of Duty Black Ops 6 v 11.1 DLC's 2024 PC RePack-FitGirl")]
        [TestCase("Resident Evil Village Gold Edition Build 11260452 DLCs 2021 PC RePack от Chovka")]
        public void should_parse_russian_repackers_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Real-world wanterlude releases (basic repack)
        [TestCase("Baldurs Gate 3 Repack by Wanterlude")]
        public void should_parse_wanterlude_repack_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Real-world wanterlude releases with DLCs (should be RepackAllDLC)
        [TestCase("Elden Ring Deluxe Edition v 1.16.1 Reg v 1.16.1 DLCs 2022 PC RePack-Wanterlude")]
        [TestCase("God of War Ragnarök Digital Deluxe Edition v 1.0.668.5700 9.1 DLCs 2024 PC RePack-Wanterlude")]
        [TestCase("Red Dead Redemption 2 Ultimate Edition v 1491.50 DLCs 2019 PC RePack-Wanterlude")]
        public void should_parse_wanterlude_repack_all_dlc_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Test that plain game names with year don't get false quality matches
        [TestCase("Cyberpunk 2077")]
        [TestCase("FIFA 23")]
        [TestCase("God of War 2022")]
        [TestCase("Red Dead Redemption 2")]
        [TestCase("Baldurs Gate 3")]
        public void should_return_unknown_for_plain_titles(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }
    }
}
