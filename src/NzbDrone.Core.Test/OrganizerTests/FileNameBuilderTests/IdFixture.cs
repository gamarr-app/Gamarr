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
                      .With(s => s.IgdbId = 123456)
                      .With(s => s.SteamAppId = 789012)
                      .Build();

            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [Test]
        public void should_add_igdb_id()
        {
            _namingConfig.GameFolderFormat = "{Game Title} ({IgdbId})";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title ({_game.IgdbId})");
        }

        [Test]
        public void should_add_steam_app_id()
        {
            _namingConfig.GameFolderFormat = "{Game Title} ({SteamAppId})";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title ({_game.SteamAppId})");
        }

        [TestCase("{Game Title} {{igdb-{IgdbId}}}")]
        public void should_handle_igdb_tag_curly_brackets(string gameFormat)
        {
            _namingConfig.GameFolderFormat = gameFormat;

            Subject.GetGameFolder(_game)
                .Should().Be($"Game Title {{{{igdb-{_game.IgdbId}}}}}");
        }

        [TestCase("{Game Title} {{steam-{SteamAppId}}}")]
        public void should_handle_steam_tag_curly_brackets(string gameFormat)
        {
            _namingConfig.GameFolderFormat = gameFormat;

            Subject.GetGameFolder(_game)
                .Should().Be($"Game Title {{{{steam-{_game.SteamAppId}}}}}");
        }
    }
}
