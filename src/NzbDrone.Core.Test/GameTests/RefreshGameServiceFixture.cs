using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class RefreshGameServiceFixture : CoreTest<RefreshGameService>
    {
        private GameMetadata _game;
        private GameCollection _gameCollection;
        private Game _existingGame;

        [SetUp]
        public void Setup()
        {
            _game = Builder<GameMetadata>.CreateNew()
                .With(s => s.Status = GameStatusType.Released)
                .Build();

            _gameCollection = Builder<GameCollection>.CreateNew()
                .Build();

            _existingGame = Builder<Game>.CreateNew()
                .With(s => s.GameMetadata.Value.Status = GameStatusType.Released)
                .Build();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(_game.Id))
                  .Returns(_existingGame);

            Mocker.GetMock<IGameMetadataService>()
                  .Setup(s => s.Get(_game.Id))
                  .Returns(_game);

            Mocker.GetMock<IAddGameCollectionService>()
                  .Setup(v => v.AddGameCollection(It.IsAny<GameCollection>()))
                  .Returns(_gameCollection);

            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfo(It.IsAny<int>()))
                  .Callback<int>((i) => { throw new GameNotFoundException(i); });

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), null))
                  .Returns(string.Empty);

            Mocker.GetMock<IAutoTaggingService>()
                .Setup(s => s.GetTagChanges(_existingGame))
                .Returns(new AutoTaggingChanges());
        }

        private void GivenNewGameInfo(GameMetadata game)
        {
            Mocker.GetMock<IProvideGameInfo>()
                  .Setup(s => s.GetGameInfo(_game.IgdbId))
                  .Returns(game);
        }

        [Test]
        public void should_log_error_if_igdb_id_not_found()
        {
            Subject.Execute(new RefreshGameCommand(new List<int> { _game.Id }));

            Mocker.GetMock<IGameMetadataService>()
                .Verify(v => v.Upsert(It.Is<GameMetadata>(s => s.Status == GameStatusType.Deleted)), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_update_if_igdb_id_changed()
        {
            var newGameInfo = _game.JsonClone();
            newGameInfo.IgdbId = _game.IgdbId + 1;

            GivenNewGameInfo(newGameInfo);

            Subject.Execute(new RefreshGameCommand(new List<int> { _game.Id }));

            Mocker.GetMock<IGameMetadataService>()
                .Verify(v => v.Upsert(It.Is<GameMetadata>(s => s.IgdbId == newGameInfo.IgdbId)));

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_mark_as_deleted_if_igdb_id_not_found()
        {
            Subject.Execute(new RefreshGameCommand(new List<int> { _game.Id }));

            Mocker.GetMock<IGameMetadataService>()
                .Verify(v => v.Upsert(It.Is<GameMetadata>(s => s.Status == GameStatusType.Deleted)), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_remark_as_deleted_if_igdb_id_not_found()
        {
            _game.Status = GameStatusType.Deleted;

            Subject.Execute(new RefreshGameCommand(new List<int> { _game.Id }));

            Mocker.GetMock<IGameMetadataService>()
                .Verify(v => v.Upsert(It.IsAny<GameMetadata>()), Times.Never());

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
