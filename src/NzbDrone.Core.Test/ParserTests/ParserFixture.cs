#pragma warning disable CS0618
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
        [TestCase("This Is A Game (1999) [IMDB #] <Genre, Genre, Genre> {ACTORS} !DIRECTOR +MORE_SILLY_STUFF_NO_ONE_NEEDS ?", "This Is A Game")]
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
        [TestCase("Vampire Survivors v1.10.103", "Vampire Survivors")]

        // Parenthesized version formats
        [TestCase("Hades II (v2025.06.18)", "Hades II")]
        [TestCase("Stardew Valley (v1.6.8)", "Stardew Valley")]

        // Russian tracker format with sequel indicator stripped
        [TestCase("[DL] Hades II (2) [P] [RUS + ENG + 13 / ENG] (2025, TPS) (1.133066) [Portable]", "Hades II")]
        [TestCase("[DL] The Witness [L] [RUS + ENG + 13 / ENG] (2016, Adventure) (21-12-2017) [GOG]", "The Witness")]
        [TestCase("[CD] Half-Life 2 [P] [RUS + ENG / ENG] (2004, FPS) (1.0.1.0) [Tycoon]", "Half-Life 2")]
        [TestCase("[DL] Elden Ring [P] [RUS + ENG + 12 / ENG] (2022, RPG) (1.16.0 + 6 DLC) [Portable]", "Elden Ring")]
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
        [TestCase("Resident Evil 7 Biohazard v 1.03u5 DLC 2017 PC RePack от xatab Gold Edition", "Resident Evil 7 Biohazard Gold Edition")]
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
    }
}
