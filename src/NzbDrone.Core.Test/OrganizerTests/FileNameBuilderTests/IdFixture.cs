#pragma warning disable CS0618
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class IdFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                      .CreateNew()
                      .With(s => s.Title = "Game Title")
                      .With(s => s.SteamAppId = 12345)
                      .With(s => s.IgdbId = 123456)
                      .Build();

            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [Test]
        public void should_add_steam_app_id()
        {
            _namingConfig.GameFolderFormat = "{Game Title} ({SteamAppId})";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title ({_game.SteamAppId})");
        }

        [Test]
        public void should_add_igdb_id()
        {
            _namingConfig.GameFolderFormat = "{Game Title} ({IgdbId})";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title ({_game.IgdbId})");
        }

        [Test]
        public void should_add_steam_tag()
        {
            _namingConfig.GameFolderFormat = "{Game Title} {steam-{SteamAppId}}";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title {{steam-{_game.SteamAppId}}}");
        }

        [TestCase("{Game Title} {steam-{SteamAppId}}")]
        [TestCase("{Game Title} {steamid-{SteamAppId}}")]
        [TestCase("{Game Title} {{steam-{SteamAppId}}}")]
        [TestCase("{Game Title} {{steamid-{SteamAppId}}}")]
        public void should_skip_steam_tag_if_zero(string gameFormat)
        {
            _namingConfig.GameFolderFormat = gameFormat;

            _game.SteamAppId = 0;

            Subject.GetGameFolder(_game)
                   .Should().Be("Game Title");
        }

        [TestCase("{Game Title} {{steam-{SteamAppId}}}")]
        public void should_handle_steam_tag_curly_brackets(string gameFormat)
        {
            _namingConfig.GameFolderFormat = gameFormat;

            Subject.GetGameFolder(_game)
                .Should().Be($"Game Title {{{{steam-{_game.SteamAppId}}}}}");
        }

        [TestCase("{Game Title} {{igdb-{IgdbId}}}")]
        public void should_handle_igdb_tag_curly_brackets(string gameFormat)
        {
            _namingConfig.GameFolderFormat = gameFormat;

            Subject.GetGameFolder(_game)
                .Should().Be($"Game Title {{{{igdb-{_game.IgdbId}}}}}");
        }
    }
}
