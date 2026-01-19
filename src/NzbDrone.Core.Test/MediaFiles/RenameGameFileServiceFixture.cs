using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class RenameGameFileServiceFixture : CoreTest<RenameGameFileService>
    {
        private Game _game;
        private List<GameFile> _gameFiles;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .Build();

            _gameFiles = Builder<GameFile>.CreateListOfSize(2)
                                                .All()
                                                .With(e => e.GameId = _game.Id)
                                                .Build()
                                                .ToList();

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(_game.Id))
                  .Returns(_game);
        }

        private void GivenNoGameFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetGames(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<GameFile>());
        }

        private void GivenGameFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetGames(It.IsAny<IEnumerable<int>>()))
                  .Returns(_gameFiles);
        }

        private void GivenMovedFiles()
        {
            Mocker.GetMock<IMoveGameFiles>()
                  .Setup(s => s.MoveGameFile(It.IsAny<GameFile>(), _game));
        }

        [Test]
        public void should_not_publish_event_if_no_files_to_rename()
        {
            GivenNoGameFiles();

            Subject.Execute(new RenameFilesCommand(_game.Id, new List<int> { 1 }));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<GameRenamedEvent>()), Times.Never());
        }

        [Test]
        public void should_not_publish_event_if_no_files_are_renamed()
        {
            GivenGameFiles();

            Mocker.GetMock<IMoveGameFiles>()
                  .Setup(s => s.MoveGameFile(It.IsAny<GameFile>(), It.IsAny<Game>()))
                  .Throws(new SameFilenameException("Same file name", "Filename"));

            Subject.Execute(new RenameFilesCommand(_game.Id, new List<int> { 1 }));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<GameRenamedEvent>()), Times.Never());
        }

        [Test]
        public void should_publish_event_if_files_are_renamed()
        {
            GivenGameFiles();
            GivenMovedFiles();

            Subject.Execute(new RenameFilesCommand(_game.Id, new List<int> { 1 }));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<GameRenamedEvent>()), Times.Once());
        }

        [Test]
        public void should_update_moved_files()
        {
            GivenGameFiles();
            GivenMovedFiles();

            Subject.Execute(new RenameFilesCommand(_game.Id, new List<int> { 1 }));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<GameFile>()), Times.Exactly(2));
        }

        [Test]
        public void should_get_gamefiles_by_ids_only()
        {
            GivenGameFiles();
            GivenMovedFiles();

            var files = new List<int> { 1 };

            Subject.Execute(new RenameFilesCommand(_game.Id, files));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.GetGames(files), Times.Once());
        }
    }
}
