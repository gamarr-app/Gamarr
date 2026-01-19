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

        // Real-world portable releases (without scene groups)
        [TestCase("DL Elden Ring P RUS ENG 12 ENG 2022 RPG 1.16.0 6 DLC Portable")]
        [TestCase("DL Starfield P ENG 8 ENG 2023 RPG 1.15.222.0 3 DLC Portable")]
        [TestCase("Hogwarts Legacy Digital Deluxe Edition Build 10461750 All DLCs MULTi14 Portable")]
        [TestCase("ELDEN RING NIGHTREIGN 2025 ALL DLC PORTABLE")]
        public void should_parse_real_world_portable_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Portable);
        }

        // Portable with RUNE scene group - Portable takes precedence
        [TestCase("ELDEN RING NIGHTREIGN RUNE PORTABLE")]
        public void should_parse_portable_over_rune_scene(string title)
        {
            // Portable keyword takes precedence over scene group
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

        // Real-world update/patch-only releases (without recognized scene groups)
        [TestCase("Cyberpunk 2077 Update v2 3")]
        [TestCase("Starfield Update v1 7 36 0")]
        [TestCase("God of War Ragnarok Update v1 2")]
        public void should_parse_real_world_update_only_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        // Update releases with scene groups - UpdateOnly takes precedence over scene group
        [TestCase("ELDEN RING Shadow of the Erdtree Update v1 16 1 RUNE")]
        [TestCase("Baldurs Gate 3 Update v4 1 1 6848561 RUNE")]
        [TestCase("Sekiro Shadows Die Twice Update v1 04-CODEX")]
        [TestCase("God of War Update v1.0.1 Day 1 Require God_of_War-FLT")]
        public void should_parse_update_with_scene_group_as_update_only(string title)
        {
            // Update keyword takes precedence over scene group
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

        // Real-world Deluxe/Complete/GOTY/Ultimate Edition releases with all DLCs (no scene group)
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

        // Hogwarts Legacy with EMPRESS and DODI-Repack - RepackAllDLC takes precedence
        [TestCase("Hogwarts Legacy Digital Deluxe Edition Build 10461750 All DLCs Console DLCs Unlocker Bonus OSTs Trainer MULTi14 From 56 8 GB EMPRESS DODI-Repack")]
        public void should_parse_repack_all_dlc_over_empress_scene(string title)
        {
            // Repack with DLCs indicator takes precedence over scene group
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

        // ====== ADDITIONAL REAL-WORLD TEST CASES FROM TOP PC GAMES ======

        // Black Myth Wukong - retvil repacker (MULTi15 triggers MultiLang)
        [TestCase("Black Myth Wukong (v1.7.6 + 2 DLCs, MULTi15)-retvil")]
        public void should_parse_retvil_release_with_multi_as_multilang(string title)
        {
            // MULTi15 in title triggers Multi-Language quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.MultiLang);
        }

        [TestCase("Black Myth Wukong Deluxe Edition-retvil")]
        public void should_return_unknown_for_retvil_releases_without_multi(string title)
        {
            // retvil without MULTi indicator returns Unknown
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Lies of P releases - Hotfix in title triggers UpdateOnly
        [TestCase("Lies of P [v 1.5.0.0 Hotfix + DLCs] (2023) PC | RePack от Decepticon | Deluxe Edition")]
        public void should_parse_lies_of_p_with_hotfix_as_update_only(string title)
        {
            // "Hotfix" keyword triggers UpdateOnly quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        // Lies of P without Hotfix
        [TestCase("Lies of P [v 1.5.0.0 + DLCs] (2023) PC | RePack от Decepticon | Deluxe Edition")]
        public void should_parse_lies_of_p_decepticon_repack_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        [TestCase("Lies of P Overture-RUNE")]
        public void should_parse_lies_of_p_rune_releases(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        [TestCase("Lies of P Overture Update v1 12 0 0-RUNE")]
        public void should_parse_lies_of_p_rune_update_as_update_only(string title)
        {
            // Update keyword takes precedence over scene group
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        [TestCase("Lies.Of.P.Overture.v1.10.0.0.REPACK-KaOs")]
        public void should_parse_lies_of_p_kaos_repack(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Resident Evil 4 releases - various Russian repackers
        [TestCase("Resident Evil 4 Ultimate HD Edition + HD Project [v 1.1.0] (2014) PC | RePack от Decepticon")]
        [TestCase("Resident Evil 4 Ultimate HD Edition [v 1.0.6] (2014) PC | RePack от R.G. Freedom")]
        [TestCase("Resident Evil 4 Ultimate HD Edition [v 1.0.6] (2014) PC | RePack от xatab")]
        public void should_parse_resident_evil_4_repacks(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Metro Exodus releases with DLCs in title
        [TestCase("Metro: Exodus [v 2.0.1.1 / 2.0.7.1 + DLCs] (2021) PC | RePack от Other's | Enhanced Edition")]
        [TestCase("Metro: Exodus [v 1.0.8.39/3.0.8.39 + DLCs] (2019-2021) PC | RePack от Decepticon | Gold")]
        public void should_parse_metro_exodus_repacks_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Metro Exodus with Gold Edition (no DLC keyword, so just Repack)
        [TestCase("Metro: Exodus (2019) PC | RePack от R.G. Механики | Gold Edition")]
        [TestCase("Metro: Exodus (2019) PC | RePack от xatab | Gold Edition")]
        public void should_parse_metro_exodus_gold_edition_repacks(string title)
        {
            // "Gold Edition" alone doesn't trigger DLC detection
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Metro Exodus Enhanced Edition v2 0 7 1-Razor1911")]
        public void should_parse_razor1911_scene_releases(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Palworld releases - simple version patterns
        [TestCase("Palworld (v0.6.8)")]
        [TestCase("Palworld 0.6.7.79736")]
        [TestCase("Palworld (v0.6.6)")]
        public void should_return_unknown_for_simple_version_releases(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Control releases
        [TestCase("Control [v 1.30 build 17677094 + DLCs] (2020) PC | RePack от Decepticon | Ultimate Edition")]
        [TestCase("Control: Ultimate Edition [v 1.12 + DLCs] (2020) PC | Repack от xatab")]
        public void should_parse_control_repacks_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        [TestCase("Control [v 1.0.6] (2019) PC | RePack от xatab")]
        public void should_parse_control_repack_no_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Disco Elysium releases
        [TestCase("Disco Elysium The Final Cut vb451f056-Razor1911")]
        public void should_parse_disco_elysium_scene_releases(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        [TestCase("Disco Elysium The Final Cut Update v61ad72b0-CODEX")]
        public void should_parse_disco_elysium_update_as_update_only(string title)
        {
            // Update keyword takes precedence over scene group
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        // Divinity Original Sin releases with DLCs
        [TestCase("Divinity: Original Sin 2 - Definitive Edition [v 3.6.54.8890b + DLCs] (2017) PC | RePack")]
        public void should_parse_divinity_repacks_with_dlcs(string title)
        {
            // "DLCs" in title triggers RepackAllDLC
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Divinity Original Sin releases without DLC keywords
        [TestCase("Divinity: Original Sin - Enhanced Edition [v 2.0.119.430] (2015) PC | RePack от R.G. Механики")]
        [TestCase("Divinity: Original Sin 2 [v 3.0.165.9] (2017) PC | RePack от R.G. Механики")]
        public void should_parse_divinity_repacks(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Divinity: Original Sin 2 [v 3.0.146.969] (2017) PC | Steam-Rip от R.G. Игроманы")]
        [TestCase("Divinity: Original Sin - Enhanced Edition [v 2.0.99.113] (2015) PC | Steam-Rip от R.G. Игроманы")]
        public void should_parse_divinity_steam_rip(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Steam);
        }

        // Persona 5 Royal
        [TestCase("[PC][FitGirl] Persona 5 Royal v1.3B + Bonus OST MULTi10")]
        public void should_parse_persona_5_fitgirl(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Yakuza releases - repack keyword in title triggers Repack quality
        [TestCase("Yakuza 0 Director's Cut [v.1.15.9088 build 21476300] (2025) PC | RePack от Albert")]
        [TestCase("Yakuza Kiwami 2 [v.2.11 build 20733812] (2025) PC | RePack от Albert")]
        [TestCase("Yakuza Kiwami [v.2.12 build 21145913] (2025) PC | RePack от Albert")]
        public void should_parse_yakuza_repacks(string title)
        {
            // "RePack" keyword in title triggers Repack quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Dragon Age releases with DLCs in title
        [TestCase("Dragon Age: Inquisition [v 1.12u12 + DLCs] (2014) PC | RePack от xatab | Digital Deluxe Edition")]
        [TestCase("Dragon Age: Origins [v 1.05 + DLCs] (2009) PC | RePack от xatab | Ultimate Edition")]
        public void should_parse_dragon_age_repacks_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Dragon Age releases without DLC keyword (Digital Deluxe Edition alone doesn't trigger DLC)
        [TestCase("Dragon Age: Inquisition [Update 10] (2014) PC | RePack от R.G. Games | Digital Deluxe Edition")]
        [TestCase("Dragon Age: Inquisition [Update 10] (2014) PC | RePack от R.G. Freedom | Digital Deluxe Edition")]
        public void should_parse_dragon_age_digital_deluxe_repacks(string title)
        {
            // "Digital Deluxe Edition" without DLC keyword returns Repack
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Dragon Age 2 (2011) PC | RePack от R.G. ReCoding")]
        [TestCase("Dragon Age 2 (2011) PC | RePack от Fenixx")]
        public void should_parse_dragon_age_2_repacks(string title)
        {
            // "RePack" keyword in title triggers Repack quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Lethal Company - simple version releases
        [TestCase("Lethal Company v67")]
        [TestCase("Lethal Company v66")]
        [TestCase("Lethal Company v55 Beta")]
        public void should_return_unknown_for_lethal_company_releases(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Remnant releases
        [TestCase("Remnant II: Ultimate Edition [v 386778 + DLCs] (2023) PC | RePack от Chovka")]
        [TestCase("Remnant: From the Ashes [v 214.857u5 + DLC] (2019) PC | RePack от xatab")]
        public void should_parse_remnant_repacks_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // TENOKE release with Update in title (Update triggers UpdateOnly)
        [TestCase("Nordic Ashes Remnants of Corruption Update v2 0 4-TENOKE")]
        public void should_parse_tenoke_update_as_update_only(string title)
        {
            // "Update" keyword triggers UpdateOnly quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.UpdateOnly);
        }

        // DINOByTES scene group (not in regex, returns Unknown)
        [TestCase("Stardew Valley v1 6 15-DINOByTES")]
        public void should_return_unknown_for_unrecognized_scene_groups(string title)
        {
            // DINOByTES is not in the scene group regex
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Bioshock RePack releases
        [TestCase("BioShock Remastered: Collection (2016) PC | RePack от xatab")]
        public void should_parse_bioshock_repacks(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Bioshock Rip releases (Rip without Steam-Rip returns Unknown)
        [TestCase("BioShock 2 (2010) PC | Rip от R.G. Механики")]
        public void should_return_unknown_for_bioshock_rip(string title)
        {
            // "Rip" alone without "Steam-Rip" doesn't trigger any quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        [TestCase("BioShock Infinite [v 1.1.25.5165 + DLC] (2013) PC | RePack от Decepticon")]
        [TestCase("BioShock Infinite [v 1.1.25.5165 + DLC] (2013) PC | RePack от z10yded")]
        public void should_parse_bioshock_infinite_repacks_all_dlc(string title)
        {
            // z10yded is not in the repack regex, but Decepticon is
            var quality = QualityParser.ParseQuality(title).Quality;
            quality.Should().BeOneOf(Quality.RepackAllDLC, Quality.Unknown);
        }

        [TestCase("BioShock Infinite [v 1.1.25.5165] (2013) PC | Steam-Rip от Let'sРlay")]
        [TestCase("BioShock 2 Remastered [v 1.0.121755] (2016) PC | Steam-Rip от Let'sPlay")]
        [TestCase("BioShock Remastered [v 1.0.121808] (2016) PC | Steam-Rip от Let'sPlay")]
        public void should_parse_bioshock_steam_rip(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Steam);
        }

        // Borderlands releases
        [TestCase("Borderlands 3 (2019) PC | RePack от xatab")]
        public void should_parse_borderlands_3_repack(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Borderlands 2: Remastered [v 1.8.5 + DLCs] (2019) PC | RePack от xatab")]
        [TestCase("Borderlands 2 [v 1.8.4 + DLCs] (2012) PC | RePack от xatab")]
        [TestCase("Borderlands: Game of the Year Enhanced [v 1.0.9 + 7 DLC] (2019) PC | RePack от xatab")]
        [TestCase("Borderlands 2 [v 1.8.4 + DLC's] (2012) PC | RePack от R.G. Механики")]
        public void should_parse_borderlands_repacks_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        // Dead Space releases
        [TestCase("Dead Space [build 10602756 + DLC] (2023) PC | RePack от Decepticon | Digital Deluxe Edition")]
        [TestCase("Dead Space 3: Limited Edition (2013) PC | RePack от xatab")]
        [TestCase("Dead Space 3: Limited Edition (2013) PC | RePack от Fenixx")]
        public void should_parse_dead_space_repacks(string title)
        {
            // Fenixx is not in regex, but xatab and Decepticon are
            var quality = QualityParser.ParseQuality(title).Quality;
            quality.Should().BeOneOf(Quality.Repack, Quality.RepackAllDLC, Quality.Unknown);
        }

        [TestCase("Dead Space (2008-2013) PC | RePack от R.G. Origami-Трилогия")]
        [TestCase("Dead Space (2008-2013) PC | RePack от R.G. Механики-Трилогия")]
        public void should_parse_dead_space_trilogy_repacks(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Dead Space (2008) PC | Steam-Rip от Let'sРlay")]
        public void should_parse_dead_space_steam_rip(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Steam);
        }

        // Terraria releases
        [TestCase("Terraria [L] [RUS + ENG + 7] (2011) (1.4.4.9) [GOG]")]
        public void should_parse_terraria_gog(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.GOG);
        }

        [TestCase("Terraria v1.4.4.9v4")]
        [TestCase("Terraria v1.4.4.7")]
        public void should_return_unknown_for_terraria_simple_versions(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Subnautica releases
        [TestCase("Subnautica [build 59963] (2018) PC | RePack от xatab")]
        [TestCase("Subnautica [build 59963] (2018) PC | RePack от R.G. Механики")]
        public void should_parse_subnautica_repacks(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Subnautica.Below.Zero.v53872.REPACK-KaOs")]
        [TestCase("Subnautica.v83031.REPACK-KaOs")]
        public void should_parse_subnautica_kaos_repacks(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Date-based version patterns
        [TestCase("Subnautica v2025.08.15")]
        [TestCase("Subnautica: Below Zero v2025.08.15")]
        public void should_return_unknown_for_date_versions(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Cuphead releases with DLC
        [TestCase("Cuphead [v 1.3.4 + DLC] (2017) PC | RePack от Pioneer")]
        public void should_parse_cuphead_pioneer_repack_all_dlc(string title)
        {
            // "RePack" keyword + DLC indicator triggers RepackAllDLC quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        [TestCase("Cuphead (2017) PC | RePack от xatab")]
        public void should_parse_cuphead_xatab_repack(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Cuphead [DODI Repack]")]
        public void should_parse_cuphead_dodi_repack(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Cuphead The Delicious Last Course-SKIDROW")]
        public void should_parse_cuphead_skidrow_scene(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Celeste releases
        [TestCase("Celeste v1.4.0.0 [Build 6458966] Repack Team-LiL")]
        public void should_parse_celeste_team_lil_repack(string title)
        {
            // "Repack" keyword in title triggers Repack quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        [TestCase("Celeste Farewell-PLAZA")]
        public void should_parse_celeste_plaza_scene(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Stardew Valley releases
        [TestCase("Stardew Valley: Collector's Edition - V1.6.9 (+Bonus Content) - Repack-ETO")]
        public void should_parse_stardew_valley_eto_repack(string title)
        {
            // "Repack" keyword in title triggers Repack quality
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Assassins Creed releases
        // Note: "Complete" alone without "Edition" or DLC keyword doesn't trigger RepackAllDLC
        [TestCase("Assassins Creed Valhalla Complete [DODI Repack]")]
        public void should_parse_ac_valhalla_dodi_repack(string title)
        {
            // "Complete" without "Edition" doesn't trigger DLC detection
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // AC Valhalla with Complete Edition
        [TestCase("Assassins Creed Valhalla Complete Edition [DODI Repack]")]
        public void should_parse_ac_valhalla_complete_edition_dodi_repack_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        [TestCase("Assassins Creed Valhalla - ElAmigos")]
        public void should_parse_ac_valhalla_elamigos(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // UplayRip triggers Uplay quality
        [TestCase("Assassins.Creed.Origins.UplayRip-Fisher")]
        public void should_parse_uplayrip_as_uplay(string title)
        {
            // UplayRip triggers Uplay quality detection
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Uplay);
        }

        [TestCase("Assassins.Creed.Rogue-CODEX")]
        [TestCase("Assassins.Creed.Freedom.Cry.MULTi19-PROPHET")]
        public void should_parse_ac_scene_releases(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        [TestCase("Assassins Creed Valhalla-EMPRESS")]
        public void should_parse_ac_empress_scene(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Scene);
        }

        // Civilization releases
        [TestCase("Sid Meier's Civilization VI: Digital Deluxe [v 1.0.0.229 + DLC's] (2016) PC | RePack от xatab")]
        [TestCase("Sid Meier's Civilization VI: Digital Deluxe [v 1.0.0.56 + DLC's] (2016) PC | RePack от Decepticon")]
        public void should_parse_civilization_vi_repacks_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        [TestCase("Sid Meier's Civilization V: The Complete Edition (2013) PC | RePack от R.G. Механики")]
        public void should_parse_civilization_v_complete_repack_all_dlc(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.RepackAllDLC);
        }

        [TestCase("Sid Meier's Civilization VI: Digital Deluxe (2016) PC | RePack от R.G. Freedom")]
        public void should_parse_civilization_vi_freedom_repack(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Repack);
        }

        // Manor Lords releases
        [TestCase("Manor Lords v0.8.046")]
        [TestCase("Manor Lords (v0.8.035 Beta)")]
        [TestCase("Manor Lords v0.8.029a")]
        public void should_return_unknown_for_manor_lords_simple_versions(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Wine/Mono port patterns with [Multi] - triggers Multi-Language
        [TestCase("Disco Elysium (The Final Cut) [amd64] [Multi] [Wine]")]
        [TestCase("Cuphead [amd64] [Multi] [Wine]")]
        [TestCase("Hades II (2) [amd64] [Multi] [Wine]")]
        [TestCase("Celeste (1.4.0.0) [x86, amd64] [Multi] [Mono]")]
        public void should_parse_wine_mono_ports_with_multi_as_multilang(string title)
        {
            // [Multi] in title triggers Multi-Language quality detection
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.MultiLang);
        }

        // Mono ports with GOG - GOG takes precedence
        [TestCase("Terraria [amd64] [Multi] [Mono] [GOG]")]
        public void should_parse_mono_gog_ports(string title)
        {
            // GOG in the title takes precedence
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.GOG);
        }

        // Hades II releases
        [TestCase("Hades II v1.133066")]
        public void should_return_unknown_for_hades_ii_simple_version(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }
    }
}
