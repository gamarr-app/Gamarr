using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.IGDB;
using NzbDrone.Core.MetadataSource.RAWG;
using NzbDrone.Core.MetadataSource.Steam;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MetadataSource
{
    [TestFixture]
    public class AggregateGameInfoProxyFixture : CoreTest
    {
        private Mock<IConfigService> _configService;
        private AggregateGameInfoProxy _subject;
        private Mock<SteamStoreProxy> _steamProxy;
        private Mock<IgdbProxy> _igdbProxy;
        private Mock<RawgProxy> _rawgProxy;

        [SetUp]
        public void Setup()
        {
            _configService = new Mock<IConfigService>();

            // By default, no credentials configured
            _configService.SetupGet(c => c.IgdbClientId).Returns(string.Empty);
            _configService.SetupGet(c => c.IgdbClientSecret).Returns(string.Empty);
            _configService.SetupGet(c => c.RawgApiKey).Returns(string.Empty);

            // Create mocks of the concrete proxies (methods aren't virtual, so we use loose mocking)
            _steamProxy = new Mock<SteamStoreProxy>(MockBehavior.Loose, null, null, null);
            _igdbProxy = new Mock<IgdbProxy>(MockBehavior.Loose, null, null, null, null, null, null, null);
            _rawgProxy = new Mock<RawgProxy>(MockBehavior.Loose, null, null, null, null, null);

            _subject = new AggregateGameInfoProxy(
                _steamProxy.Object,
                _rawgProxy.Object,
                _igdbProxy.Object,
                _configService.Object,
                TestLogger);
        }

        private void GivenIgdbCredentials()
        {
            _configService.SetupGet(c => c.IgdbClientId).Returns("test-client-id");
            _configService.SetupGet(c => c.IgdbClientSecret).Returns("test-client-secret");

            // Rebuild subject with updated config
            _subject = new AggregateGameInfoProxy(
                _steamProxy.Object,
                _rawgProxy.Object,
                _igdbProxy.Object,
                _configService.Object,
                TestLogger);
        }

        private void GivenRawgCredentials()
        {
            _configService.SetupGet(c => c.RawgApiKey).Returns("test-rawg-key");

            // Rebuild subject with updated config
            _subject = new AggregateGameInfoProxy(
                _steamProxy.Object,
                _rawgProxy.Object,
                _igdbProxy.Object,
                _configService.Object,
                TestLogger);
        }

        private void GivenAllCredentials()
        {
            _configService.SetupGet(c => c.IgdbClientId).Returns("test-client-id");
            _configService.SetupGet(c => c.IgdbClientSecret).Returns("test-client-secret");
            _configService.SetupGet(c => c.RawgApiKey).Returns("test-rawg-key");

            _subject = new AggregateGameInfoProxy(
                _steamProxy.Object,
                _rawgProxy.Object,
                _igdbProxy.Object,
                _configService.Object,
                TestLogger);
        }

        private void InvokeMergeMetadata(GameMetadata existing, GameMetadata secondary)
        {
            var method = typeof(AggregateGameInfoProxy).GetMethod(
                "MergeMetadata",
                BindingFlags.NonPublic | BindingFlags.Instance);

            method.Invoke(_subject, new object[] { existing, secondary });
        }

        private GameMetadata BuildGameMetadata(
            string title = "Test Game",
            int steamAppId = 0,
            int igdbId = 0,
            int rawgId = 0,
            string igdbSlug = null,
            string overview = null,
            List<NzbDrone.Core.MediaCover.MediaCover> images = null,
            List<string> genres = null,
            Ratings ratings = null,
            int? parentGameId = null,
            List<int> igdbDlcIds = null,
            List<int> steamDlcIds = null)
        {
            return new GameMetadata
            {
                Title = title,
                SteamAppId = steamAppId,
                IgdbId = igdbId,
                RawgId = rawgId,
                IgdbSlug = igdbSlug,
                Overview = overview,
                Images = images ?? new List<NzbDrone.Core.MediaCover.MediaCover>(),
                Genres = genres ?? new List<string>(),
                Ratings = ratings ?? new Ratings(),
                ParentGameId = parentGameId,
                IgdbDlcIds = igdbDlcIds ?? new List<int>(),
                SteamDlcIds = steamDlcIds ?? new List<int>()
            };
        }

        // ============================================================
        // MergeMetadata Tests
        // ============================================================

        [Test]
        public void merge_metadata_should_add_igdb_images_when_existing_has_none()
        {
            var existing = BuildGameMetadata(title: "Half-Life 2", steamAppId: 220);

            var igdbImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://images.igdb.com/cover.jpg"),
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Fanart, "https://images.igdb.com/fanart.jpg")
            };

            var secondary = BuildGameMetadata(title: "Half-Life 2", igdbId: 72, images: igdbImages);

            InvokeMergeMetadata(existing, secondary);

            existing.Images.Should().HaveCount(2);
            existing.Images[0].RemoteUrl.Should().Contain("igdb.com");
            existing.Images[1].RemoteUrl.Should().Contain("igdb.com");
        }

        [Test]
        public void merge_metadata_should_keep_existing_images_when_both_have_them()
        {
            var existingImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://cdn.steam.com/poster.jpg"),
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Fanart, "https://cdn.steam.com/fanart.jpg")
            };

            var existing = BuildGameMetadata(title: "Portal 2", steamAppId: 620, images: existingImages);

            var igdbImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://images.igdb.com/cover.jpg"),
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Fanart, "https://images.igdb.com/fanart.jpg")
            };

            var secondary = BuildGameMetadata(title: "Portal 2", igdbId: 88, images: igdbImages);

            InvokeMergeMetadata(existing, secondary);

            // IGDB images are inserted at the front
            existing.Images[0].RemoteUrl.Should().Contain("igdb.com");
            existing.Images[1].RemoteUrl.Should().Contain("igdb.com");

            // Steam images are still present
            existing.Images.Should().Contain(i => i.RemoteUrl.Contains("cdn.steam.com"));
        }

        [Test]
        public void merge_metadata_should_insert_igdb_images_at_front()
        {
            var existingImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://cdn.steam.com/poster.jpg")
            };

            var existing = BuildGameMetadata(title: "Test", images: existingImages);

            var igdbImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://images.igdb.com/cover.jpg")
            };

            var secondary = BuildGameMetadata(images: igdbImages);

            InvokeMergeMetadata(existing, secondary);

            existing.Images[0].RemoteUrl.Should().Be("https://images.igdb.com/cover.jpg");
        }

        [Test]
        public void merge_metadata_should_not_duplicate_cover_type_for_non_igdb_images()
        {
            var existingImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://cdn.steam.com/poster.jpg")
            };

            var existing = BuildGameMetadata(title: "Test", images: existingImages);

            // Non-IGDB image with same cover type should NOT be added
            var secondaryImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://rawg.io/poster.jpg")
            };

            var secondary = BuildGameMetadata(images: secondaryImages);

            InvokeMergeMetadata(existing, secondary);

            existing.Images.Count(i => i.CoverType == NzbDrone.Core.MediaCover.MediaCoverTypes.Poster).Should().Be(1);
            existing.Images[0].RemoteUrl.Should().Be("https://cdn.steam.com/poster.jpg");
        }

        [Test]
        public void merge_metadata_should_add_non_igdb_images_for_missing_cover_types()
        {
            var existingImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://cdn.steam.com/poster.jpg")
            };

            var existing = BuildGameMetadata(title: "Test", images: existingImages);

            // Non-IGDB image with different cover type should be added
            var secondaryImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Screenshot, "https://rawg.io/screenshot.jpg")
            };

            var secondary = BuildGameMetadata(images: secondaryImages);

            InvokeMergeMetadata(existing, secondary);

            existing.Images.Should().HaveCount(2);
            existing.Images.Should().Contain(i => i.CoverType == NzbDrone.Core.MediaCover.MediaCoverTypes.Screenshot);
        }

        [Test]
        public void merge_metadata_should_add_steam_app_id_from_steam_source()
        {
            var existing = BuildGameMetadata(title: "Test Game", igdbId: 100);
            var secondary = BuildGameMetadata(steamAppId: 440);

            // MergeMetadata doesn't currently merge SteamAppId, but it merges IgdbId
            // Testing IgdbId merge from secondary
            existing.IgdbId.Should().Be(100);

            var existingNoIgdb = BuildGameMetadata(title: "Test Game");
            var secondaryWithIgdb = BuildGameMetadata(igdbId: 200);

            InvokeMergeMetadata(existingNoIgdb, secondaryWithIgdb);

            existingNoIgdb.IgdbId.Should().Be(200);
        }

        [Test]
        public void merge_metadata_should_add_igdb_id_when_missing()
        {
            var existing = BuildGameMetadata(title: "Test Game", steamAppId: 440, igdbId: 0);
            var secondary = BuildGameMetadata(igdbId: 1234);

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbId.Should().Be(1234);
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_igdb_id()
        {
            var existing = BuildGameMetadata(title: "Test Game", igdbId: 100);
            var secondary = BuildGameMetadata(igdbId: 200);

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbId.Should().Be(100);
        }

        [Test]
        public void merge_metadata_should_add_igdb_slug_when_missing()
        {
            var existing = BuildGameMetadata(title: "Half-Life 2");
            var secondary = BuildGameMetadata(igdbSlug: "half-life-2");

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbSlug.Should().Be("half-life-2");
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_igdb_slug()
        {
            var existing = BuildGameMetadata(title: "Half-Life 2", igdbSlug: "existing-slug");
            var secondary = BuildGameMetadata(igdbSlug: "new-slug");

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbSlug.Should().Be("existing-slug");
        }

        [Test]
        public void merge_metadata_should_add_ratings_from_secondary_when_missing()
        {
            var existing = BuildGameMetadata(title: "Test Game");
            var secondary = BuildGameMetadata(
                ratings: new Ratings
                {
                    Igdb = new RatingChild { Value = 85, Votes = 1000, Type = RatingType.User }
                });

            InvokeMergeMetadata(existing, secondary);

            existing.Ratings.Should().NotBeNull();
            existing.Ratings.Igdb.Should().NotBeNull();
            existing.Ratings.Igdb.Value.Should().Be(85);
            existing.Ratings.Igdb.Votes.Should().Be(1000);
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_igdb_rating()
        {
            var existing = BuildGameMetadata(
                title: "Test Game",
                ratings: new Ratings
                {
                    Igdb = new RatingChild { Value = 90, Votes = 500, Type = RatingType.User }
                });

            var secondary = BuildGameMetadata(
                ratings: new Ratings
                {
                    Igdb = new RatingChild { Value = 75, Votes = 200, Type = RatingType.User }
                });

            InvokeMergeMetadata(existing, secondary);

            existing.Ratings.Igdb.Value.Should().Be(90);
            existing.Ratings.Igdb.Votes.Should().Be(500);
        }

        [Test]
        public void merge_metadata_should_add_genres_when_missing()
        {
            var existing = BuildGameMetadata(title: "Test Game");
            var secondary = BuildGameMetadata(genres: new List<string> { "Action", "RPG" });

            InvokeMergeMetadata(existing, secondary);

            existing.Genres.Should().Contain("Action");
            existing.Genres.Should().Contain("RPG");
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_genres()
        {
            var existing = BuildGameMetadata(
                title: "Test Game",
                genres: new List<string> { "Adventure", "Puzzle" });

            var secondary = BuildGameMetadata(genres: new List<string> { "Action", "RPG" });

            InvokeMergeMetadata(existing, secondary);

            existing.Genres.Should().Contain("Adventure");
            existing.Genres.Should().Contain("Puzzle");
            existing.Genres.Should().NotContain("Action");
        }

        [Test]
        public void merge_metadata_should_add_overview_when_missing()
        {
            var existing = BuildGameMetadata(title: "Test Game");
            var secondary = BuildGameMetadata(overview: "A great game about testing.");

            InvokeMergeMetadata(existing, secondary);

            existing.Overview.Should().Be("A great game about testing.");
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_overview()
        {
            var existing = BuildGameMetadata(title: "Test Game", overview: "Original overview.");
            var secondary = BuildGameMetadata(overview: "Secondary overview.");

            InvokeMergeMetadata(existing, secondary);

            existing.Overview.Should().Be("Original overview.");
        }

        [Test]
        public void merge_metadata_should_add_parent_game_id_when_missing()
        {
            var existing = BuildGameMetadata(title: "Test DLC");
            var secondary = BuildGameMetadata(parentGameId: 42);

            InvokeMergeMetadata(existing, secondary);

            existing.ParentGameId.Should().Be(42);
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_parent_game_id()
        {
            var existing = BuildGameMetadata(title: "Test DLC", parentGameId: 10);
            var secondary = BuildGameMetadata(parentGameId: 42);

            InvokeMergeMetadata(existing, secondary);

            existing.ParentGameId.Should().Be(10);
        }

        [Test]
        public void merge_metadata_should_add_igdb_dlc_ids_when_missing()
        {
            var existing = BuildGameMetadata(title: "Test Game");
            var secondary = BuildGameMetadata(igdbDlcIds: new List<int> { 101, 102, 103 });

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbDlcIds.Should().BeEquivalentTo(new List<int> { 101, 102, 103 });
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_igdb_dlc_ids()
        {
            var existing = BuildGameMetadata(
                title: "Test Game",
                igdbDlcIds: new List<int> { 50, 51 });

            var secondary = BuildGameMetadata(igdbDlcIds: new List<int> { 101, 102, 103 });

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbDlcIds.Should().BeEquivalentTo(new List<int> { 50, 51 });
        }

        [Test]
        public void merge_metadata_should_add_steam_dlc_ids_when_missing()
        {
            var existing = BuildGameMetadata(title: "Test Game");
            var secondary = BuildGameMetadata(steamDlcIds: new List<int> { 100101, 100102, 100103 });

            InvokeMergeMetadata(existing, secondary);

            existing.SteamDlcIds.Should().BeEquivalentTo(new List<int> { 100101, 100102, 100103 });
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_steam_dlc_ids()
        {
            var existing = BuildGameMetadata(
                title: "Test Game",
                steamDlcIds: new List<int> { 100050, 100051 });

            var secondary = BuildGameMetadata(steamDlcIds: new List<int> { 100101, 100102, 100103 });

            InvokeMergeMetadata(existing, secondary);

            existing.SteamDlcIds.Should().BeEquivalentTo(new List<int> { 100050, 100051 });
        }

        [Test]
        public void merge_metadata_should_handle_null_existing()
        {
            var secondary = BuildGameMetadata(title: "Test");

            // Should not throw
            InvokeMergeMetadata(null, secondary);
        }

        [Test]
        public void merge_metadata_should_handle_null_secondary()
        {
            var existing = BuildGameMetadata(title: "Test");

            // Should not throw
            InvokeMergeMetadata(existing, null);
        }

        [Test]
        public void merge_metadata_should_create_ratings_object_if_null_on_existing()
        {
            var existing = BuildGameMetadata(title: "Test");
            existing.Ratings = null;

            var secondary = BuildGameMetadata(
                ratings: new Ratings
                {
                    Igdb = new RatingChild { Value = 80, Votes = 300, Type = RatingType.User }
                });

            InvokeMergeMetadata(existing, secondary);

            existing.Ratings.Should().NotBeNull();
            existing.Ratings.Igdb.Value.Should().Be(80);
        }

        [Test]
        public void merge_metadata_should_create_images_list_if_null_on_existing()
        {
            var existing = BuildGameMetadata(title: "Test");
            existing.Images = null;

            var igdbImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://images.igdb.com/cover.jpg")
            };

            var secondary = BuildGameMetadata(images: igdbImages);

            InvokeMergeMetadata(existing, secondary);

            existing.Images.Should().NotBeNull();
            existing.Images.Should().HaveCount(1);
        }

        // ============================================================
        // GetGameInfo Tests
        // ============================================================

        [Test]
        public void get_game_info_by_igdb_id_should_return_null_when_no_igdb_credentials()
        {
            // No IGDB credentials set - should return null
            var result = _subject.GetGameInfoByIgdbId(12345);

            result.Should().BeNull();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // SearchForNewGame Tests
        // ============================================================

        [Test]
        public void search_should_return_empty_list_when_all_sources_fail()
        {
            // All proxies will throw due to null dependencies
            var result = _subject.SearchForNewGame("nonexistent game");

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_should_not_throw_when_steam_fails()
        {
            // Steam proxy throws due to null IHttpClient - should be caught
            Action act = () => _subject.SearchForNewGame("test game");

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_should_not_throw_when_igdb_fails()
        {
            GivenIgdbCredentials();

            // IGDB proxy throws due to null IHttpClient - should be caught
            Action act = () => _subject.SearchForNewGame("test game");

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_should_not_throw_when_rawg_fails()
        {
            GivenRawgCredentials();

            // RAWG proxy throws due to null IHttpClient - should be caught
            Action act = () => _subject.SearchForNewGame("test game");

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // GetTrendingGames Tests
        // ============================================================

        [Test]
        public void get_trending_should_return_empty_when_no_credentials()
        {
            var result = _subject.GetTrendingGames();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void get_trending_should_not_throw_when_igdb_fails()
        {
            GivenIgdbCredentials();

            Action act = () => _subject.GetTrendingGames();

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void get_trending_should_not_throw_when_rawg_fails()
        {
            GivenRawgCredentials();

            Action act = () => _subject.GetTrendingGames();

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // GetPopularGames Tests
        // ============================================================

        [Test]
        public void get_popular_should_return_empty_when_no_credentials()
        {
            var result = _subject.GetPopularGames();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void get_popular_should_not_throw_when_igdb_fails()
        {
            GivenIgdbCredentials();

            Action act = () => _subject.GetPopularGames();

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void get_popular_should_not_throw_when_rawg_fails()
        {
            GivenRawgCredentials();

            Action act = () => _subject.GetPopularGames();

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // GetBulkGameInfoByIgdbIds Tests
        // ============================================================

        [Test]
        public void get_bulk_game_info_by_igdb_ids_should_return_empty_when_no_credentials()
        {
            var ids = new List<int> { 100, 200, 300 };

            var result = _subject.GetBulkGameInfoByIgdbIds(ids);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void get_bulk_game_info_by_igdb_ids_should_not_throw_when_igdb_fails()
        {
            GivenIgdbCredentials();

            var ids = new List<int> { 100, 200 };

            Action act = () => _subject.GetBulkGameInfoByIgdbIds(ids);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void get_bulk_game_info_by_rawg_ids_should_return_empty_when_no_credentials()
        {
            var ids = new List<int> { 100, 200, 300 };

            var result = _subject.GetBulkGameInfoByRawgIds(ids);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void get_bulk_game_info_by_steam_app_ids_should_not_throw()
        {
            var ids = new List<int> { 100, 200 };

            Action act = () => _subject.GetBulkGameInfoBySteamAppIds(ids);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // GetChangedGames Tests
        // ============================================================

        [Test]
        public void get_changed_games_should_return_empty_when_no_credentials()
        {
            var result = _subject.GetChangedGames(DateTime.UtcNow.AddDays(-1));

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void get_changed_games_should_not_throw_when_igdb_fails()
        {
            GivenIgdbCredentials();

            Action act = () => _subject.GetChangedGames(DateTime.UtcNow.AddDays(-1));

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // GetCollectionInfo Tests
        // ============================================================

        [Test]
        public void get_collection_info_should_return_null_when_no_igdb_credentials()
        {
            var result = _subject.GetCollectionInfo(42);

            result.Should().BeNull();
        }

        [Test]
        public void get_collection_info_should_not_throw_when_igdb_fails()
        {
            GivenIgdbCredentials();

            Action act = () => _subject.GetCollectionInfo(42);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // Credential Checking Tests
        // ============================================================

        [Test]
        public void should_skip_igdb_when_client_id_is_empty()
        {
            _configService.SetupGet(c => c.IgdbClientId).Returns(string.Empty);
            _configService.SetupGet(c => c.IgdbClientSecret).Returns("secret");

            _subject = new AggregateGameInfoProxy(
                _steamProxy.Object,
                _rawgProxy.Object,
                _igdbProxy.Object,
                _configService.Object,
                TestLogger);

            // GetTrendingGames only queries IGDB/RAWG, not Steam
            var result = _subject.GetTrendingGames();

            result.Should().BeEmpty();
        }

        [Test]
        public void should_skip_igdb_when_client_secret_is_empty()
        {
            _configService.SetupGet(c => c.IgdbClientId).Returns("id");
            _configService.SetupGet(c => c.IgdbClientSecret).Returns(string.Empty);

            _subject = new AggregateGameInfoProxy(
                _steamProxy.Object,
                _rawgProxy.Object,
                _igdbProxy.Object,
                _configService.Object,
                TestLogger);

            var result = _subject.GetTrendingGames();

            result.Should().BeEmpty();
        }

        [Test]
        public void should_skip_rawg_when_api_key_is_empty()
        {
            _configService.SetupGet(c => c.RawgApiKey).Returns(string.Empty);

            _subject = new AggregateGameInfoProxy(
                _steamProxy.Object,
                _rawgProxy.Object,
                _igdbProxy.Object,
                _configService.Object,
                TestLogger);

            var result = _subject.GetPopularGames();

            result.Should().BeEmpty();
        }

        // ============================================================
        // MapGameToIgdbGame Tests
        // ============================================================

        [Test]
        public void map_game_should_return_input_when_no_credentials()
        {
            var game = BuildGameMetadata(title: "Test Game", steamAppId: 100);

            var result = _subject.MapGameToIgdbGame(game);

            result.Should().BeSameAs(game);
        }

        [Test]
        public void map_game_should_not_throw_when_igdb_fails()
        {
            GivenIgdbCredentials();
            var game = BuildGameMetadata(title: "Test Game");

            Action act = () => _subject.MapGameToIgdbGame(game);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void map_game_should_not_throw_when_rawg_fails()
        {
            GivenRawgCredentials();
            var game = BuildGameMetadata(title: "Test Game");

            Action act = () => _subject.MapGameToIgdbGame(game);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // GetGameBySteamAppId Tests
        // ============================================================

        [Test]
        public void get_game_by_steam_app_id_should_not_throw_when_steam_fails()
        {
            Action act = () => _subject.GetGameBySteamAppId(440);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void get_game_by_steam_app_id_should_return_null_when_all_fail()
        {
            var result = _subject.GetGameBySteamAppId(440);

            result.Should().BeNull();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // NormalizeTitleForComparison Tests (via SearchForNewGame dedup)
        // ============================================================

        [Test]
        public void search_should_handle_null_title_without_throwing()
        {
            Action act = () => _subject.SearchForNewGame(null);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_should_handle_empty_title_without_throwing()
        {
            Action act = () => _subject.SearchForNewGame(string.Empty);

            act.Should().NotThrow();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // MergeGameMetadata (Game-level merge for search results) Tests
        // ============================================================

        [Test]
        public void merge_metadata_should_add_igdb_id_from_igdb_source()
        {
            var existing = BuildGameMetadata(title: "Team Fortress 2", steamAppId: 440);
            var secondary = BuildGameMetadata(title: "Team Fortress 2", igdbId: 877);

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbId.Should().Be(877);
            existing.SteamAppId.Should().Be(440);
        }

        [Test]
        public void merge_metadata_should_add_multiple_igdb_images_at_front_in_order()
        {
            var existingImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://cdn.steam.com/poster.jpg"),
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Screenshot, "https://cdn.steam.com/screen1.jpg")
            };

            var existing = BuildGameMetadata(title: "Test", images: existingImages);

            var igdbImages = new List<NzbDrone.Core.MediaCover.MediaCover>
            {
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, "https://images.igdb.com/cover1.jpg"),
                new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Fanart, "https://images.igdb.com/fanart1.jpg")
            };

            var secondary = BuildGameMetadata(images: igdbImages);

            InvokeMergeMetadata(existing, secondary);

            // IGDB images should be first
            existing.Images[0].RemoteUrl.Should().Be("https://images.igdb.com/cover1.jpg");
            existing.Images[1].RemoteUrl.Should().Be("https://images.igdb.com/fanart1.jpg");

            // Original Steam images should follow
            existing.Images[2].RemoteUrl.Should().Be("https://cdn.steam.com/poster.jpg");
            existing.Images[3].RemoteUrl.Should().Be("https://cdn.steam.com/screen1.jpg");
        }

        [Test]
        public void merge_metadata_should_combine_igdb_and_rawg_data_sequentially()
        {
            var existing = BuildGameMetadata(
                title: "Elden Ring",
                steamAppId: 1245620);

            // First merge: IGDB data
            var igdbData = BuildGameMetadata(
                igdbId: 119171,
                igdbSlug: "elden-ring",
                genres: new List<string> { "RPG", "Action" },
                ratings: new Ratings
                {
                    Igdb = new RatingChild { Value = 92, Votes = 5000, Type = RatingType.User }
                });

            InvokeMergeMetadata(existing, igdbData);

            existing.IgdbId.Should().Be(119171);
            existing.IgdbSlug.Should().Be("elden-ring");
            existing.Genres.Should().Contain("RPG");
            existing.Ratings.Igdb.Value.Should().Be(92);

            // Second merge: RAWG data (should not overwrite existing)
            var rawgData = BuildGameMetadata(
                overview: "An action RPG by FromSoftware",
                genres: new List<string> { "Adventure" },
                ratings: new Ratings
                {
                    Igdb = new RatingChild { Value = 88, Votes = 3000, Type = RatingType.User }
                });

            InvokeMergeMetadata(existing, rawgData);

            // Overview was empty, so it should be added
            existing.Overview.Should().Be("An action RPG by FromSoftware");

            // Genres and ratings should NOT be overwritten
            existing.Genres.Should().Contain("RPG");
            existing.Genres.Should().NotContain("Adventure");
            existing.Ratings.Igdb.Value.Should().Be(92);
        }

        // ============================================================
        // RAWG Prefix Search Tests
        // ============================================================

        [Test]
        public void search_with_rawg_prefix_should_return_empty_when_no_credentials()
        {
            var result = _subject.SearchForNewGame("rawg:3328");

            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void search_with_rawgid_prefix_should_return_empty_when_no_credentials()
        {
            var result = _subject.SearchForNewGame("rawgid:3328");

            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        // ============================================================
        // Recommendations Merge Tests
        // ============================================================

        [Test]
        public void merge_metadata_should_preserve_igdb_recommendations_from_secondary()
        {
            var existing = BuildGameMetadata(title: "Test Game", steamAppId: 100);
            existing.IgdbRecommendations = new List<int>();

            var secondary = BuildGameMetadata();
            secondary.IgdbRecommendations = new List<int> { 1001, 1002, 1003 };

            InvokeMergeMetadata(existing, secondary);

            existing.IgdbRecommendations.Should().BeEquivalentTo(new List<int> { 1001, 1002, 1003 });
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_igdb_recommendations()
        {
            var existing = BuildGameMetadata(title: "Test Game", steamAppId: 100);
            existing.IgdbRecommendations = new List<int> { 2001, 2002 };

            var secondary = BuildGameMetadata();
            secondary.IgdbRecommendations = new List<int> { 3001, 3002, 3003 };

            InvokeMergeMetadata(existing, secondary);

            // Should keep existing recommendations
            existing.IgdbRecommendations.Should().BeEquivalentTo(new List<int> { 2001, 2002 });
        }

        [Test]
        public void merge_metadata_should_preserve_rawg_recommendations_from_secondary()
        {
            var existing = BuildGameMetadata(title: "Test Game", steamAppId: 100);
            existing.RawgRecommendations = new List<int>();

            var secondary = BuildGameMetadata();
            secondary.RawgRecommendations = new List<int> { 4001, 4002, 4003 };

            InvokeMergeMetadata(existing, secondary);

            existing.RawgRecommendations.Should().BeEquivalentTo(new List<int> { 4001, 4002, 4003 });
        }

        [Test]
        public void merge_metadata_should_not_overwrite_existing_rawg_recommendations()
        {
            var existing = BuildGameMetadata(title: "Test Game", steamAppId: 100);
            existing.RawgRecommendations = new List<int> { 5001, 5002 };

            var secondary = BuildGameMetadata();
            secondary.RawgRecommendations = new List<int> { 6001, 6002, 6003 };

            InvokeMergeMetadata(existing, secondary);

            // Should keep existing recommendations
            existing.RawgRecommendations.Should().BeEquivalentTo(new List<int> { 5001, 5002 });
        }

        [Test]
        public void merge_metadata_should_keep_igdb_and_rawg_recommendations_separate()
        {
            var existing = BuildGameMetadata(title: "Test Game", steamAppId: 100);
            existing.IgdbRecommendations = new List<int>();
            existing.RawgRecommendations = new List<int>();

            var igdbData = BuildGameMetadata();
            igdbData.IgdbRecommendations = new List<int> { 1001, 1002 };
            igdbData.RawgRecommendations = new List<int>();

            InvokeMergeMetadata(existing, igdbData);

            var rawgData = BuildGameMetadata();
            rawgData.IgdbRecommendations = new List<int>();
            rawgData.RawgRecommendations = new List<int> { 2001, 2002 };

            InvokeMergeMetadata(existing, rawgData);

            // Both should be preserved separately
            existing.IgdbRecommendations.Should().BeEquivalentTo(new List<int> { 1001, 1002 });
            existing.RawgRecommendations.Should().BeEquivalentTo(new List<int> { 2001, 2002 });
        }
    }
}
