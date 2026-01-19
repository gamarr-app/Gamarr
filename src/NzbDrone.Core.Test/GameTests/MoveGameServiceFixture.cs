using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class MoveGameServiceFixture : CoreTest<MoveGameService>
    {
        private Game _game;
        private MoveGameCommand _command;
        private BulkMoveGameCommand _bulkCommand;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                .CreateNew()
                .Build();

            _command = new MoveGameCommand
            {
                GameId = 1,
                SourcePath = @"C:\Test\Games\Game".AsOsAgnostic(),
                DestinationPath = @"C:\Test\Games2\Game".AsOsAgnostic()
            };

            _bulkCommand = new BulkMoveGameCommand
            {
                Games = new List<BulkMoveGame>
                                    {
                                        new BulkMoveGame
                                        {
                                            GameId = 1,
                                            SourcePath = @"C:\Test\Games\Game".AsOsAgnostic()
                                        }
                                    },
                DestinationRootFolder = @"C:\Test\Games2".AsOsAgnostic()
            };

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(It.IsAny<int>()))
                  .Returns(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);
        }

        private void GivenFailedMove()
        {
            Mocker.GetMock<IDiskTransferService>()
                  .Setup(s => s.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), TransferMode.Move))
                  .Throws<IOException>();
        }

        [Test]
        public void should_log_error_when_move_throws_an_exception()
        {
            GivenFailedMove();

            Subject.Execute(_command);

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_revert_game_path_on_error()
        {
            GivenFailedMove();

            Subject.Execute(_command);

            ExceptionVerification.ExpectedErrors(1);

            Mocker.GetMock<IGameService>()
                  .Verify(v => v.UpdateGame(It.IsAny<Game>()), Times.Once());
        }

        [Test]
        public void should_use_destination_path()
        {
            Subject.Execute(_command);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(_command.SourcePath, _command.DestinationPath, TransferMode.Move), Times.Once());

            Mocker.GetMock<IBuildFileNames>()
                  .Verify(v => v.GetGameFolder(It.IsAny<Game>(), null), Times.Never());
        }

        [Test]
        public void should_build_new_path_when_root_folder_is_provided()
        {
            var gameFolder = "Game";
            var expectedPath = Path.Combine(_bulkCommand.DestinationRootFolder, gameFolder);

            Mocker.GetMock<IBuildFileNames>()
                    .Setup(s => s.GetGameFolder(It.IsAny<Game>(), null))
                    .Returns(gameFolder);

            Subject.Execute(_bulkCommand);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(_bulkCommand.Games.First().SourcePath, expectedPath, TransferMode.Move), Times.Once());
        }

        [Test]
        public void should_skip_game_folder_if_it_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.Execute(_command);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(_command.SourcePath, _command.DestinationPath, TransferMode.Move), Times.Never());

            Mocker.GetMock<IBuildFileNames>()
                  .Verify(v => v.GetGameFolder(It.IsAny<Game>(), null), Times.Never());
        }
    }
}
