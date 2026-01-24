#pragma warning disable CS0618
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Gamarr.Api.V3.AutoTagging;
using Gamarr.Http.ClientSchema;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class GameFixture : IntegrationTest
    {
        [Test]
        [Order(0)]
        public void add_game_with_tags_should_store_them()
        {
            EnsureNoGame(21, "Half-Life 2");
            var tag = EnsureTag("abc");

            var game = Games.Lookup("igdb:21").Single();

            game.QualityProfileId = 1;
            game.Path = Path.Combine(GameRootFolder, game.Title);
            game.Tags = new HashSet<int>();
            game.Tags.Add(tag.Id);

            var result = Games.Post(game);

            result.Should().NotBeNull();
            result.Tags.Should().Equal(tag.Id);
        }

        [Test]
        [Order(0)]
        public void add_game_should_trigger_autotag()
        {
            var tag = EnsureTag("autotag-test");
            var game = Games.Lookup("igdb:21").Single();
            game.Genres = new List<string> { "Thriller" };

            var item = AutoTagging.Post(new AutoTaggingResource
            {
                Name = "Test",
                RemoveTagsAutomatically = false,
                Tags = new HashSet<int> { tag.Id },
                Specifications = new List<AutoTaggingSpecificationSchema>
                {
                    new AutoTaggingSpecificationSchema
                    {
                        Name = "Test",
                        Implementation = "GenreSpecification",
                        ImplementationName = "Genre",
                        Negate = false,
                        Required = false,
                        Fields = new List<Field>
                        {
                            new Field
                            {
                                Name = "value",
                                Label = "Genre(s)",
                                Type = "tag",
                                Value = new List<string> { "Thriller" }
                            }
                        }
                    }
                }
            });

            EnsureNoGame(21, "Half-Life 2");

            game.QualityProfileId = 1;
            game.Path = Path.Combine(GameRootFolder, game.Title);

            var result = Games.Post(game);

            result.Should().NotBeNull();
            result.Tags.Should().Contain(tag.Id);
        }

        [Test]
        [Order(0)]
        public void add_game_without_profileid_should_return_badrequest()
        {
            EnsureNoGame(21, "Half-Life 2");

            var game = Games.Lookup("igdb:21").Single();

            game.Path = Path.Combine(GameRootFolder, game.Title);

            Games.InvalidPost(game);
        }

        [Test]
        [Order(0)]
        public void add_game_without_path_should_return_badrequest()
        {
            EnsureNoGame(21, "Half-Life 2");

            var game = Games.Lookup("igdb:21").Single();

            game.QualityProfileId = 1;

            Games.InvalidPost(game);
        }

        [Test]
        [Order(1)]
        public void add_game()
        {
            EnsureNoGame(21, "Half-Life 2");

            var game = Games.Lookup("igdb:21").Single();

            game.QualityProfileId = 1;
            game.Path = Path.Combine(GameRootFolder, game.Title);

            var result = Games.Post(game);

            result.Should().NotBeNull();
            result.Id.Should().NotBe(0);
            result.QualityProfileId.Should().Be(1);
            result.Path.Should().Be(Path.Combine(GameRootFolder, game.Title));
        }

        [Test]
        [Order(2)]
        public void get_all_games()
        {
            EnsureGame(21, "Half-Life 2");
            EnsureGame(38, "Portal");

            var games = Games.All();

            games.Should().NotBeNullOrEmpty();
            games.Should().Contain(v => v.IgdbId == 21);
            games.Should().Contain(v => v.IgdbId == 38);
        }

        [Test]
        [Order(2)]
        public void get_game_by_igdbid()
        {
            EnsureGame(21, "Half-Life 2");
            EnsureGame(38, "Portal");

            var queryParams = new Dictionary<string, object>()
            {
                { "igdbId", 21 }
            };

            var games = Games.All(queryParams);

            games.Should().NotBeNullOrEmpty();
            games.Should().Contain(v => v.IgdbId == 21);
        }

        [Test]
        [Order(2)]
        public void get_game_by_id()
        {
            var game = EnsureGame(21, "Half-Life 2");

            var result = Games.Get(game.Id);

            result.IgdbId.Should().Be(21);
        }

        [Test]
        public void get_game_by_unknown_id_should_return_404()
        {
            var result = Games.InvalidGet(1000000);
        }

        [Test]
        [Order(2)]
        public void update_game_profile_id()
        {
            var game = EnsureGame(21, "Half-Life 2");

            var profileId = 1;
            if (game.QualityProfileId == profileId)
            {
                profileId = 2;
            }

            game.QualityProfileId = profileId;

            var result = Games.Put(game);

            Games.Get(game.Id).QualityProfileId.Should().Be(profileId);
        }

        [Test]
        [Order(3)]
        public void update_game_monitored()
        {
            var game = EnsureGame(21, "Half-Life 2", false);

            game.Monitored.Should().BeFalse();

            game.Monitored = true;

            var result = Games.Put(game);

            result.Monitored.Should().BeTrue();
        }

        [Test]
        [Order(3)]
        public void update_game_tags()
        {
            var game = EnsureGame(21, "Half-Life 2");
            var tag = EnsureTag("abc");

            if (game.Tags.Contains(tag.Id))
            {
                game.Tags.Remove(tag.Id);

                var result = Games.Put(game);
                Games.Get(game.Id).Tags.Should().NotContain(tag.Id);
            }
            else
            {
                game.Tags.Add(tag.Id);

                var result = Games.Put(game);
                Games.Get(game.Id).Tags.Should().Contain(tag.Id);
            }
        }

        [Test]
        [Order(4)]
        public void delete_game()
        {
            var game = EnsureGame(21, "Half-Life 2");

            Games.Get(game.Id).Should().NotBeNull();

            Games.Delete(game.Id);

            Games.All().Should().NotContain(v => v.IgdbId == 21);
        }
    }
}
