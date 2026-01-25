using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ParserFixture : CoreTest
    {
        /*Fucked-up hall of shame,
         * WWE.Wrestlemania.27.PPV.HDTV.XviD-KYR
         * Unreported.World.Chinas.Lost.Sons.WS.PDTV.XviD-FTP
         * [TestCase("Big Time Rush 1x01 to 10 480i DD2 0 Sianto", "Big Time Rush", 1, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 10)]
         * [TestCase("Desparate Housewives - S07E22 - 7x23 - And Lots of Security.. [HDTV-720p].mkv", "Desparate Housewives", 7, new[] { 22, 23 }, 2)]
         * [TestCase("S07E22 - 7x23 - And Lots of Security.. [HDTV-720p].mkv", "", 7, new[] { 22, 23 }, 2)]
         * (Game of Thrones s03 e - "Game of Thrones Season 3 Episode 10"
         * The.Man.of.Steel.1994-05.33.hybrid.DreamGirl-Novus-HD
         * Superman.-.The.Man.of.Steel.1994-06.34.hybrid.DreamGirl-Novus-HD
         * Superman.-.The.Man.of.Steel.1994-05.33.hybrid.DreamGirl-Novus-HD
         * Constantine S1-E1-WEB-DL-1080p-NZBgeek
         * [TestCase("Valana la Game FRENCH BluRay 720p 2016 kjhlj", "Valana la Game")]  Removed 2021-12-19 as this / the regex for this was breaking all games w/ french in title
         */

        [Test]
        public void should_remove_accents_from_title()
        {
            const string title = "Carniv\u00E0le";

            title.CleanGameTitle().Should().Be("carnivale");
        }

        [TestCase("The.Game.from.U.N.C.L.E.2015.1080p.BluRay.x264-SPARKS", "The Game from U.N.C.L.E.")]
        [TestCase("1776.1979.EXTENDED.720p.BluRay.X264-AMIABLE", "1776")]
        [TestCase("MY GAME (2016) [R][Action, Horror][720p.WEB-DL.AVC.8Bit.6ch.AC3].mkv", "MY GAME")]
        [TestCase("R.I.P.D.2013.720p.BluRay.x264-SPARKS", "R.I.P.D.")]
        [TestCase("V.H.S.2.2013.LIMITED.720p.BluRay.x264-GECKOS", "V.H.S. 2")]
        [TestCase("This Is A Game (1999) [IGDB #12345] <Genre, Genre, Genre> {STUDIO} !DEVELOPER +MORE_SILLY_STUFF_NO_ONE_NEEDS ?", "This Is A Game")]
        [TestCase("We Are the Game!.2013.720p.H264.mkv", "We Are the Game!")]
        [TestCase("(500).Days.Of.Game.(2009).DTS.1080p.BluRay.x264.NLsubs", "(500) Days Of Game")]
        [TestCase("To.Live.and.Game.in.L.A.1985.1080p.BluRay", "To Live and Game in L.A.")]
        [TestCase("A.I.Artificial.Game.(2001)", "A.I. Artificial Game")]
        [TestCase("A.Game.Name.(1998)", "A Game Name")]
        [TestCase("www.Torrenting.com - Game.2008.720p.X264-DIMENSION", "Game")]
        [TestCase("www.5GameRulz.tc - Game (2000) Malayalam HQ HDRip - x264 - AAC - 700MB.mkv", "Game")]
        [TestCase("Game: The Game World 2013", "Game: The Game World")]
        [TestCase("Game.The.Final.Chapter.2016", "Game The Final Chapter")]
        [TestCase("Der.Game.James.German.Bluray.FuckYou.Pso.Why.cant.you.follow.scene.rules.1998", "Der Game James")]
        [TestCase("Game.German.DL.AC3.Dubbed..BluRay.x264-PsO", "Game")]
        [TestCase("Valana la Game TRUEFRENCH BluRay 720p 2016 kjhlj", "Valana la Game")]
        [TestCase("Mission Game: Rogue Game (2015)�[XviD - Ita Ac3 - SoftSub Ita]azione, spionaggio, thriller *Prima Visione* Team mulnic Tom Cruise", "Mission Game: Rogue Game")]
        [TestCase("Game.Game.2000.FRENCH..BluRay.-AiRLiNE", "Game Game")]
        [TestCase("My Game 1999 German Bluray", "My Game")]
        [TestCase("Leaving Game by Game (1897) [DVD].mp4", "Leaving Game by Game")]
        [TestCase("Game.2018.1080p.AMZN.WEB-DL.DD5.1.H.264-NTG", "Game")]
        [TestCase("Game.Title.Imax.2018.1080p.AMZN.WEB-DL.DD5.1.H.264-NTG", "Game Title")]
        [TestCase("World.Game.Z.EXTENDED.2013.German.DL.1080p.BluRay.AVC-XANOR", "World Game Z")]
        [TestCase("World.Game.Z.2.EXTENDED.2013.German.DL.1080p.BluRay.AVC-XANOR", "World Game Z 2")]
        [TestCase("G.I.Game.Game.2013.THEATRiCAL.COMPLETE.BLURAY-GLiMMER", "G.I. Game Game")]
        [TestCase("www.Torrenting.org - Game.2008.720p.X264-DIMENSION", "Game")]
        [TestCase("The.French.Game.2013.720p.BluRay.x264 - ROUGH[PublicHD]", "The French Game")]
        [TestCase("The.Good.German.2006.720p.BluRay.x264-RlsGrp", "The Good German", Description = "Hardcoded to exclude from German regex")]

        // Game-style release names with version and release groups
        [TestCase("Cyberpunk.2077.v2.1-CODEX", "Cyberpunk 2077")]
        [TestCase("Elden.Ring.v1.10-FitGirl", "Elden Ring")]
        [TestCase("The.Witcher.3.Wild.Hunt.v4.04-PLAZA", "The Witcher 3 Wild Hunt")]
        [TestCase("Half-Life.2-RELOADED", "Half-Life 2")]
        [TestCase("Baldurs.Gate.3.v4.1.1.5009956-EMPRESS", "Baldurs Gate 3")]

        // Date-based version formats (common for indie/early access games)
        [TestCase("Hades II v2025.08.03", "Hades II")]
        [TestCase("The Witness v24.01.2019", "The Witness")] // European date format DD.MM.YYYY
        [TestCase("Vampire Survivors v1.10.103", "Vampire Survivors")]

        // Parenthesized version formats
        [TestCase("Hades II (v2025.06.18)", "Hades II")]
        [TestCase("Stardew Valley (v1.6.8)", "Stardew Valley")]

        // Russian tracker format with sequel indicator stripped
        [TestCase("[DL] Hades II (2) [P] [RUS + ENG + 13 / ENG] (2025, TPS) (1.133066) [Portable]", "Hades II")]
        [TestCase("[DL] The Witness [L] [RUS + ENG + 13 / ENG] (2016, Adventure) (21-12-2017) [GOG]", "The Witness")]
        [TestCase("[CD] Half-Life 2 [P] [RUS + ENG / ENG] (2004, FPS) (1.0.1.0) [Tycoon]", "Half-Life 2")]
        [TestCase("[DL] Elden Ring [P] [RUS + ENG + 12 / ENG] (2022, RPG) (1.16.0 + 6 DLC) [Portable]", "Elden Ring")]

        // Public tracker formats with versions
        [TestCase("ELDEN RING Deluxe Edition v1.03.2", "ELDEN RING Deluxe Edition")]
        [TestCase("ELDEN RING v1 04 1-FLT", "ELDEN RING")]
        [TestCase("Elden Ring Deluxe Edition v 1 03 2", "Elden Ring Deluxe Edition")]

        // Repack formats
        [TestCase("ELDEN RING Shadow of the Erdtree Deluxe Edition v1 12 v1 12 1 9 DLCs Bonuses Windows 7 Fix MULTi14 FitGirl Repack", "ELDEN RING Shadow of the Erdtree Deluxe Edition")]
        [TestCase("ELDEN RING Deluxe Edition v1 10 1 DLC Bonus Content Windows 7 Fix MULTi14 FitGirl Repack", "ELDEN RING Deluxe Edition")]
        [TestCase("Elden Ring Nightreign Deluxe Edition v1 03 All DLCs Bonus Content MULTi15 From 20 2 GB DODI-Repack", "Elden Ring Nightreign Deluxe Edition")]

        // Parenthesized metadata with bracketed repack group
        [TestCase("DARQ: Complete Edition (v1.3 + 2 DLCs, MULTi19) [FitGirl Repack]", "DARQ: Complete Edition")]
        [TestCase("Hollow Knight (v1.5.78 + All DLCs) [DODI Repack]", "Hollow Knight")]
        [TestCase("Celeste (v1.4.0.0, MULTi15) [FitGirl Repack]", "Celeste")]

        // Scene releases with hyphenated groups
        [TestCase("ELDEN RING-PLAZA", "ELDEN RING")]
        [TestCase("ELDEN RING Shadow of the Erdtree-RUNE", "ELDEN RING Shadow of the Erdtree")]
        public void should_parse_game_title(string postTitle, string title)
        {
            Parser.Parser.ParseGameTitle(postTitle).PrimaryGameTitle.Should().Be(title);
        }

        [TestCase("[MTBB] Kimi no Na wa. (2016) v2 [97681524].mkv", "Kimi no Na wa", "MTBB", 2016)]
        [TestCase("[sam] Toward the Terra (1980) [BD 1080p TrueHD].mkv", "Toward the Terra", "sam", 1980)]
        public void should_parse_anime_game_title(string postTitle, string title, string releaseGroup, int year)
        {
            var game = Parser.Parser.ParseGameTitle(postTitle);
            using (new AssertionScope())
            {
                game.PrimaryGameTitle.Should().Be(title);
                game.ReleaseGroup.Should().Be(releaseGroup);
                game.Year.Should().Be(year);
            }
        }

        [TestCase("[Arid] Cowboy Bebop - Knockin' on Heaven's Door v2 [00F4CDA0].mkv", "Cowboy Bebop - Knockin' on Heaven's Door", "Arid")]
        [TestCase("[Baws] Evangelion 1.11 - You Are (Not) Alone v2 (1080p BD HEVC FLAC) [BF42B1C8].mkv", "Evangelion 1 11 - You Are (Not) Alone", "Baws")]
        [TestCase("[Arid] 5 Centimeters per Second (BDRip 1920x1080 Hi10 FLAC) [FD8B6FF2].mkv", "5 Centimeters per Second", "Arid")]
        [TestCase("[Baws] Evangelion 2.22 - You Can (Not) Advance (1080p BD HEVC FLAC) [56E7A5B8].mkv", "Evangelion 2 22 - You Can (Not) Advance", "Baws")]
        [TestCase("[sam] Goblin Slayer - Goblin's Crown [BD 1080p FLAC] [CD298D48].mkv", "Goblin Slayer - Goblin's Crown", "sam")]
        [TestCase("[Kulot] Violet Evergarden Gaiden Eien to Jidou Shuki Ningyou [Dual-Audio][BDRip 1920x804 HEVC FLACx2] [91FC62A8].mkv", "Violet Evergarden Gaiden Eien to Jidou Shuki Ningyou", "Kulot")]
        public void should_parse_anime_game_title_without_year(string postTitle, string title, string releaseGroup)
        {
            var game = Parser.Parser.ParseGameTitle(postTitle);
            using (new AssertionScope())
            {
                game.PrimaryGameTitle.Should().Be(title);
                game.ReleaseGroup.Should().Be(releaseGroup);
            }
        }

        [TestCase("Game.Aufbruch.nach.Pandora.Extended.2009.German.DTS.720p.BluRay.x264-SoW", "Game Aufbruch nach Pandora", "Extended", 2009)]
        [TestCase("Drop.Game.1994.German.AC3D.DL.720p.BluRay.x264-KLASSiGERHD", "Drop Game", "", 1994)]
        [TestCase("Kick.Game.2.2013.German.DTS.DL.720p.BluRay.x264-Pate", "Kick Game 2", "", 2013)]
        [TestCase("Game.Hills.2019.German.DL.AC3.Dubbed.1080p.BluRay.x264-muhHD", "Game Hills", "", 2019)]
        [TestCase("96.Hours.Game.3.EXTENDED.2014.German.DL.1080p.BluRay.x264-ENCOUNTERS", "96 Hours Game 3", "EXTENDED", 2014)]
        [TestCase("Game.War.Q.EXTENDED.CUT.2013.German.DL.1080p.BluRay.x264-HQX", "Game War Q", "EXTENDED CUT", 2013)]
        [TestCase("Sin.Game.2005.RECUT.EXTENDED.German.DL.1080p.BluRay.x264-DETAiLS", "Sin Game", "RECUT EXTENDED", 2005)]
        [TestCase("2.Game.in.L.A.1996.GERMAN.DL.720p.WEB.H264-SOV", "2 Game in L.A.", "", 1996)]
        [TestCase("8.2019.GERMAN.720p.BluRay.x264-UNiVERSUM", "8", "", 2019)]
        [TestCase("Life.Game.2014.German.DL.PAL.DVDR-ETM", "Life Game", "", 2014)]
        [TestCase("Joe.Game.2.EXTENDED.EDITION.2015.German.DL.PAL.DVDR-ETM", "Joe Game 2", "EXTENDED EDITION", 2015)]
        [TestCase("Game.EXTENDED.2011.HDRip.AC3.German.XviD-POE", "Game", "EXTENDED", 2011)]

        // Special cases (see description)
        [TestCase("Game.Klasse.von.1999.1990.German.720p.HDTV.x264-NORETAiL", "Game Klasse von 1999", "", 1990, Description = "year in the title")]
        [TestCase("Game.Squad.2016.EXTENDED.German.DL.AC3.BDRip.x264-hqc", "Game Squad", "EXTENDED", 2016, Description = "edition after year")]
        [TestCase("Game.and.Game.2010.Extended.Cut.German.DTS.DL.720p.BluRay.x264-HDS", "Game and Game", "Extended Cut", 2010, Description = "edition after year")]
        [TestCase("Der.Game.James.German.Bluray.FuckYou.Pso.Why.cant.you.follow.scene.rules.1998", "Der Game James", "", 1998, Description = "year at the end")]
        [TestCase("Der.Game.Eine.Unerwartete.Reise.Extended.German.720p.BluRay.x264-EXQUiSiTE", "Der Game Eine Unerwartete Reise", "Extended", 0, Description = "no year & edition")]
        [TestCase("Game.Weg.des.Kriegers.EXTENDED.German.720p.BluRay.x264-EXQUiSiTE", "Game Weg des Kriegers", "EXTENDED", 0, Description = "no year & edition")]
        [TestCase("Die.Unfassbaren.Game.Name.EXTENDED.German.DTS.720p.BluRay.x264-RHD", "Die Unfassbaren Game Name", "EXTENDED", 0, Description = "no year & edition")]
        [TestCase("Die Unfassbaren Game Name EXTENDED German DTS 720p BluRay x264-RHD", "Die Unfassbaren Game Name", "EXTENDED", 0, Description = "no year & edition & without dots")]
        [TestCase("Passengers.German.DL.AC3.Dubbed..BluRay.x264-PsO", "Passengers", "", 0, Description = "no year")]
        [TestCase("Das.A.Team.Der.Film.Extended.Cut.German.720p.BluRay.x264-ANCIENT", "Das A Team Der Film", "Extended Cut", 0, Description = "no year")]
        [TestCase("Cars.2.German.DL.720p.BluRay.x264-EmpireHD", "Cars 2", "", 0, Description = "no year")]
        [TestCase("Die.fantastische.Reise.des.Dr.Dolittle.2020.German.DL.LD.1080p.WEBRip.x264-PRD", "Die fantastische Reise des Dr. Dolittle", "", 2020, Description = "dot after dr")]
        [TestCase("Der.Film.deines.Lebens.German.2011.PAL.DVDR-ETM", "Der Film deines Lebens", "", 2011, Description = "year at wrong position")]
        [TestCase("Kick.Ass.2.2013.German.DTS.DL.720p.BluRay.x264-Pate_", "Kick Ass 2", "", 2013, Description = "underscore at the end")]
        [TestCase("The.Good.German.2006.GERMAN.720p.HDTV.x264-RLsGrp", "The Good German", "", 2006, Description = "German in the title")]
        public void should_parse_german_game(string postTitle, string title, string edition, int year)
        {
            var game = Parser.Parser.ParseGameTitle(postTitle);
            using (new AssertionScope())
            {
                game.PrimaryGameTitle.Should().Be(title);
                game.Edition.Should().Be(edition);
                game.Year.Should().Be(year);
            }
        }

        [TestCase("L'hypothèse.du.game.volé.AKA.The.Hypothesis.of.the.Game.Title.1978.1080p.CINET.WEB-DL.AAC2.0.x264-Cinefeel.mkv",
            new string[]
            {
                "L'hypothèse du game volé AKA The Hypothesis of the Game Title",
                "L'hypothèse du game volé",
                "The Hypothesis of the Game Title"
            })]
        [TestCase("Skjegg.AKA.Rox.Beard.1965.CD1.CRiTERiON.DVDRip.XviD-KG.avi",
            new string[]
            {
                "Skjegg AKA Rox Beard",
                "Skjegg",
                "Rox Beard"
            })]
        [TestCase("Kjeller.chitai.AKA.Basement.of.Shame.1956.1080p.BluRay.x264.FLAC.1.0.mkv",
            new string[]
            {
                "Kjeller chitai AKA Basement of Shame",
                "Kjeller chitai",
                "Basement of Shame"
            })]
        [TestCase("Gamarr.Under.Water.(aka.Beneath.the.Code.Freeze).1997.DVDRip.x264.CG-Grzechsin.mkv",
            new string[]
            {
                "Gamarr Under Water (aka Beneath the Code Freeze)",
                "Gamarr Under Water",
                "Beneath the Code Freeze"
            })]
        [TestCase("Gamarr.prodavet. AKA.Gamarr.Shift.2005.DVDRip.x264-HANDJOB.mkv",
            new string[]
            {
                "Gamarr prodavet  AKA Gamarr Shift",
                "Gamarr prodavet",
                "Gamarr Shift"
            })]
        [TestCase("AKA.2002.DVDRip.x264-HANDJOB.mkv",
            new string[]
            {
                "AKA"
            })]
        [TestCase("KillRoyWasHere.2000.BluRay.1080p.DTS.x264.dxva-EuReKA.mkv",
            new string[]
            {
                "KillRoyWasHere"
            })]
        [TestCase("Aka Rox (2008).avi",
            new string[]
            {
                "Aka Rox"
            })]
        [TestCase("Return Earth to Normal 'em High aka World 2 (2022) 1080p.mp4",
            new string[]
            {
                "Return Earth to Normal 'em High aka World 2",
                "Return Earth to Normal 'em High",
                "World 2"
            })]
        [TestCase("Енола Голмс / Enola Holmes (2020) UHD WEB-DL 2160p 4K HDR H.265 Ukr/Eng | Sub Ukr/Eng",
            new string[]
            {
                "Енола Голмс / Enola Holmes",
                "Енола Голмс",
                "Enola Holmes"
            })]
        [TestCase("Mon cousin a.k.a. My Cousin 2020 1080p Blu-ray DD 5.1 x264.mkv",
            new string[]
            {
                "Mon cousin AKA My Cousin",
                "Mon cousin",
                "My Cousin"
            })]
        [TestCase("Sydney A.K.A. Hard Eight 1996 1080p AMZN WEB-DL DD+ 2.0 H.264.mkv",
            new string[]
            {
                "Sydney AKA Hard Eight",
                "Sydney",
                "Hard Eight"
            })]
        public void should_parse_game_alternative_titles(string postTitle, string[] parsedTitles)
        {
            var gameInfo = Parser.Parser.ParseGameTitle(postTitle, true);

            gameInfo.GameTitles.Count.Should().Be(parsedTitles.Length);

            for (var i = 0; i < gameInfo.GameTitles.Count; i += 1)
            {
                gameInfo.GameTitles[i].Should().Be(parsedTitles[i]);
            }
        }

        [TestCase("(1995) Game Name", "Game Name")]
        public void should_parse_game_folder_name(string postTitle, string title)
        {
            Parser.Parser.ParseGameTitle(postTitle, true).PrimaryGameTitle.Should().Be(title);
        }

        [TestCase("1776.1979.EXTENDED.720p.BluRay.X264-AMIABLE", 1979)]
        [TestCase("Game Name FRENCH BluRay 720p 2016 kjhlj", 2016)]
        [TestCase("Der.Game.German.Bluray.FuckYou.Pso.Why.cant.you.follow.scene.rules.1998", 1998)]
        [TestCase("Game Name (1897) [DVD].mp4", 1897)]
        [TestCase("World Game Z Game [2023]", 2023)]
        public void should_parse_game_year(string postTitle, int year)
        {
            Parser.Parser.ParseGameTitle(postTitle).Year.Should().Be(year);
        }

        [TestCase("Game Name (2016) {igdbid-43074}", 43074)]
        [TestCase("Game Name (2016) [igdb-43074]", 43074)]
        [TestCase("Game Name (2016) {igdb-43074}", 43074)]
        [TestCase("Game Name (2016) {igdb-2020}", 2020)]
        public void should_parse_igdb_id(string postTitle, int igdbId)
        {
            Parser.Parser.ParseGameTitle(postTitle).IgdbId.Should().Be(igdbId);
        }

        [TestCase("The.Italian.Game.2025.720p.BluRay.X264-AMIABLE")]
        [TestCase("The.French.Game.2013.720p.BluRay.x264 - ROUGH[PublicHD]")]
        [TestCase("The.German.Doctor.2013.LIMITED.DVDRip.x264-RedBlade", Description = "When German is not followed by a year or a SCENE word it is not matched")]
        [TestCase("The.Good.German.2006.720p.HDTV.x264-TVP", Description = "The Good German is hardcoded not to match")]
        [TestCase("German.Lancers.2019.720p.BluRay.x264-UNiVERSUM", Description = "German at the beginning is never matched")]
        [TestCase("The.German.2019.720p.BluRay.x264-UNiVERSUM", Description = "The German is hardcoded not to match")]
        [TestCase("Game Name (2016) BluRay 1080p DTS-ES AC3 x264-3Li", Description = "DTS-ES should not match ES (Spanish)")]
        public void should_not_parse_wrong_language_in_title(string postTitle)
        {
            var parsed = Parser.Parser.ParseGameTitle(postTitle, true);
            parsed.Languages.Count.Should().Be(1);
            parsed.Languages.First().Should().Be(Language.Unknown);
        }

        [TestCase("Game.Title.2016.1080p.KORSUB.WEBRip.x264.AAC2.0-GAMARR", "KORSUB")]
        [TestCase("Game.Title.2016.1080p.KORSUBS.WEBRip.x264.AAC2.0-GAMARR", "KORSUBS")]
        [TestCase("Game Title 2017 HC 720p HDRiP DD5 1 x264-LEGi0N", "Generic Hardcoded Subs")]
        [TestCase("Game.Title.2017.720p.SUBBED.HDRip.V2.XViD-26k.avi", "Generic Hardcoded Subs")]
        [TestCase("Game.Title.2000.1080p.BlueRay.x264.DTS.RoSubbed-playHD", null)]
        [TestCase("Game Title! 2018 [Web][MKV][h264][480p][AAC 2.0][Softsubs]", null)]
        [TestCase("Game Title! 2019 [HorribleSubs][Web][MKV][h264][848x480][AAC 2.0][Softsubs(HorribleSubs)]", null)]
        [TestCase("Game Title! 2024 [Web][x265][1080p][EAC3][MultiSubs]", null)]
        public void should_parse_hardcoded_subs(string postTitle, string sub)
        {
            Parser.Parser.ParseGameTitle(postTitle).HardcodedSubs.Should().Be(sub);
        }

        [TestCase("Elden.Ring-CODEX", "Elden Ring")]
        [TestCase("DOOM.Eternal-CODEX", "DOOM Eternal")]
        [TestCase("Starfield-RUNE", "Starfield")]
        [TestCase("Half.Life.Alyx-VREX", "Half Life Alyx")]
        [TestCase("Baldurs.Gate.3-GOG", "Baldurs Gate 3")]
        [TestCase("Red.Dead.Redemption.2-EMPRESS", "Red Dead Redemption 2")]
        [TestCase("Grand.Theft.Auto.V-RELOADED", "Grand Theft Auto V")]
        [TestCase("The.Witcher.3.Wild.Hunt-GOG", "The Witcher 3 Wild Hunt")]
        [TestCase("God.of.War-CODEX", "God of War")]
        [TestCase("Cyberpunk.2077-CODEX", "Cyberpunk 2077")]
        [TestCase("God.of.War-FitGirl.Repack", "God of War")]
        [TestCase("Hogwarts.Legacy.Deluxe.Edition-DODI", "Hogwarts Legacy Deluxe Edition")]
        [TestCase("Elden.Ring-XATAB", "Elden Ring")]
        [TestCase("Starfield-Elamigos", "Starfield")]
        [TestCase("Diablo.IV-REPACK", "Diablo IV")]
        [TestCase("Hades.v1.38290-GOG", "Hades")]
        [TestCase("The.Last.of.Us.Part.I.v1.0.5-P2P", "The Last of Us Part I")]
        [TestCase("Grand.Theft.Auto.V.v1.68-RELOADED", "Grand Theft Auto V")]
        [TestCase("Forza.Horizon.5.Premium.Edition.v1.607.765.0-P2P", "Forza Horizon 5 Premium Edition")]
        [TestCase("Cyberpunk.2077.v2.1-CODEX", "Cyberpunk 2077")]
        [TestCase("Disco.Elysium.The.Final.Cut-GOG", "Disco Elysium The Final Cut")]
        [TestCase("Hollow.Knight-GOG", "Hollow Knight")]
        [TestCase("Hades-Steam-Rip", "Hades")]
        public void should_parse_game_release_without_year(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real-world FitGirl Repack releases (high seeder count)
        [TestCase("Elden Ring Deluxe Edition v 1.03.2 DLC 2022 PC Steam-Rip", "Elden Ring Deluxe Edition")]
        [TestCase("ELDEN RING Shadow of the Erdtree Deluxe Edition v1 12 v1 12 1 9 DLCs Bonuses Windows 7 Fix MULTi14 FitGirl Repack Selective Download from 47 4-GB", "ELDEN RING Shadow of the Erdtree Deluxe Edition")]
        [TestCase("God of War v1 0 1 Day 1 Patch Build 8008283 Bonus OST MULTi18 FitGirl Repack Selective Download from 26-GB", "God of War")]
        [TestCase("Red Dead Redemption 2 Build 1311.23 MULTi13 FitGirl Repack", "Red Dead Redemption 2")]
        [TestCase("Red Dead Redemption v1 0 40 57107 Bonus Content MULTi13 FitGirl Repack Selective Download from 5 4-GB", "Red Dead Redemption")]
        [TestCase("Starfield v1 7 23 0 2 DLCs Bonus Artbook OST MULTi9 FitGirl Repack Selective Download from 62 2-GB", "Starfield")]
        [TestCase("Cyberpunk 2077 Ultimate Edition v2 3 All DLCs Bonus Content", "Cyberpunk 2077 Ultimate Edition")]
        [TestCase("Hogwarts Legacy Digital Deluxe Edition Build 10461750 All DLCs Console DLCs Unlocker Bonus OSTs Trainer MULTi14 From 56 8 GB EMPRESS DODI-Repack", "Hogwarts Legacy Digital Deluxe Edition")]
        [TestCase("The Legend of Zelda Tears of the Kingdom v1.0.0 Switch Emulators MULTi10 FitGirl Repack", "The Legend of Zelda Tears of the Kingdom")]
        [TestCase("Hollow Knight Silksong v1.0.28324 MULTi10 FitGirl Repack", "Hollow Knight Silksong")]
        [TestCase("Sekiro Shadows Die Twice Game of the Year Edition v1 06 Bonus Content MULTi13 FitGirl Repack Selective Download from 7 2-GB", "Sekiro Shadows Die Twice Game of the Year Edition")]
        [TestCase("FINAL FANTASY VII REBIRTH Digital Deluxe Edition All DLCs Bonus Content Unlocker Fixes MULTi11 FitGirl Monkey Repack Selective Download from 131 6-GB", "FINAL FANTASY VII REBIRTH Digital Deluxe Edition")]
        [TestCase("Call of Duty Black Ops 6 v11 1 Campaign Only 4 Bonus OSTs MULTi14 FitGirl Repack Selective Download from 39 3-GB", "Call of Duty Black Ops 6")]
        [TestCase("Hades II Hades 2 v1 131346 Bonus OST MULTi15 FitGirl Repack Selective Download from 3 3-GB", "Hades II Hades 2")]
        public void should_parse_fitgirl_repack_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real-world DODI Repack releases
        [TestCase("ELDEN RING Deluxe Edition Shadow of the Erdtree Premium Bundle v1 12 1 12 1 All DLCs Bonus Content MULTi15 From 49 9 GB DODI-Repack", "ELDEN RING Deluxe Edition Shadow of the Erdtree Premium Bundle")]
        [TestCase("God of War Ragnarok Digital Deluxe Edition All DLCs Bonus Content 6 GB VRam Fix MULTi22 From 68 5 GB DODI-Repack", "God of War Ragnarok Digital Deluxe Edition")]
        [TestCase("Red Dead Redemption Undead Nightmare 2024 PC v1 0 40 57107 Bonus Content MULTi13 From 7 6 GB DODI-Repack", "Red Dead Redemption Undead Nightmare")]
        [TestCase("Hollow Knight Silksong v1.0.28324 MULTi10 DODI Repack", "Hollow Knight Silksong")]
        [TestCase("Final Fantasy VII Rebirth Digital Deluxe Edition v1 0 0 0 All DLCs Bonus Content MULTi11 From 137 GB Fast Install DODI-Repack", "Final Fantasy VII Rebirth Digital Deluxe Edition")]
        public void should_parse_dodi_repack_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real-world scene releases (CODEX, EMPRESS, RUNE, FLT, CPY)
        [TestCase("Hogwarts Legacy Deluxe Edition EMPRESS", "Hogwarts Legacy Deluxe Edition")]
        [TestCase("ELDEN RING Shadow of the Erdtree RUNE", "ELDEN RING Shadow of the Erdtree")]
        [TestCase("God of War-FLT", "God of War")]
        [TestCase("Starfield-RUNE", "Starfield")]
        [TestCase("Death Stranding-CPY", "Death Stranding")]
        [TestCase("Horizon Zero Dawn-CODEX", "Horizon Zero Dawn")]
        [TestCase("Sekiro Shadows Die Twice-CODEX", "Sekiro Shadows Die Twice")]
        [TestCase("DEATH STRANDING DIRECTORS CUT FLT", "DEATH STRANDING DIRECTORS CUT")]
        [TestCase("Hollow Knight Silksong-FLT", "Hollow Knight Silksong")]
        [TestCase("FINAL FANTASY VII REBIRTH FLT", "FINAL FANTASY VII REBIRTH")]
        [TestCase("Hades II RUNE", "Hades II")]
        [TestCase("God of War Ragnarok RUNE", "God of War Ragnarok")]
        public void should_parse_scene_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real-world Russian repacker releases (xatab, R.G. Механики)
        [TestCase("FIFA 17 Super Deluxe Edition 2016 PC RePack от xatab", "FIFA 17 Super Deluxe Edition")]
        [TestCase("Resident Evil 7 Biohazard v 1.03u5 DLC 2017 PC RePack от xatab Gold Edition", "Resident Evil 7 Biohazard")]
        [TestCase("Call of Duty WWII 2017 PC Rip от xatab", "Call of Duty WWII")]
        [TestCase("Hollow Knight v 1.2.2.1 2 DLC 2017 PC RePack от xatab", "Hollow Knight")]
        [TestCase("God of War v 1.0.8 1.0.447.8 2022 PC RePack от R.G. Механики", "God of War")]
        [TestCase("Elden Ring Deluxe Edition v 1.02.3 DLC 2022 PC RePack от R.G. Механики", "Elden Ring Deluxe Edition")]
        public void should_parse_russian_repacker_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real-world Switch emulator releases
        [TestCase("Super Mario Galaxy Super Mario Galaxy 2 v1.2.0 5 Switch Emulators MULTi11 FitGirl Repack", "Super Mario Galaxy Super Mario Galaxy 2")]
        [TestCase("Mario Luigi Brothership v1.0.0 Ryujinx Suyu Switch Emulators MULTi10 FitGirl Repack", "Mario Luigi Brothership")]
        [TestCase("The Legend of Zelda Echoes of Wisdom v1 0 1 Ryujinx Suyu Switch Emulators MULTi12 FitGirl-Repack", "The Legend of Zelda Echoes of Wisdom")]
        [TestCase("Nintendo Switch The Legend of Zelda Tears of the Kingdom NSP RUS Multi10", "The Legend of Zelda Tears of the Kingdom")]
        [TestCase("Nintendo Switch Hollow Knight NSZ RUS Multi9", "Hollow Knight")]
        [TestCase("Red Dead Redemption v1 0 1 Undead Nightmare DLC Bonus Content Switch Emulators MULT10 FitGirl Repack Selective Download from 7 6-GB", "Red Dead Redemption")]
        public void should_parse_switch_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real-world GOG releases
        [TestCase("DL Hollow Knight L RUS ENG 8 2017 Arcade 1.5.78.11833a 2 DLC GOG", "Hollow Knight")]
        [TestCase("DL The Witcher 3 Wild Hunt Complete Edition L RUS ENG 14 RUS ENG 6 2015 RPG 4.04a redkit update 2 18 DLC GOG", "The Witcher 3 Wild Hunt Complete Edition")]
        [TestCase("Cyberpunk 2077 GOG", "Cyberpunk 2077")]
        [TestCase("Baldurs Gate 3 live v4 1 1 5932596 76418 win gog", "Baldurs Gate 3")]
        public void should_parse_gog_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real-world update/patch releases
        [TestCase("ELDEN RING Shadow of the Erdtree Update v1 16 1 RUNE", "ELDEN RING Shadow of the Erdtree")]
        [TestCase("Baldurs Gate 3 Update v4 1 1 6848561 RUNE", "Baldurs Gate 3")]
        [TestCase("Cyberpunk 2077 Update v2 3", "Cyberpunk 2077")]
        [TestCase("Starfield Update v1 7 36 0", "Starfield")]
        [TestCase("God of War Ragnarok Update v1 2", "God of War Ragnarok")]
        [TestCase("Sekiro Shadows Die Twice Update v1 04-CODEX", "Sekiro Shadows Die Twice")]
        [TestCase("Baldurs Gate 3 Language Pack v4 1 1 6072089 RUNE", "Baldurs Gate 3")]
        public void should_parse_update_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Games with numbers in title
        [TestCase("Baldurs Gate 3 v4.1.1.5009956-EMPRESS", "Baldurs Gate 3")]
        [TestCase("Red Dead Redemption 2 Ultimate Edition v1491.50-FitGirl", "Red Dead Redemption 2 Ultimate Edition")]
        [TestCase("FIFA 23 v 1.0.82.43747 2022 PC RePack-FitGirl", "FIFA 23")]
        [TestCase("Resident Evil 2 Biohazard RE 2 Deluxe Edition v 20220613 DLCs 2019 PC RePack-FitGirl", "Resident Evil 2 Biohazard RE 2 Deluxe Edition")]
        [TestCase("FINAL FANTASY I VI Bundle Pixel Remaster Bonus DLCs Font Fixes MULTi12 FitGirl Repack Selective Download from 1 3-GB", "FINAL FANTASY I VI Bundle Pixel Remaster")]
        [TestCase("GTA 5 Grand Theft Auto V v 1.0.3725.0 1.72 Bonus 2015 PC RePack-FitGirl", "GTA 5 Grand Theft Auto V")]
        [TestCase("Call of Duty Black Ops 6 Campaign r4v3n Vault Edition v111", "Call of Duty Black Ops 6 Campaign")]
        public void should_parse_games_with_numbers_in_title(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Portable and P2P releases
        [TestCase("DL Elden Ring P RUS ENG 12 ENG 2022 RPG 1.16.0 6 DLC Portable", "Elden Ring")]
        [TestCase("ELDEN RING NIGHTREIGN RUNE PORTABLE", "ELDEN RING NIGHTREIGN")]
        [TestCase("DL Starfield P ENG 8 ENG 2023 RPG 1.15.222.0 3 DLC Portable", "Starfield")]
        [TestCase("Hogwarts Legacy Digital Deluxe Edition Build 10461750 All DLCs MULTi14 Portable", "Hogwarts Legacy Digital Deluxe Edition")]
        public void should_parse_portable_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // ElAmigos releases
        [TestCase("Hogwarts Legacy Deluxe Edition MULTi11 -ElAmigos", "Hogwarts Legacy Deluxe Edition")]
        [TestCase("Cyberpunk 2077 MULTi19-ElAmigos", "Cyberpunk 2077")]
        [TestCase("God of War 2022 MULTi19-ElAmigos", "God of War")]
        public void should_parse_elamigos_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x/scene releases - RUNE group
        // Note: Single-letter abbreviations after periods are treated as acronyms (e.g., "Z." preserved)
        [TestCase("Ravenswatch.Hourglass.of.Dreams.Update.v1.04.01.incl.DLC-RUNE", "Ravenswatch Hourglass of Dreams")]
        [TestCase("Winter.Survival.Update.v51123-RUNE", "Winter Survival")]
        [TestCase("Trailmakers.Frozen.Tracks.Update.v222.506.298.0-RUNE", "Trailmakers Frozen Tracks")]
        [TestCase("The.Lord.of.the.Rings.Return.to.Moria.Durins.Folk.Update.v1.6.6.218163-RUNE", "The Lord of the Rings Return to Moria Durins Folk")]
        [TestCase("Gold.Mining.Simulator.Gems.and.Glory.Update.v1.10.1.8-RUNE", "Gold Mining Simulator Gems and Glory")]
        [TestCase("Firefighting.Simulator.Ignite.Update.v1.0022-RUNE", "Firefighting Simulator Ignite")]
        [TestCase("CityDriver.Update.v55184-RUNE", "CityDriver")]
        [TestCase("Anima.Gate.of.Memories.I.and.II.Remaster.Update.v20251121-RUNE", "Anima Gate of Memories I. and II Remaster")]
        [TestCase("Achilles.Survivor.Update.v1.3-RUNE", "Achilles Survivor")]
        [TestCase("Kotama.and.Academy.Citadel-RUNE", "Kotama and Academy Citadel")]
        [TestCase("Dragon.Ball.Z.Kakarot.DAIMA.Adventure.Through.The.Demon.Realm.Part.2-RUNE", "Dragon Ball Z. Kakarot DAIMA Adventure Through The Demon Realm Part 2")]
        [TestCase("Voidtrain.Update.v1.04-RUNE", "Voidtrain")]
        [TestCase("Sacred.2.Remaster.Update.3.0-RUNE", "Sacred 2 Remaster")]
        [TestCase("Police.Simulator.Patrol.Officers.Contraband.Update.v22.0.4-RUNE", "Police Simulator Patrol Officers Contraband")]
        [TestCase("NODE.The.Last.Favor.of.the.Antarii.Update.v1.0.12-RUNE", "NODE The Last Favor of the Antarii")]
        [TestCase("Kingdom.Come.Deliverance.II.Mysteria.Ecclesiae.MULTi15.Update.v1.5.2-RUNE", "Kingdom Come Deliverance II Mysteria Ecclesiae MULTi15")]
        [TestCase("Kingdom.Come.Deliverance.II.Mysteria.Ecclesiae.Update.v1.5.2-RUNE", "Kingdom Come Deliverance II Mysteria Ecclesiae")]
        [TestCase("JDM.Japanese.Drift.Master.v1.2.157.1-RUNE", "JDM Japanese Drift Master")]
        [TestCase("Funko.Fusion.Update.v3.4.1.180665.incl.DLC-RUNE", "Funko Fusion")]
        [TestCase("Forever.Skies.Echoes-RUNE", "Forever Skies Echoes")]
        [TestCase("Quarantine.Zone.The.Last.Check-RUNE", "Quarantine Zone The Last Check")]
        [TestCase("WARNO.SOUTHAG.Update.v178307.incl.DLC-RUNE", "WARNO SOUTHAG")]
        [TestCase("Pathologic.3-RUNE", "Pathologic 3")]
        [TestCase("Terra.Invicta-RUNE", "Terra Invicta")]
        [TestCase("Wartales.The.Curse.of.Rigel.Update.v1.0.45242-RUNE", "Wartales The Curse of Rigel")]
        [TestCase("Tormented.Souls.2.Update.v1.3.5-RUNE", "Tormented Souls 2")]
        [TestCase("Shadow.Labyrinth.Update.v1.1.0-RUNE", "Shadow Labyrinth")]
        [TestCase("Senuas.Saga.Hellblade.II.Enhanced.Update.v20251218-RUNE", "Senuas Saga Hellblade II Enhanced")]
        [TestCase("Reentry.A.Space.Flight.Simulator.Update.v1.0.21-RUNE", "Reentry A Space Flight Simulator")]
        [TestCase("Knights.of.the.Crusades.Update.v1.11-RUNE", "Knights of the Crusades")]
        [TestCase("Dark.Moon.v1.02-RUNE", "Dark Moon")]
        [TestCase("Chip.n.Clawz.vs.The.Brainioids.v1.0.24500-RUNE", "Chip n. Clawz vs The Brainioids")]
        public void should_parse_rune_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x/scene releases - TENOKE group
        // Note: Single-letter abbreviations after periods are treated as acronyms (e.g., "I." preserved)
        [TestCase("Yao-Guai.Hunter.Update.v1.2.8-TENOKE", "Yao-Guai Hunter")]
        [TestCase("Sultans.Game.Update.v1.0.17991-TENOKE", "Sultans Game")]
        [TestCase("Photo.Studio.Simulator.Update.v20260115-TENOKE", "Photo Studio Simulator")]
        [TestCase("Out.Of.Hands.Update.v1.0.0.202-TENOKE", "Out Of Hands")]
        [TestCase("DAVE.THE.DIVER.Update.v1.0.5.1781-TENOKE", "DAVE THE DIVER")]
        [TestCase("SOS.OPS.TRIALS.Update.v20260115-TENOKE", "SOS OPS TRIALS")]
        [TestCase("Pale.Coins.Update.v1.1.0-TENOKE", "Pale Coins")]
        [TestCase("Fabledom.Update.v1.083b-TENOKE", "Fabledom")]
        [TestCase("Cash.Cleaner.Simulator.Update.v1.4.3.808-TENOKE", "Cash Cleaner Simulator")]
        [TestCase("1998.The.Toll.Keeper.Story.Update.v1.0.8f3-TENOKE", "1998 The Toll Keeper Story")]
        [TestCase("Streetdog.BMX-TENOKE", "Streetdog BMX")]
        [TestCase("Napoleons.Eagles.Game.of.the.Napoleonic.Wars.La.Marseillaise-TENOKE", "Napoleons Eagles Game of the Napoleonic Wars La Marseillaise")]
        [TestCase("hololive.GoroGoro.Mountain.Secret.Society.holoX.Collaboration-TENOKE", "hololive GoroGoro Mountain Secret Society holoX Collaboration")]
        [TestCase("Hatsune.Miku.Logic.Paint.S.Plus.SNOW.MIKU.Sky.Town-TENOKE", "Hatsune Miku Logic Paint S. Plus SNOW MIKU Sky Town")]
        [TestCase("Bloodface.Scars.of.the.Past-TENOKE", "Bloodface Scars of the Past")]
        [TestCase("SIDE.EFFECTS.Update.v1.09-TENOKE", "SIDE EFFECTS")]
        [TestCase("Sani.Yangs.Laboratory.Update.v20251205-TENOKE", "Sani Yangs Laboratory")]
        [TestCase("OFF.Update.v1.1-TENOKE", "OFF")]
        [TestCase("Easy.Red.2.Update.v2.0.2-TENOKE", "Easy Red 2")]
        [TestCase("Do.No.Harm.Update.v1.2.4-TENOKE", "Do No Harm")]
        [TestCase("Xenopurge.Update.v20251219-TENOKE", "Xenopurge")]
        [TestCase("Wretch.Divine.Ascent.Update.v1.1.1-TENOKE", "Wretch Divine Ascent")]
        [TestCase("WAR.RATS.The.Rat.em.Up.Update.v1.0.7-TENOKE", "WAR RATS The Rat em Up")]
        [TestCase("Let.Them.Trade.Update.v1.1.4-TENOKE", "Let Them Trade")]
        [TestCase("Discounty.Update.v1.1.3a-TENOKE", "Discounty")]
        [TestCase("Log.in-TENOKE", "Log in")]
        [TestCase("Dream.of.Corpse.Lady-TENOKE", "Dream of Corpse Lady")]
        [TestCase("Confidential.Killings.A.Detective.Game-TENOKE", "Confidential Killings A Detective Game")]
        [TestCase("Big.Hops-TENOKE", "Big Hops")]
        [TestCase("AiliA-TENOKE", "AiliA")]
        [TestCase("Touhou.Danmaku.Kagura.Phantasia.Lost.Update.v1.12.0-TENOKE", "Touhou Danmaku Kagura Phantasia Lost")]
        [TestCase("Intravenous.2.Update.v1.4.7-TENOKE", "Intravenous 2")]
        [TestCase("Demonschool.Update.v20260108-TENOKE", "Demonschool")]
        [TestCase("Deep.Sleep.Labyrinth.of.the.Forsaken.Update.v1.1.3.1-TENOKE", "Deep Sleep Labyrinth of the Forsaken")]
        [TestCase("Coral.Island.Update.v1.2b-1245-TENOKE", "Coral Island")]
        [TestCase("Northgard.Definitive.Edition-TENOKE", "Northgard Definitive Edition")]
        [TestCase("Monster.Prom.4.Monster.Con.v1.55-TENOKE", "Monster Prom 4 Monster Con")]
        [TestCase("Just.King.v1.4.0g-TENOKE", "Just King")]
        [TestCase("Cozy.Caravan-TENOKE", "Cozy Caravan")]
        [TestCase("Basketboys.Adventure-TENOKE", "Basketboys Adventure")]
        [TestCase("Ancient.Farm-TENOKE", "Ancient Farm")]
        [TestCase("Touhou.Makuka.Sai.Fantastic.Danmaku.Festival.Part.III.Update.v20251224-TENOKE", "Touhou Makuka Sai Fantastic Danmaku Festival Part III")]
        [TestCase("Sea.Fantasy.Update.v2.7.0-TENOKE", "Sea Fantasy")]
        [TestCase("PATAPON.1.Plus.2.REPLAY.Update.v1.0.9-TENOKE", "PATAPON 1 Plus 2 REPLAY")]
        [TestCase("Looking.Up.I.See.Only.A.Ceiling.Update.v2.0.0.19-TENOKE", "Looking Up I. See Only A Ceiling")]
        [TestCase("Artis.Impact.Update.v1.14-TENOKE", "Artis Impact")]
        [TestCase("Kingdoms.And.Slaves-TENOKE", "Kingdoms And Slaves")]
        [TestCase("House.Builder.Tiny.Houses-TENOKE", "House Builder Tiny Houses")]
        [TestCase("Northgard.Gardariki.Clan.of.the.Hippogriff.Update.v4.0.7.43120-TENOKE", "Northgard Gardariki Clan of the Hippogriff")]
        [TestCase("Granvir.Update.v2.12.2-TENOKE", "Granvir")]
        [TestCase("Foolish.Mortals.Update.v1.3-TENOKE", "Foolish Mortals")]
        [TestCase("Deconstruction.Simulator.Update.v1.4.0-TENOKE", "Deconstruction Simulator")]
        [TestCase("Ancient.Cities.Update.v1.8.26-TENOKE", "Ancient Cities")]
        [TestCase("Sky.traveler-TENOKE", "Sky traveler")]
        [TestCase("Feed.the.Reactor-TENOKE", "Feed the Reactor")]
        [TestCase("DuneCrawl-TENOKE", "DuneCrawl")]
        [TestCase("Viewfinder.Update.v20251126-TENOKE", "Viewfinder")]
        [TestCase("The.Salesman.Update.v1.1.0-TENOKE", "The Salesman")]
        [TestCase("Journey.of.Realm.Dawn.Dew.Update.v20251218-TENOKE", "Journey of Realm Dawn Dew")]
        [TestCase("Cobalt.Core.Update.v1.2.6-TENOKE", "Cobalt Core")]
        [TestCase("Battle.Suit.Aces.Update.v1.0.48-TENOKE", "Battle Suit Aces")]
        [TestCase("Witchy.Business.Update.v1.1.2-TENOKE", "Witchy Business")]
        [TestCase("Winter.Burrow.Update.v1.1.0-TENOKE", "Winter Burrow")]
        [TestCase("Million.Depth.Update.v1.08.03-TENOKE", "Million Depth")]
        [TestCase("Junji.Ito.Maniac.An.Infinite.Gaol.Update.v1.1.2-TENOKE", "Junji Ito Maniac An Infinite Gaol")]
        [TestCase("Dice.Gambit.Update.v20251126-TENOKE", "Dice Gambit")]
        public void should_parse_tenoke_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x/scene releases - I_KnoW group
        [TestCase("Sandwalkers.v2.2.3.MacOS-I_KnoW", "Sandwalkers")]
        [TestCase("Sandwalkers.v2.2.3.Linux-I_KnoW", "Sandwalkers")]
        [TestCase("Mato.Anomalies.Digital.Deluxe.Edition.v20251010-I_KnoW", "Mato Anomalies Digital Deluxe Edition")]
        [TestCase("Sandwalkers.v2.2.3-I_KnoW", "Sandwalkers")]
        [TestCase("Lust.Theory.Season.3.MacOS-I_KnoW", "Lust Theory Season 3")]
        [TestCase("GIGASWORD.v1.1.1-I_KnoW", "GIGASWORD")]
        public void should_parse_i_know_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x/scene releases - RAZOR/Razor1911 group
        [TestCase("Love_and_Knights_Solitaire-RAZOR", "Love and Knights Solitaire")]
        [TestCase("Jewel_Match_Solitaire_Fantasy_Collectors_Edition-RAZOR", "Jewel Match Solitaire Fantasy Collectors Edition")]
        [TestCase("Dark_Town_Secrets_2_The_Last_Burger_Collectors_Edition-RAZOR", "Dark Town Secrets 2 The Last Burger Collectors Edition")]
        [TestCase("Clear_It_M3-RAZOR", "Clear It M3")]
        [TestCase("Amandas_Magic_Book_12_The_Islands_of_the_Sleeping_Titans-RAZOR", "Amandas Magic Book 12 The Islands of the Sleeping Titans")]
        [TestCase("Adventure_Mosaics_Venice_Carnival-RAZOR", "Adventure Mosaics Venice Carnival")]
        [TestCase("Unforeseen_Incidents_v1.9_MacOS-Razor1911", "Unforeseen Incidents")]
        [TestCase("Unforeseen_Incidents_Update_v1.9-RazorDOX", "Unforeseen Incidents")]
        [TestCase("Forgotten_23_MacOS-Razor1911", "Forgotten 23 MacOS")]
        [TestCase("Cosplayers_Quest-Razor1911", "Cosplayers Quest")]
        [TestCase("Hidden_Weekend_The_American_Getaway-RAZOR", "Hidden Weekend The American Getaway")]
        [TestCase("Argonauts_Agency_11_Bronze_Plague-RAZOR", "Argonauts Agency 11 Bronze Plague")]
        public void should_parse_razor_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x/scene releases - DELiGHT group (GOG releases)
        [TestCase("Wizordum.v1.2.01.GOG-DELiGHT", "Wizordum")]
        [TestCase("Wizardry.Proving.Grounds.of.the.Mad.Overlord.v1.1.1.GOG-DELiGHT", "Wizardry Proving Grounds of the Mad Overlord")]
        [TestCase("Shrines.Legacy.v1.1.2.GOG-DELiGHT", "Shrines Legacy")]
        [TestCase("Legends.of.Amberland.The.Song.of.Trees.v1.24.GOG-DELiGHT", "Legends of Amberland The Song of Trees")]
        [TestCase("Legends.of.Amberland.The.forgotten.Crown.v1.31.GOG-DELiGHT", "Legends of Amberland The forgotten Crown")]
        [TestCase("Breath.of.Fire.IV.v1.0.hotfix5.GOG-DELiGHT", "Breath of Fire IV")]
        [TestCase("Anodyne.v2.01.GOG-DELiGHT", "Anodyne")]
        [TestCase("Anodyne.2.Return.to.Dust.v1.5.1.GOG-DELiGHT", "Anodyne 2 Return to Dust")]
        [TestCase("Travel.to.Thailand.German-DELiGHT", "Travel to Thailand")]
        [TestCase("Gaslamp.Cases.9.The.Ghost.of.Automaton.City.German-DELiGHT", "Gaslamp Cases 9 The Ghost of Automaton City")]
        [TestCase("Gaslamp.Cases.8.Quest.for.the.Relic.German-DELiGHT", "Gaslamp Cases 8 Quest for the Relic")]
        [TestCase("Gaslamp.Cases.7.The.Fait.of.Rasputin.German-DELiGHT", "Gaslamp Cases 7 The Fait of Rasputin")]
        [TestCase("Gaslamp.Cases.6.Haunted.Waters.German-DELiGHT", "Gaslamp Cases 6 Haunted Waters")]
        [TestCase("Gaslamp.Cases.5.The.Dreadful.City.German-DELiGHT", "Gaslamp Cases 5 The Dreadful City")]
        [TestCase("Gaslamp.Cases.4.The.Arcane.Village.German-DELiGHT", "Gaslamp Cases 4 The Arcane Village")]
        [TestCase("Gaslamp.Cases.3.Ancient.Secrets.German-DELiGHT", "Gaslamp Cases 3 Ancient Secrets")]
        [TestCase("Gaslamp.Cases.2.The.Haunted.Village.German-DELiGHT", "Gaslamp Cases 2 The Haunted Village")]
        [TestCase("Dungeon.Antiqua.2.v20260115-DELiGHT", "Dungeon Antiqua 2")]
        [TestCase("Dungeon.Antiqua.v20251123-DELiGHT", "Dungeon Antiqua")]
        [TestCase("Fear.and.Hunger.2.Termina.v1.9.1-DELiGHT", "Fear and Hunger 2 Termina")]
        [TestCase("Philna.Fantasy.v1.0.2-DELiGHT", "Philna Fantasy")]
        public void should_parse_delight_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x/scene releases - Other groups (TiNYiSO, SKIDROW, DINOByTES, bADkARMA)
        [TestCase("Journey.Through.The.Undead-TiNYiSO", "Journey Through The Undead")]
        [TestCase("Meal.Mystery.Escape.Room-TiNYiSO", "Meal Mystery Escape Room")]
        [TestCase("DeadCore.Redux-SKIDROW", "DeadCore Redux")]
        [TestCase("Farm.Manager.World.Africa-SKIDROW", "Farm Manager World Africa")]
        [TestCase("The.Settlers.3.Ultimate.Collection.v1.60.Win11.GOG-DINOByTES", "The Settlers 3 Ultimate Collection v1 60 Win11 GOG")]
        [TestCase("Esophaguys_The_Neckening-DINOByTES", "Esophaguys The Neckening")]
        [TestCase("Control_Ultimate_Edition_v1.32-DINOByTES", "Control Ultimate Edition")]
        [TestCase("Inside_the_Flesh_Engine-bADkARMA", "Inside the Flesh Engine")]
        [TestCase("Kinsfolk_Linux-bADkARMA", "Kinsfolk Linux")]
        [TestCase("Kinsfolk-bADkARMA", "Kinsfolk")]
        [TestCase("Damn_Dolls-bADkARMA", "Damn Dolls")]
        [TestCase("Stranger_Things_Game-bADkARMA", "Stranger Things Game")]
        [TestCase("MARVEL.Cosmic.Invasion.Plus.3.Trainer-PLAYMAGiC", "MARVEL Cosmic Invasion Plus 3 Trainer")]
        [TestCase("Ghost.of.Tsushima.Directors.Cut.v20251216.REPACK-KaOs", "Ghost of Tsushima Directors Cut")]
        public void should_parse_other_scene_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x top 100 releases - FitGirl repacks with parenthesized version
        [TestCase("Grand Theft Auto V (v1.0.3725.0/1.72 + Bonus Content, MULTi13) [FitGirl Repack, Selective Download - from 53.7 GB]", "Grand Theft Auto V")]
        [TestCase("Quarantine Zone: The Last Check (v1.0.1176 + 5 DLCs, MULTi14) [FitGirl Repack, Selective Download - from 5.2 GB]", "Quarantine Zone: The Last Check")]
        [TestCase("Dead Space (2023): Digital Deluxe Edition (Build 10602756 + DLC, MULTi13) [FitGirl Repack]", "Dead Space (2023): Digital Deluxe Edition")]
        [TestCase("Persona 5 Royal (v1.3B + Bonus OST, MULTi10) [FitGirl Repack, Selective Download - from 18.8 GB]", "Persona 5 Royal")]
        [TestCase("Call of Duty: Black Ops - Cold War (v1.34.1.15931218 + All DLCs and Modes + Bonus OSTs, MULTi13) [FitGirl Repack]", "Call of Duty: Black Ops - Cold War")]
        [TestCase("Grand Theft Auto V Enhanced (Build 1013.20/Online 1.72, MULTi13) [FitGirl Repack]", "Grand Theft Auto V Enhanced")]
        [TestCase("FIFA 22 (v1.0.77.45722, MULTi21) [FitGirl Monkey Repack, Selective Download - from 53.5 GB]", "FIFA 22")]
        [TestCase("DIRT 5: Year One Edition (v1.2770.47.0/UWP + All DLCs + Multiplayer, MULTi9) [FitGirl Repack]", "DIRT 5: Year One Edition")]
        [TestCase("Warhammer 40,000: Space Marine 2: 1-Year Anniversary Edition (v11.2.0.1 + 19 DLCs, MULTi16) [FitGirl Repack]", "Warhammer 40,000: Space Marine 2: 1-Year Anniversary Edition")]
        [TestCase("Gotham Knights: Deluxe Edition (v6.0.21.0 + 11 DLCs + Bonus Soundtrack, MULTi14) [FitGirl Repack]", "Gotham Knights: Deluxe Edition")]
        [TestCase("Lords of the Fallen: Deluxe Edition (v2.5.220 + 6 DLCs/Bonuses + Multiplayer, MULTi20) [FitGirl Repack]", "Lords of the Fallen: Deluxe Edition")]
        [TestCase("Sonic Colors: Ultimate - Digital Deluxe Edition (+ 6 DLCs, MULTi10) [FitGirl Repack]", "Sonic Colors: Ultimate - Digital Deluxe Edition")]
        [TestCase("Total War: Three Kingdoms - Collection (v1.7.8 Build 187 + 10 DLCs, MULTi13) [FitGirl Repack]", "Total War: Three Kingdoms - Collection")]
        [TestCase("Terra Invicta (v1.0.25, MULTi14) [FitGirl Repack]", "Terra Invicta")]
        [TestCase("SCUM: Complete Bundle (v1.2.0.1.103760 + 14 DLCs/Bonuses, MULTi13) [FitGirl Repack]", "SCUM: Complete Bundle")]
        [TestCase("The Spirit of the Samurai: Deluxe Edition (v1.0.15 + Bonus OST, MULTi10) [FitGirl Repack]", "The Spirit of the Samurai: Deluxe Edition")]
        [TestCase("THE HOUSE OF THE DEAD 2: Remake (v20251217 GOG, MULTi9) [FitGirl Repack]", "THE HOUSE OF THE DEAD 2: Remake")]
        [TestCase("Phoenix Point: Complete Edition (v1.30 + 6 DLCs + Bonus Content, MULTi8) [FitGirl Repack]", "Phoenix Point: Complete Edition")]
        [TestCase("ServiceIT: You can do IT (v1.1.1 + DLC, MULTi26) [FitGirl Repack]", "ServiceIT: You can do IT")]
        [TestCase("Halls of Torment: Tormented Supporter Bundle (v2025-12-04 + 3 DLCs/Bonuses, MULTi9) [FitGirl Repack]", "Halls of Torment: Tormented Supporter Bundle")]
        [TestCase("Pathologic 3 (v60905 + Supporter Pack DLC, ENG/RUS) [FitGirl Repack]", "Pathologic 3")]
        [TestCase("Burkina Faso: Radical Insurgency (MULTi14) [FitGirl Repack]", "Burkina Faso: Radical Insurgency")]
        [TestCase("Northgard: Definitive Edition (v4.0.11.43178 + 17 DLCs/Bonuses, MULTi11) [FitGirl Repack]", "Northgard: Definitive Edition")]
        [TestCase("Warhammer 40,000: Gladius - Complete Edition (v1.17.0 + 20 DLCs/Bonuses, MULTi6) [FitGirl Repack]", "Warhammer 40,000: Gladius - Complete Edition")]
        [TestCase("Formula Legends (Build 21082031 + 5 DLCs, MULTi13) [FitGirl Repack]", "Formula Legends")]
        [TestCase("Tunguska: The Visitation - Final Cut (v1.95-1 + 7 DLCs, MULTi9) [FitGirl Repack]", "Tunguska: The Visitation - Final Cut")]
        [TestCase("JDM: Japanese Drift Master (v1.2.157.1 + 2 DLCs, MULTi15) [FitGirl Repack, Selective Download - from 10.0 GB]", "JDM: Japanese Drift Master")]
        [TestCase("Jotunnslayer: Hordes of Hel - Collector's Edition (v1.1.2.91003 + 6 DLCs/Bonuses, MULTi20) [FitGirl Repack]", "Jotunnslayer: Hordes of Hel - Collector's Edition")]
        [TestCase("Wreckreation (v1.2.0.147169, MULTi12) [FitGirl Repack]", "Wreckreation")]
        [TestCase("Swordhaven: Iron Conspiracy (v1.0.0 + 4 DLCs/Bonuses, MULTi14) [FitGirl Repack, Selective Download]", "Swordhaven: Iron Conspiracy")]
        [TestCase("Forever Skies: Deluxe Edition (v1.2.0 #43440 + 8 DLCs/Bonuses, MULTi12) [FitGirl Repack]", "Forever Skies: Deluxe Edition")]
        [TestCase("Rising Front (v1.0/Release, MULTi30) [FitGirl Repack]", "Rising Front")]
        [TestCase("Songs of Silence: Complete Edition (v1.6.0-d.9224 + 10 DLCs/Bonuses, MULTi14) [FitGirl Repack]", "Songs of Silence: Complete Edition")]
        [TestCase("Oddsparks: An Automation Adventure - Ultimate Edition (v1.0.S31386 + 6 DLCs, MULTi9) [FitGirl Repack]", "Oddsparks: An Automation Adventure - Ultimate Edition")]
        [TestCase("Vampiress: Eternal Duet (ENG/CHS) [FitGirl Repack]", "Vampiress: Eternal Duet")]
        [TestCase("City Transport Simulator: Bus + Tram Special Bundle (v1.4.0 + 13 DLCs, MULTi9) [FitGirl Repack]", "City Transport Simulator: Bus + Tram Special Bundle")]
        [TestCase("Pure Badminton (MULTi11) [FitGirl Repack]", "Pure Badminton")]
        [TestCase("Star Trucker: Deluxe Bundle (v1.0.72.2 + 5 DLCs/Bonuses, MULTi10) [FitGirl Repack]", "Star Trucker: Deluxe Bundle")]
        [TestCase("The Legend of Heroes: Trails beyond the Horizon - Complete Edition (v1.0.6 r21 + 4 DLCs, MULTi7) [FitGirl Repack]", "The Legend of Heroes: Trails beyond the Horizon - Complete Edition")]
        [TestCase("Prison Simulator (v1.4.3.29 + 4 DLCs/Bonuses, MULTi18) [FitGirl Repack]", "Prison Simulator")]
        [TestCase("Onirism (Build 21264271, MULTi8) [FitGirl Repack]", "Onirism")]
        [TestCase("FRONT MISSION 2: Remake (v1.1.0, MULTi9) [FitGirl Repack]", "FRONT MISSION 2: Remake")]
        [TestCase("Project Hunt: Hunter's Collection (Build 21226542 + 3 DLCs, MULTi16) [FitGirl Repack]", "Project Hunt: Hunter's Collection")]
        [TestCase("Barotrauma: Supporter Bundle (v1.11.4.1 + 2 DLCs/Bonuses, MULTi13) [FitGirl Repack]", "Barotrauma: Supporter Bundle")]
        [TestCase("Spiritfall (v1.6.28 + Bonus Soundtrack, MULTi13) [FitGirl Repack]", "Spiritfall")]
        [TestCase("Ogu and the Secret Forest: Deluxe Edition (v1.3a + 2 DLCs/Bonuses, MULTi12) [FitGirl Repack]", "Ogu and the Secret Forest: Deluxe Edition")]
        [TestCase("Tomba! 2: The Evil Swine Return Special Edition (MULTi6) [FitGirl Repack]", "Tomba! 2: The Evil Swine Return Special Edition")]
        [TestCase("House Builder: Pack and Punch Bundle (Build 07-01-2026 + 9 DLCs, MULTi35) [FitGirl Repack]", "House Builder: Pack and Punch Bundle")]
        [TestCase("Kamikaze Strike: FPV Drone (MULTi29) [FitGirl Repack]", "Kamikaze Strike: FPV Drone")]
        [TestCase("Movies Tycoon: Ultimate Edition (v2.3.1 + 2 DLCs, MULTi9) [FitGirl Repack]", "Movies Tycoon: Ultimate Edition")]
        [TestCase("Taxi Chaos 2 [FitGirl Repack]", "Taxi Chaos 2")]
        [TestCase("Call of Duty: Black Ops - Cold War (HD Textures Pack, MULTi13) [FitGirl Repack]", "Call of Duty: Black Ops - Cold War")]
        [TestCase("PBA Pro Bowling 2026 (MULTi6) [FitGirl Repack]", "PBA Pro Bowling 2026")]
        [TestCase("The Temple of Elemental Evil (Re-release) [FitGirl Repack]", "The Temple of Elemental Evil")]
        [TestCase("Mutant Football League 2 [FitGirl Repack]", "Mutant Football League 2")]
        [TestCase("Anno 1800: Definitive Annoversary Edition (v18.4.1412158 + All DLCs/Bonuses, MULTi12) [FitGirl Repack]", "Anno 1800: Definitive Annoversary Edition")]
        [TestCase("Halo: The Master Chief Collection (v1.3528.0.0, MULTi12) [FitGirl Repack, Selective Download - from 75.8 GB]", "Halo: The Master Chief Collection")]
        [TestCase("MIO: Memories in Orbit (v21606, MULTi15) [FitGirl Repack]", "MIO: Memories in Orbit")]
        [TestCase("DYNASTY WARRIORS: ORIGINS - Digital Deluxe Edition (v1.0.0.9 + 9 DLCs + Controller Fix, MULTi14) [FitGirl Repack]", "DYNASTY WARRIORS: ORIGINS - Digital Deluxe Edition")]
        [TestCase("Len's Island: Deluxe Edition (v1.1.43 + 3 DLCs/Bonuses, MULTi12) [FitGirl Repack]", "Len's Island: Deluxe Edition")]
        [TestCase("Red Dead Redemption 2: Bonus Content [FitGirl Repack, Selective Download - from 9.5 GB]", "Red Dead Redemption 2: Bonus Content")]
        [TestCase("Foundation: Supporter Edition (v1.11.0.11 + 2 DLCs/Bonuses, MULTi27) [FitGirl Repack]", "Foundation: Supporter Edition")]
        [TestCase("Farm Manager World (v1.1.20260115.534 + Africa DLC, MULTi12) [FitGirl Repack]", "Farm Manager World")]
        [TestCase("Craftlings (v1.0.2 + Bonus OST, MULTi14) [FitGirl Repack]", "Craftlings")]
        [TestCase("Dungeon Defenders: Ultimate Collection (v10.5.0 + 42 DLCs, MULTi5) [FitGirl Repack]", "Dungeon Defenders: Ultimate Collection")]
        [TestCase("Kotama and Academy Citadel (v1.00.01.00, MULTi5) [FitGirl Repack]", "Kotama and Academy Citadel")]
        [TestCase("Tomba! Special Edition (Build 19099803, MULTi7) [FitGirl Repack, Selective Download]", "Tomba! Special Edition")]
        [TestCase("Cubic Odyssey: Complete Edition (v1.2.0.1 + 6 DLCs, MULTi11) [FitGirl Repack]", "Cubic Odyssey: Complete Edition")]
        [TestCase("BrokenLore: UNFOLLOW (+ Deluxe Pack DLC*, MULTi15) [FitGirl Repack]", "BrokenLore: UNFOLLOW")]
        [TestCase("Journey Through the Undead [FitGirl Repack]", "Journey Through the Undead")]
        public void should_parse_1337x_fitgirl_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x top 100 releases - DODI repacks
        // Note: Title is extracted up to the first parenthesis (version/build info)
        [TestCase("Hytale [DODI Repack]", "Hytale")]
        [TestCase("Grand Theft Auto V Enhanced: Premium Edition (Build 1013.20 / Online 1.72 / v1.0.3725.0, MULTi13) [DODI Repack]", "Grand Theft Auto V Enhanced: Premium Edition")]
        [TestCase("Dead Space Remake (2023) Digital Deluxe Edition (Build 10602756 + All DLCs + Bonus, MULTi13) [DODI Repack]", "Dead Space Remake")]
        [TestCase("StarRupture: Supporter Edition (v0.1.1.112941-S + Supporter Pack + Bonus Content, MULTi9) [DODI Repack]", "StarRupture: Supporter Edition")]
        [TestCase("Lords of the Fallen: Deluxe Edition (2023) (v2.5.220 - Final Major Update + All DLCs, MULTi20) [DODI Repack]", "Lords of the Fallen: Deluxe Edition (2023)")]
        [TestCase("Palworld (v0.7.0.84578 - Home Sweet Home Update + Bonus Content + MULTi17) (From 17.8 GB) [DODI Repack]", "Palworld")]
        [TestCase("Grand Theft Auto V Legacy (Build 3725.0 / Online 1.72 / v1.0.3725.0 + All DLCs, MULTi13) [DODI Repack]", "Grand Theft Auto V Legacy")]
        [TestCase("Persona 5 Royal (v1.03B + Bonus Content + MULTi10) (From 26.6 GB) (Lossless) [DODI Repack]", "Persona 5 Royal")]
        [TestCase("Grand Theft Auto V Legacy (Build 3725.0 / Online 1.72 / v1.0.3725.0 + All DLCs, MULTi13) [DODI Repack]", "Grand Theft Auto V Legacy")]
        [TestCase("inZOI (v20260108.9692.W + Island Getaway DLC + MULTi13) [DODI Repack]", "inZOI")]
        [TestCase("Grand Theft Auto V Enhanced: Premium Edition (Build 1013.20 / Online 1.72 / v1.0.3725.0, MULTi13) [DODI Repack]", "Grand Theft Auto V Enhanced: Premium Edition")]
        [TestCase("Dead Space Remake (2023) Digital Deluxe Edition (Build 10602756 + All DLCs + Bonus, MULTi13) [DODI Repack]", "Dead Space Remake")]
        public void should_parse_1337x_dodi_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x top 100 releases - IGGGAMESCOM simple releases
        [TestCase("Hytale", "Hytale")]
        [TestCase("Manor Lords (v2025.12.21)", "Manor Lords")]
        [TestCase("My Winter Car", "My Winter Car")]
        [TestCase("StarRupture", "StarRupture")]
        [TestCase("Project Zomboid (v42.13.1)", "Project Zomboid")]
        [TestCase("Assetto Corsa Rally (v0.2)", "Assetto Corsa Rally")]
        [TestCase("Megabonk (v2025.12.30)", "Megabonk")]
        [TestCase("inZOI (v0.5.0 & All DLCs)", "inZOI")]
        [TestCase("Witchfire (v0.8.3)", "Witchfire")]
        [TestCase("My Winter Car (v2026.01.03)", "My Winter Car")]
        [TestCase("Bellwright (v2025.12.28)", "Bellwright")]
        [TestCase("BeamNG.drive (v2025.12.30)", "BeamNG drive")]
        [TestCase("Wicked Island (v2025.12.21)", "Wicked Island")]
        [TestCase("Gamblers Table", "Gamblers Table")]
        [TestCase("DuneCrawl v1.01", "DuneCrawl")]
        [TestCase("RuneScape: Dragonwilds (v0.10.0.2)", "RuneScape: Dragonwilds")]
        [TestCase("Alchemy Factory (v0.4.1.3838)", "Alchemy Factory")]
        [TestCase("Enshrouded (v2025.12.30)", "Enshrouded")]
        [TestCase("Palworld (v0.7)", "Palworld")]
        [TestCase("Clair Obscur: Expedition 33 Update v1.5.1", "Clair Obscur: Expedition 33")]
        [TestCase("Schedule I v0.4.2f7", "Schedule I")]
        [TestCase("Super Fantasy Kingdom (v2025.12.21)", "Super Fantasy Kingdom")]
        [TestCase("Dispatch (v1.0.16598)", "Dispatch")]
        [TestCase("Super Woden: Rally Edge", "Super Woden: Rally Edge")]
        [TestCase("Farthest Frontier (v1.0.6)", "Farthest Frontier")]
        [TestCase("Hytale (v2026.01.20)", "Hytale")]
        [TestCase("Timberborn (v2025.12.30)", "Timberborn")]
        [TestCase("Tavern Keeper v0.70.5", "Tavern Keeper")]
        [TestCase("Drifter Star: Evolution", "Drifter Star: Evolution")]
        [TestCase("My Winter Car (v2026.01.11)", "My Winter Car")]
        [TestCase("Anno 1800 End of an Era-voices38", "Anno 1800 End of an Era")]
        public void should_parse_1337x_igggamescom_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x top 100 releases - Scene group releases
        [TestCase("Quarantine Zone The Last Check-RUNE", "Quarantine Zone The Last Check")]
        [TestCase("Terra Invicta-RUNE", "Terra Invicta")]
        [TestCase("Feed the Reactor-TENOKE", "Feed the Reactor")]
        [TestCase("Dying Light The Beast Update v1 5 0-TENOKE", "Dying Light The Beast")]
        [TestCase("The Legend of Heroes Trails beyond the Horizon-RUNE", "The Legend of Heroes Trails beyond the Horizon")]
        public void should_parse_1337x_scene_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x top 100 releases - Portable releases
        // Note: Year is parsed as metadata, title doesn't include the year
        [TestCase("Hytale (2026) [PORTABLE]", "Hytale")]
        [TestCase("Dead Space Remake (2023) + DLC [Crack V1] [PORTABLE]", "Dead Space Remake")]
        [TestCase("StarRupture (2026) [PORTABLE]", "StarRupture")]
        public void should_parse_1337x_portable_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }

        // Real 1337x top 100 releases - Crack releases
        // Note: Year is parsed as metadata, title doesn't include the year
        [TestCase("Dead Space Remake (2023)[CRACK 1.1][AMD+Intel]", "Dead Space Remake")]
        [TestCase("Dead Space Remake (2023)[CRACK 2.0][Win10+11][AMD+Intel]", "Dead Space Remake")]
        [TestCase("Dead Space Remake CRACKFIX-voices38", "Dead Space Remake")]
        [TestCase("Dead Space Remake PROPER-voices38", "Dead Space Remake")]
        public void should_parse_1337x_crack_releases(string postTitle, string title)
        {
            var result = Parser.Parser.ParseGameTitle(postTitle);
            result.Should().NotBeNull($"Failed to parse: {postTitle}");
            result.PrimaryGameTitle.Should().Be(title);
        }
    }
}
