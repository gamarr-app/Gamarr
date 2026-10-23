using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaFileDeletionService
{
    /// <summary>
    /// An update or DLC imported as a folder (Updates/&lt;version&gt;,
    /// DLC/&lt;name&gt;) carries a non-empty RelativePath, so it is not the game
    /// folder, but its path is still a directory. Deleting one used to remove
    /// the row and nothing else (gamarr-app/Gamarr#215).
    /// </summary>
    [TestFixture]
    public class DeleteComponentGameFileFixture : CoreTest<Core.MediaFiles.MediaFileDeletionService>
    {
        private const string RootFolder = @"C:\Test\Games";
        private Game _game;
        private GameFile _componentFile;
        private string _componentPath;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                .With(s => s.Path = Path.Combine(RootFolder, "Game Title"))
                .Build();

            _componentPath = Path.Combine(_game.Path, Path.Combine("Updates", "4.9.1"));

            _componentFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = Path.Combine("Updates", "4.9.1"))
                .With(f => f.Path = _componentPath)
                .With(f => f.ComponentId = 0)
                .Build();

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetParentFolder(_game.Path))
                .Returns(RootFolder);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(RootFolder))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetDirectories(RootFolder))
                .Returns(new[] { _game.Path });

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_game.Path))
                .Returns(true);
        }

        private void GivenComponentFolderExists()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_componentPath))
                .Returns(true);
        }

        private void GivenComponentSlot(GameComponentType type, string key)
        {
            _componentFile.ComponentId = 7;

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByGame(_game.Id))
                .Returns(new List<GameFile>());

            Mocker.GetMock<IGameComponentRepository>()
                .Setup(s => s.GetByGame(_game.Id))
                .Returns(new List<GameComponent>
                {
                    new GameComponent
                    {
                        Id = 7,
                        GameId = _game.Id,
                        ComponentType = type,
                        Key = key,
                        Title = key,
                        Monitored = true
                    }
                });
        }

        [Test]
        public void should_recycle_the_component_folder_and_nothing_else()
        {
            GivenComponentFolderExists();

            Subject.DeleteGameFile(_game, _componentFile);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_componentPath), Times.Once());
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(_game.Path), Times.Never());
            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_componentFile, DeleteMediaFileReason.Manual), Times.Once());
        }

        [Test]
        public void should_not_hard_delete_the_component_folder()
        {
            GivenComponentFolderExists();

            Subject.DeleteGameFile(_game, _componentFile);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_report_an_error_when_the_component_folder_cannot_be_deleted()
        {
            GivenComponentFolderExists();

            Mocker.GetMock<IRecycleBinProvider>()
                .Setup(s => s.DeleteFolder(_componentPath))
                .Throws(new IOException($"Access to the path '{_componentPath}' is denied."));

            var exception = Assert.Throws<NzbDroneClientException>(() => Subject.DeleteGameFile(_game, _componentFile));

            exception.Message.Should().Contain($"Access to the path '{_componentPath}' is denied.");

            // The row stays so the folder can be retried, instead of leaving an
            // untracked folder the next scan re-adopts.
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<GameFile>(), It.IsAny<DeleteMediaFileReason>()), Times.Never());
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_warn_when_the_component_folder_is_already_gone()
        {
            Subject.DeleteGameFile(_game, _componentFile);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_componentFile, DeleteMediaFileReason.Manual), Times.Once());
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_remove_the_component_slot_of_a_deleted_update()
        {
            GivenComponentFolderExists();
            GivenComponentSlot(GameComponentType.Update, "4.9.1");

            Subject.DeleteGameFile(_game, _componentFile);

            // A monitored update slot with no files left would be searched for
            // and grabbed again right after the user deleted it.
            Mocker.GetMock<IGameComponentRepository>().Verify(v => v.Delete(7), Times.Once());
        }

        [Test]
        public void should_remove_the_component_slot_of_a_deleted_imported_dlc()
        {
            GivenComponentFolderExists();
            GivenComponentSlot(GameComponentType.Dlc, "import:Some.Expansion-GRP");

            Subject.DeleteGameFile(_game, _componentFile);

            Mocker.GetMock<IGameComponentRepository>().Verify(v => v.Delete(7), Times.Once());
        }

        [Test]
        public void should_keep_a_metadata_dlc_slot()
        {
            GivenComponentFolderExists();
            GivenComponentSlot(GameComponentType.Dlc, "igdb:1234");

            Subject.DeleteGameFile(_game, _componentFile);

            // The catalogue entry outlives its files: it goes back to wanted.
            Mocker.GetMock<IGameComponentRepository>().Verify(v => v.Delete(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_keep_the_slot_while_another_file_still_uses_it()
        {
            GivenComponentFolderExists();
            GivenComponentSlot(GameComponentType.Update, "4.9.1");

            var other = Builder<GameFile>.CreateNew()
                .With(f => f.Id = _componentFile.Id + 1)
                .With(f => f.RelativePath = Path.Combine("Updates", "4.9.1", "patch.nsp"))
                .With(f => f.ComponentId = 7)
                .Build();

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByGame(_game.Id))
                .Returns(new List<GameFile> { other });

            Subject.DeleteGameFile(_game, _componentFile);

            Mocker.GetMock<IGameComponentRepository>().Verify(v => v.Delete(It.IsAny<int>()), Times.Never());
        }
    }
}
