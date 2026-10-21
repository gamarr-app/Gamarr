using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.RomCatalog
{
    [TestFixture]
    public class GameFileHashServiceFixture : DbTest<GameFileHashService, GameFileHash>
    {
        private Game _game;
        private GameFile _gameFile;
        private string _filePath;
        private IGameFileHashRepository _repository;

        [SetUp]
        public void Setup()
        {
            _game = new Game { Id = 12, Path = Path.Combine(TempFolder, "The Game") };
            Directory.CreateDirectory(_game.Path);

            _gameFile = new GameFile
            {
                Id = 3,
                GameId = _game.Id,
                ComponentId = 5,
                RelativePath = "base.nsp",
                Size = 4
            };

            _filePath = Path.Combine(_game.Path, _gameFile.RelativePath);

            _repository = Mocker.Resolve<GameFileHashRepository>();
            Mocker.SetConstant(_repository);

            Mocker.GetMock<IGameService>()
                  .Setup(x => x.FindGame(_game.Id))
                  .Returns(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(x => x.GetFilesByGame(_game.Id))
                  .Returns(() => new List<GameFile> { _gameFile });
        }

        [Test]
        public void should_store_the_hash_triplet_of_a_component_file()
        {
            var content = new byte[] { 1, 2, 3, 4 };
            GivenFile(content, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));

            Subject.Execute(new GameFileHashCommand(_game.Id));

            var stored = AllStoredHashes().Should().ContainSingle().Subject;
            stored.GameFileId.Should().Be(_gameFile.Id);
            stored.Size.Should().Be(content.Length);
            stored.Sha1.Should().Be(Sha1Of(content));
            stored.Md5.Should().NotBeNullOrWhiteSpace();
            stored.Crc32.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public void should_not_rehash_a_file_whose_size_and_last_write_are_unchanged()
        {
            GivenFile(new byte[] { 1, 2, 3, 4 }, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));

            Subject.Execute(new GameFileHashCommand(_game.Id));
            Subject.Execute(new GameFileHashCommand(_game.Id));

            Mocker.GetMock<IDiskProvider>().Verify(x => x.OpenReadStream(_filePath), Times.Once());
            AllStoredHashes().Should().HaveCount(1);
        }

        [Test]
        public void should_rehash_when_the_last_write_moves_without_the_size_changing()
        {
            GivenFile(new byte[] { 1, 2, 3, 4 }, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));

            Subject.Execute(new GameFileHashCommand(_game.Id));

            var replacement = new byte[] { 9, 9, 9, 9 };
            GivenFile(replacement, new DateTime(2026, 10, 21, 8, 0, 0, DateTimeKind.Utc));

            Subject.Execute(new GameFileHashCommand(_game.Id));

            Mocker.GetMock<IDiskProvider>().Verify(x => x.OpenReadStream(_filePath), Times.Exactly(2));
            AllStoredHashes().Should().ContainSingle(x => x.Sha1 == Sha1Of(replacement));
        }

        [Test]
        public void should_not_report_a_stored_hash_once_the_recorded_size_no_longer_matches()
        {
            GivenFile(new byte[] { 1, 2, 3, 4 }, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));

            Subject.Execute(new GameFileHashCommand(_game.Id));
            Subject.GetCurrentHashes(new List<GameFile> { _gameFile }).Should().HaveCount(1);

            // What the scan records when the file on disk was replaced.
            _gameFile.Size = 8;

            Subject.GetCurrentHashes(new List<GameFile> { _gameFile }).Should().BeEmpty();
        }

        [Test]
        public void should_not_touch_the_disk_when_reading_stored_hashes()
        {
            GivenFile(new byte[] { 1, 2, 3, 4 }, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));

            Subject.Execute(new GameFileHashCommand(_game.Id));
            Mocker.GetMock<IDiskProvider>().Invocations.Clear();

            Subject.GetCurrentHashes(new List<GameFile> { _gameFile }).Should().HaveCount(1);

            Mocker.GetMock<IDiskProvider>().VerifyNoOtherCalls();
        }

        [Test]
        public void should_skip_files_that_belong_to_no_component()
        {
            GivenFile(new byte[] { 1, 2, 3, 4 }, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));
            _gameFile.ComponentId = 0;

            Subject.Execute(new GameFileHashCommand(_game.Id));

            AllStoredHashes().Should().BeEmpty();
            Mocker.GetMock<IDiskProvider>().Verify(x => x.OpenReadStream(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_warn_and_continue_when_a_file_cannot_be_read()
        {
            Mocker.GetMock<IDiskProvider>().Setup(x => x.FileExists(_filePath)).Returns(true);
            Mocker.GetMock<IDiskProvider>().Setup(x => x.GetFileSize(_filePath)).Returns(4);
            Mocker.GetMock<IDiskProvider>().Setup(x => x.FileGetLastWrite(_filePath)).Returns(DateTime.UtcNow);
            Mocker.GetMock<IDiskProvider>().Setup(x => x.OpenReadStream(_filePath)).Throws(new IOException("NFS went away"));

            Subject.Execute(new GameFileHashCommand(_game.Id));

            AllStoredHashes().Should().BeEmpty();
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_queue_one_hash_pass_per_game_rather_than_hashing_inline()
        {
            Subject.QueueHashing(_game.Id);
            Subject.Handle(new GameFileAddedEvent(_gameFile));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(x => x.Push(It.Is<GameFileHashCommand>(c => c.GameId == _game.Id), CommandPriority.Normal, CommandTrigger.Unspecified), Times.Exactly(2));

            Mocker.GetMock<IDiskProvider>().Verify(x => x.OpenReadStream(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_drop_stored_hashes_when_the_game_file_is_deleted()
        {
            GivenFile(new byte[] { 1, 2, 3, 4 }, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));
            Subject.Execute(new GameFileHashCommand(_game.Id));

            Subject.Handle(new GameFileDeletedEvent(_gameFile, DeleteMediaFileReason.Manual));

            AllStoredHashes().Should().BeEmpty();
        }

        private void GivenFile(byte[] content, DateTime lastWrite)
        {
            File.WriteAllBytes(_filePath, content);
            _gameFile.Size = content.Length;

            Mocker.GetMock<IDiskProvider>().Setup(x => x.FileExists(_filePath)).Returns(true);
            Mocker.GetMock<IDiskProvider>().Setup(x => x.GetFileSize(_filePath)).Returns(content.Length);
            Mocker.GetMock<IDiskProvider>().Setup(x => x.FileGetLastWrite(_filePath)).Returns(lastWrite);
            Mocker.GetMock<IDiskProvider>().Setup(x => x.OpenReadStream(_filePath)).Returns(() => File.OpenRead(_filePath));
        }

        private List<GameFileHash> AllStoredHashes()
        {
            return _repository.All().ToList();
        }

        private static string Sha1Of(byte[] content)
        {
            using var sha1 = SHA1.Create();
            return BitConverter.ToString(sha1.ComputeHash(content)).Replace("-", string.Empty);
        }
    }
}
