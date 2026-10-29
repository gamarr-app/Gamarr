using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras.Metadata
{
    [TestFixture]
    public class MetadataServiceFixture : CoreTest<NzbDrone.Core.Extras.Metadata.MetadataService>
    {
        private Game _game;
        private GameFile _unwritableGameFile;
        private GameFile _writableGameFile;
        private Mock<NzbDrone.Core.Extras.Metadata.IMetadata> _consumer;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                  .With(g => g.Path = @"C:\Test\Games\Game Title".AsOsAgnostic())
                                  .Build();

            _unwritableGameFile = Builder<GameFile>.CreateNew()
                                      .With(f => f.Id = 1)
                                      .With(f => f.RelativePath = "Unwritable Game.mkv".AsOsAgnostic())
                                      .Build();

            _writableGameFile = Builder<GameFile>.CreateNew()
                                      .With(f => f.Id = 2)
                                      .With(f => f.RelativePath = "Writable Game.mkv".AsOsAgnostic())
                                      .Build();

            _consumer = new Mock<NzbDrone.Core.Extras.Metadata.IMetadata>();
            _consumer.Setup(c => c.GameImages(It.IsAny<Game>()))
                     .Returns(new List<NzbDrone.Core.Extras.Metadata.Files.ImageFileResult>());

            _consumer.Setup(c => c.GameMetadata(_game, _unwritableGameFile))
                     .Returns(new MetadataFileResult("unwritable.nfo", "contents"));

            _consumer.Setup(c => c.GameMetadata(_game, _writableGameFile))
                     .Returns(new MetadataFileResult("writable.nfo", "contents"));

            Mocker.GetMock<NzbDrone.Core.Extras.Metadata.IMetadataFactory>()
                  .Setup(s => s.Enabled())
                  .Returns(new List<NzbDrone.Core.Extras.Metadata.IMetadata> { _consumer.Object });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_game.Path))
                  .Returns(true);

            Mocker.GetMock<IMetadataFileService>()
                  .Setup(s => s.GetFilesByGame(_game.Id))
                  .Returns(new List<MetadataFile>());

            // The read-only library mount from Sentry issue DOTNET-ASPNETCORE-33: one
            // file throws IOException out of WriteAllText while a sibling is fine.
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.WriteAllText(Path.Combine(_game.Path, "unwritable.nfo"), It.IsAny<string>()))
                  .Throws(new IOException("Read-only file system"));
        }

        [Test]
        public void should_continue_writing_remaining_metadata_files_when_one_file_is_unwritable()
        {
            Subject.CreateAfterGameScan(_game, new List<GameFile> { _unwritableGameFile, _writableGameFile });

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.WriteAllText(Path.Combine(_game.Path, "writable.nfo"), It.IsAny<string>()), Times.Once());

            Mocker.GetMock<IMetadataFileService>()
                  .Verify(v => v.Upsert(It.Is<List<MetadataFile>>(l => l.Count == 1 && l[0].RelativePath == "writable.nfo")), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
