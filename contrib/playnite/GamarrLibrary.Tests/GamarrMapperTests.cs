using System;
using System.Collections.Generic;
using System.Linq;
using GamarrLibrary.Mapping;
using NUnit.Framework;

namespace GamarrLibrary.Tests
{
    [TestFixture]
    public class GamarrMapperTests
    {
        private static GamarrGameDto DownloadedGame()
        {
            return new GamarrGameDto
            {
                Id = 42,
                Title = "Half-Life 2",
                SortTitle = "half life 2",
                Year = 2004,
                Overview = "Gordon returns.",
                Path = @"C:\Games\Half-Life 2",
                HasFile = true,
                SizeOnDisk = 6_500_000_000,
                SteamAppId = 220,
                IgdbId = 233,
                IgdbSlug = "half-life-2",
                RawgId = 13537,
                TitleSlug = "220",
                Developer = "Valve",
                Publisher = "Valve",
                Website = "https://half-life.com",
                Genres = new List<string> { "Shooter", "shooter", "Adventure" },
                Platforms = new List<GamarrPlatformDto>
                {
                    new GamarrPlatformDto { Name = "PC (Microsoft Windows)", Abbreviation = "PC" },
                    new GamarrPlatformDto { Name = "PC (Microsoft Windows)" },
                    new GamarrPlatformDto { Name = null }
                },
                DigitalRelease = new DateTime(2004, 11, 16)
            };
        }

        [Test]
        public void IsDownloaded_TrueWhenHasFile()
        {
            Assert.That(GamarrMapper.IsDownloaded(new GamarrGameDto { HasFile = true }), Is.True);
        }

        [Test]
        public void IsDownloaded_TrueWhenSizeOnDiskPositive()
        {
            Assert.That(GamarrMapper.IsDownloaded(new GamarrGameDto { HasFile = null, SizeOnDisk = 123 }), Is.True);
        }

        [Test]
        public void IsDownloaded_FalseWhenNoFileAndZeroSize()
        {
            Assert.That(GamarrMapper.IsDownloaded(new GamarrGameDto { HasFile = false, SizeOnDisk = 0 }), Is.False);
            Assert.That(GamarrMapper.IsDownloaded(new GamarrGameDto()), Is.False);
            Assert.That(GamarrMapper.IsDownloaded(null), Is.False);
        }

        [TestCase("http://localhost:6767", "http://localhost:6767")]
        [TestCase("http://localhost:6767/", "http://localhost:6767")]
        [TestCase("  http://gamarr.local//  ", "http://gamarr.local")]
        [TestCase(null, null)]
        [TestCase("   ", null)]
        public void NormalizeBaseUrl_Works(string input, string expected)
        {
            Assert.That(GamarrMapper.NormalizeBaseUrl(input), Is.EqualTo(expected));
        }

        [Test]
        public void BuildCoverUrl_UsesMediaCoverRouteAndEscapesApiKey()
        {
            var url = GamarrMapper.BuildCoverUrl("http://localhost:6767/", 42, "a b&c");

            Assert.That(url, Is.EqualTo("http://localhost:6767/api/v3/mediacover/42/poster.jpg?apikey=a%20b%26c"));
        }

        [Test]
        public void Map_DownloadedGame_MapsCoreFields()
        {
            var mapped = GamarrMapper.Map(DownloadedGame(), "http://localhost:6767/", "key123");

            Assert.That(mapped.GameId, Is.EqualTo("42"));
            Assert.That(mapped.Name, Is.EqualTo("Half-Life 2"));
            Assert.That(mapped.SortingName, Is.EqualTo("half life 2"));
            Assert.That(mapped.Description, Is.EqualTo("Gordon returns."));
            Assert.That(mapped.IsInstalled, Is.True);
            Assert.That(mapped.InstallDirectory, Is.EqualTo(@"C:\Games\Half-Life 2"));
            Assert.That(mapped.CoverUrl, Is.EqualTo("http://localhost:6767/api/v3/mediacover/42/poster.jpg?apikey=key123"));
            Assert.That(mapped.Developer, Is.EqualTo("Valve"));
            Assert.That(mapped.Publisher, Is.EqualTo("Valve"));
        }

