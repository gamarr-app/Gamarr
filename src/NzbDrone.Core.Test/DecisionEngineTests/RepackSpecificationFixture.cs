using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class RepackSpecificationFixture : CoreTest<RepackSpecification>
    {
        private ParsedGameInfo _parsedGameInfo;
        private Game _game;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            _parsedGameInfo = Builder<ParsedGameInfo>.CreateNew()
                                                           .With(p => p.Quality = new QualityModel(Quality.Scene,
                                                               new Revision(2, 0, false)))
                                                           .With(p => p.ReleaseGroup = "Gamarr")
                                                           .Build();

            _game = Builder<Game>.CreateNew()
                                        .With(e => e.GameFileId = 0)
                                        .Build();
        }

        [Test]
        public void should_return_true_if_it_is_not_a_repack()
        {
            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void should_return_true_if_there_are_is_no_game_file()
        {
            _parsedGameInfo.Quality.Revision.IsRepack = true;

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void should_return_true_if_is_a_repack_for_a_different_quality()
        {
            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                                .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                                .With(e => e.ReleaseGroup = "Gamarr")
                                                                .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void should_return_true_if_is_a_repack_for_existing_file()
        {
            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                 .With(e => e.ReleaseGroup = "Gamarr")
                                                 .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void should_return_false_if_is_a_repack_for_a_different_file()
        {
            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                 .With(e => e.ReleaseGroup = "NotGamarr")
                                                 .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_false_if_release_group_for_existing_file_is_unknown()
        {
            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                 .With(e => e.ReleaseGroup = "")
                                                 .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_false_if_release_group_for_release_is_unknown()
        {
            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _parsedGameInfo.ReleaseGroup = null;

            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                 .With(e => e.ReleaseGroup = "Gamarr")
                                                 .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_false_when_repack_but_auto_download_repack_is_false()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                 .With(e => e.ReleaseGroup = "Gamarr")
                                                 .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_true_when_repack_but_auto_download_repack_is_true()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                 .With(e => e.ReleaseGroup = "Gamarr")
                                                 .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void should_return_true_when_repacks_are_not_preferred()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            _parsedGameInfo.Quality.Revision.IsRepack = true;
            _game.GameFileId = 1;
            _game.GameFile = Builder<GameFile>.CreateNew()
                                                 .With(e => e.Quality = new QualityModel(Quality.Scene))
                                                 .With(e => e.ReleaseGroup = "Gamarr")
                                                 .Build();

            var remoteGame = Builder<RemoteGame>.CreateNew()
                                                      .With(e => e.ParsedGameInfo = _parsedGameInfo)
                                                      .With(e => e.Game = _game)
                                                      .Build();

            Subject.IsSatisfiedBy(remoteGame, null)
                   .Accepted
                   .Should()
                   .BeTrue();
        }
    }
}
