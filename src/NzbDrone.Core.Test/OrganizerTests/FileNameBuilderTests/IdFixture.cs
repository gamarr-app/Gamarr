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
                      .With(s => s.ImdbId = "tt12345")
                      .With(s => s.IgdbId = 123456)
                      .Build();

            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [Test]
        public void should_add_imdb_id()
        {
            _namingConfig.GameFolderFormat = "{Game Title} ({ImdbId})";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title ({_game.ImdbId})");
        }

        [Test]
        public void should_add_igdb_id()
        {
            _namingConfig.GameFolderFormat = "{Game Title} ({IgdbId})";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title ({_game.IgdbId})");
        }

        [Test]
        public void should_add_imdb_tag()
        {
            _namingConfig.GameFolderFormat = "{Game Title} {imdb-{ImdbId}}";

            Subject.GetGameFolder(_game)
                   .Should().Be($"Game Title {{imdb-{_game.ImdbId}}}");
        }

        [TestCase("{Game Title} {imdb-{ImdbId}}")]
        [TestCase("{Game Title} {imdbid-{ImdbId}}")]
        [TestCase("{Game Title} {{imdb-{ImdbId}}}")]
        [TestCase("{Game Title} {{imdbid-{ImdbId}}}")]
        public void should_skip_imdb_tag_if_null(string gameFormat)
        {
            _namingConfig.GameFolderFormat = gameFormat;

            _game.ImdbId = null;

            Subject.GetGameFolder(_game)
                   .Should().Be("Game Title");
        }

        [TestCase("{Game Title} {{imdb-{ImdbId}}}")]
        public void should_handle_imdb_tag_curly_brackets(string gameFormat)
        {
            _namingConfig.GameFolderFormat = gameFormat;

            Subject.GetGameFolder(_game)
                .Should().Be($"Game Title {{{{imdb-{_game.ImdbId}}}}}");
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
