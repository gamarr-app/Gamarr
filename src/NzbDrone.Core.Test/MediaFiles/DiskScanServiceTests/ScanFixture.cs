using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.DiskScanServiceTests
{
    [TestFixture]
    public class ScanFixture : CoreTest<DiskScanService>
    {
        private Game _game;
        private string _rootFolder;
        private string _otherGameFolder;

        [SetUp]
        public void Setup()
        {
            _rootFolder = @"C:\Test\Games".AsOsAgnostic();
            _otherGameFolder = @"C:\Test\Games\OtherGame".AsOsAgnostic();
            var gameFolder = @"C:\Test\Games\Game".AsOsAgnostic();

            _game = Builder<Game>.CreateNew()
                .With(s => s.Path = gameFolder)
                                     .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(It.IsAny<string>()))
                  .Returns((string path) => Directory.GetParent(path).FullName);

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), null))
                  .Returns(_rootFolder);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByGame(It.IsAny<int>()))
                  .Returns(new List<GameFile>());
        }

        private void GivenRootFolder(params string[] subfolders)
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_rootFolder))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(_rootFolder))
                  .Returns(subfolders);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderEmpty(_rootFolder))
                  .Returns(subfolders.Empty());

            foreach (var folder in subfolders)
            {
                Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(folder))
                  .Returns(true);
            }
        }

        private void GivenGameFolder()
        {
            GivenRootFolder(_game.Path);
        }

        private void GivenFiles(IEnumerable<string> files)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFiles(It.IsAny<string>(), true))
                  .Returns(files.ToArray());
        }

        [Test]
        public void should_not_scan_if_game_root_folder_does_not_exist()
        {
            Subject.Scan(_game);

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetFiles(_game.Path, true), Times.Never());

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                .Verify(v => v.Clean(It.IsAny<Game>(), It.IsAny<List<string>>()), Times.Never());
        }

        [Test]
        public void should_not_scan_if_game_root_folder_is_empty()
        {
            GivenRootFolder();

            Subject.Scan(_game);

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetFiles(_game.Path, true), Times.Never());

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Game>(), It.IsAny<List<string>>()), Times.Never());

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.IsAny<List<string>>(), _game, false), Times.Never());
        }

        [Test]
        public void should_create_if_game_folder_does_not_exist_but_create_folder_enabled()
        {
            GivenRootFolder(_otherGameFolder);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CreateEmptyGameFolders)
                  .Returns(true);

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Once());
        }

        [Test]
        public void should_not_scan_extras_subfolder()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "EXTRAS", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Extras", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "EXTRAs", "file3.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "ExTrAs", "file4.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetFiles(It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_not_scan_various_extras_subfolders()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "Behind the Scenes", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Deleted Scenes", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Featurettes", "file3.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Interviews", "file4.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Sample", "file5.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Samples", "file6.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Scenes", "file7.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Shorts", "file8.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Trailers", "file9.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Other", "file9.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "The Count of Monte Cristo (2002) (1080p BluRay x265 10bit Tigole).iso").AsOsAgnostic(),
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_not_scan_featurettes_subfolders()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "Featurettes", "An Epic Reborn.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Featurettes", "Deleted & Alternate Scenes.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Featurettes", "En Garde - Multi-Angle Dailies.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Featurettes", "Layer-By-Layer - Sound Design - Multiple Audio.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "The Count of Monte Cristo (2002) (1080p BluRay x265 10bit Tigole).iso").AsOsAgnostic(),
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_not_create_if_game_folder_does_not_exist_and_create_folder_disabled()
        {
            GivenRootFolder(_otherGameFolder);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CreateEmptyGameFolders)
                  .Returns(false);

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.CreateFolder(_game.Path), Times.Never());
        }

        [Test]
        public void should_clean_but_not_import_if_game_folder_does_not_exist()
        {
            GivenRootFolder(_otherGameFolder);

            Subject.Scan(_game);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FolderExists(_game.Path), Times.Once());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Game>(), It.IsAny<List<string>>()), Times.Once());

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.IsAny<List<string>>(), _game, false), Times.Never());
        }

        [Test]
        public void should_not_scan_AppleDouble_subfolder()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, ".AppleDouble", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, ".appledouble", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_scan_extras_game_and_subfolders()
        {
            _game.Path = @"C:\Test\Games\Extras".AsOsAgnostic();

            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "Extras", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, ".AppleDouble", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e02.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 2", "s02e01.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 2", "s02e02.iso").AsOsAgnostic(),
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 4), _game, false), Times.Once());
        }

        [Test]
        public void should_not_scan_subfolders_that_start_with_period()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, ".@__thumb", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, ".@__THUMB", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, ".hidden", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_not_scan_subfolder_of_season_folder_that_starts_with_a_period()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "Season 1", ".@__thumb", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", ".@__THUMB", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", ".hidden", "file2.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", ".AppleDouble", "s01e01.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_not_scan_Synology_eaDir()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "@eaDir", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_not_scan_thumb_folder()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, ".@__thumb", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_scan_dotHack_folder()
        {
            _game.Path = @"C:\Test\TV\.hack".AsOsAgnostic();

            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "Season 1", "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Season 1", "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 2), _game, false), Times.Once());
        }

        [Test]
        public void should_find_files_at_root_of_game_folder()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "file1.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "s01e01.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 2), _game, false), Times.Once());
        }

        [Test]
        public void should_exclude_inline_extra_files()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "Avatar (2009).iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "Deleted Scenes-deleted.iso").AsOsAgnostic(),
                           Path.Combine(_game.Path, "The World of Pandora-other.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_exclude_osx_metadata_files()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_game.Path, "._24 The Status Quo Combustion.mp4").AsOsAgnostic(),
                           Path.Combine(_game.Path, "24 The Status Quo Combustion.iso").AsOsAgnostic()
                       });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());
        }

        [Test]
        public void should_not_scan_excluded_files()
        {
            GivenGameFolder();

            GivenFiles(new List<string>
            {
                Path.Combine(_game.Path, ".DS_Store").AsOsAgnostic(),
                Path.Combine(_game.Path, ".unmanic").AsOsAgnostic(),
                Path.Combine(_game.Path, ".unmanic.part").AsOsAgnostic(),
                Path.Combine(_game.Path, "24 The Status Quo Combustion.iso").AsOsAgnostic()
            });

            Subject.Scan(_game);

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<string>>(l => l.Count == 1), _game, false), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.Is<GameScannedEvent>(c => c.Game != null && c.PossibleExtraFiles.Count == 0)), Times.Once());
        }
    }
}
