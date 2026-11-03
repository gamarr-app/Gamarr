using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class UpdateGameFileServiceFixture : CoreTest<UpdateGameFileService>
    {
        private Game _game;
        private GameFile _badFile;
        private GameFile _goodFile;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 5,
                Path = "/pc-games/Hades".AsOsAgnostic(),
                GameMetadata = new GameMetadata
                {
                    Title = "Hades",
                    PhysicalRelease = new DateTime(2020, 9, 17, 0, 0, 0, DateTimeKind.Utc)
                }
            };

            _badFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = "bad.exe", Game = _game };
            _goodFile = new GameFile { Id = 2, GameId = _game.Id, RelativePath = "good.exe", Game = _game };

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.FileDate)
                  .Returns(FileDateType.Release);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { _badFile, _goodFile });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileGetLastWrite(_badFile.GetPath(_game)))
                  .Throws(new IOException("I/O error"));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileGetLastWrite(_goodFile.GetPath(_game)))
                  .Returns(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void should_skip_unreadable_file_and_still_update_the_others_on_scan()
        {
            Subject.Invoking(s => s.Handle(new GameScannedEvent(_game, new List<string>()))).Should().NotThrow();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FileSetLastWriteTime(_goodFile.GetPath(_game), It.IsAny<DateTime>()), Times.Once());

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FileSetLastWriteTime(_badFile.GetPath(_game), It.IsAny<DateTime>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