        [Test]
        public void Map_DeduplicatesGenresAndPlatforms()
        {
            var mapped = GamarrMapper.Map(DownloadedGame(), "http://localhost:6767", "k");

            Assert.That(mapped.Genres, Is.EqualTo(new[] { "Shooter", "Adventure" }));
            Assert.That(mapped.Platforms, Is.EqualTo(new[] { "PC (Microsoft Windows)" }));
        }

        [Test]
        public void Map_BuildsExternalLinks()
        {
            var mapped = GamarrMapper.Map(DownloadedGame(), "http://localhost:6767", "k");
            var byName = mapped.Links.ToDictionary(l => l.Name, l => l.Url);

            Assert.That(byName["Gamarr"], Is.EqualTo("http://localhost:6767/game/220"));
            Assert.That(byName["Steam"], Is.EqualTo("https://store.steampowered.com/app/220/"));
            Assert.That(byName["IGDB"], Is.EqualTo("https://www.igdb.com/games/half-life-2"));
            Assert.That(byName["RAWG"], Is.EqualTo("https://rawg.io/games/13537"));
            Assert.That(byName["Website"], Is.EqualTo("https://half-life.com"));
        }

        [Test]
        public void Map_OmitsLinksWithoutIds()
        {
            var game = new GamarrGameDto { Id = 1, Title = "Obscure Indie", HasFile = true, Path = "/games/obscure" };

            var mapped = GamarrMapper.Map(game, "http://localhost:6767", "k");

            Assert.That(mapped.Links, Is.Empty);
        }

        [Test]
        public void Map_NotDownloadedGame_IsNotInstalledAndHasNoInstallDirectory()
        {
            var game = DownloadedGame();
            game.HasFile = false;
            game.SizeOnDisk = 0;

            var mapped = GamarrMapper.Map(game, "http://localhost:6767", "k");

            Assert.That(mapped.IsInstalled, Is.False);
            Assert.That(mapped.InstallDirectory, Is.Null);
        }

        [Test]
        public void Map_DownloadedButNoPath_IsNotInstalled()
        {
            var game = DownloadedGame();
            game.Path = "  ";

            var mapped = GamarrMapper.Map(game, "http://localhost:6767", "k");

            Assert.That(mapped.IsInstalled, Is.False);
            Assert.That(mapped.InstallDirectory, Is.Null);
        }

        [Test]
        public void Map_MissingTitle_FallsBackToPlaceholder()
        {
            var game = new GamarrGameDto { Id = 7, HasFile = true, Path = "/g" };

            var mapped = GamarrMapper.Map(game, "http://localhost:6767", "k");

            Assert.That(mapped.Name, Is.EqualTo("Gamarr game 7"));
        }

        [Test]
        public void Map_NullBaseUrl_ProducesNoCoverOrGamarrLink()
        {
            var mapped = GamarrMapper.Map(DownloadedGame(), null, "k");

            Assert.That(mapped.CoverUrl, Is.Null);
            Assert.That(mapped.Links.Any(l => l.Name == "Gamarr"), Is.False);
        }

        [Test]
        public void Map_NullGame_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GamarrMapper.Map(null, "http://x", "k"));
        }

        [Test]
        public void ResolveReleaseDate_PrefersReleaseDateThenDigitalThenPhysicalThenYear()
        {
            var full = new GamarrGameDto
            {
                ReleaseDate = new DateTime(2004, 11, 16),
                DigitalRelease = new DateTime(2005, 1, 1),
                PhysicalRelease = new DateTime(2006, 1, 1),
                Year = 2007
            };
            Assert.That(GamarrMapper.ResolveReleaseDate(full), Is.EqualTo(new DateTime(2004, 11, 16)));

            full.ReleaseDate = null;
            Assert.That(GamarrMapper.ResolveReleaseDate(full), Is.EqualTo(new DateTime(2005, 1, 1)));

            full.DigitalRelease = null;
            Assert.That(GamarrMapper.ResolveReleaseDate(full), Is.EqualTo(new DateTime(2006, 1, 1)));

            full.PhysicalRelease = null;
            Assert.That(GamarrMapper.ResolveReleaseDate(full), Is.EqualTo(new DateTime(2007, 1, 1)));

            full.Year = 0;
            Assert.That(GamarrMapper.ResolveReleaseDate(full), Is.Null);
        }
    }
}
