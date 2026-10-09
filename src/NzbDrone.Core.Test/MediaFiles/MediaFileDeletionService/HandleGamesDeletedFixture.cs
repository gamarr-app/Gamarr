using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaFileDeletionService
{
    [TestFixture]
    public class HandleGamesDeletedFixture : CoreTest<Core.MediaFiles.MediaFileDeletionService>
    {
        private const string RootFolder = @"C:\Test\Games";
        private Game _firstGame;
        private Game _secondGame;

        [SetUp]
        public void Setup()
        {
            _firstGame = Builder<Game>.CreateNew()
                                      .With(s => s.Id = 1)
                                      .With(s => s.Path = Path.Combine(RootFolder, "First Game").AsOsAgnostic())
                                      .Build();

            _secondGame = Builder<Game>.CreateNew()
                                       .With(s => s.Id = 2)
                                       .With(s => s.Path = Path.Combine(RootFolder, "Second Game").AsOsAgnostic())
                                       .Build();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.AllGamePaths())
                  .Returns(new Dictionary<int, string>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);
        }

        private GamesDeletedEvent GivenGamesDeleted(params Game[] games)
        {
            return new GamesDeletedEvent(new List<Game>(games), true, false);
        }

        [Test]
        public void should_delete_game_folders_and_publish_delete_completed()
        {
            Subject.HandleAsync(GivenGamesDeleted(_firstGame, _secondGame));

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_firstGame.Path), Times.Once());
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_secondGame.Path), Times.Once());
            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<DeleteCompletedEvent>()), Times.Once());
        }

        [Test]
        public void should_continue_and_publish_delete_completed_when_deleting_a_game_folder_fails()
        {
            Mocker.GetMock<IRecycleBinProvider>()
                  .Setup(s => s.DeleteFolder(_firstGame.Path))
                  .Throws(new IOException($"Access to the path '{_firstGame.Path}' is denied."));

            Subject.HandleAsync(GivenGamesDeleted(_firstGame, _secondGame));

            // The failure must not abort the rest of the batch...
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_secondGame.Path), Times.Once());

            // ...and consumers waiting on completion must still hear back.
            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<DeleteCompletedEvent>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
