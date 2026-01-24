using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport
{
    [TestFixture]

    // TODO: Update all of this for games.
    public class ImportApprovedGamesFixture : CoreTest<ImportApprovedGame>
    {
        private List<ImportDecision> _rejectedDecisions;
        private List<ImportDecision> _approvedDecisions;

        private DownloadClientItem _downloadClientItem;

        [SetUp]
        public void Setup()
        {
            _rejectedDecisions = new List<ImportDecision>();
            _approvedDecisions = new List<ImportDecision>();

            var outputPath = @"C:\Test\Unsorted\Games\Portal.2".AsOsAgnostic();

            var game = Builder<Game>.CreateNew()
                .With(e => e.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                .With(s => s.Path = @"C:\Test\Games\Portal 2".AsOsAgnostic())
                .Build();

            _rejectedDecisions.Add(new ImportDecision(new LocalGame(), new ImportRejection(ImportRejectionReason.Unknown, "Rejected!")));
            _rejectedDecisions.Add(new ImportDecision(new LocalGame(), new ImportRejection(ImportRejectionReason.Unknown, "Rejected!")));
            _rejectedDecisions.Add(new ImportDecision(new LocalGame(), new ImportRejection(ImportRejectionReason.Unknown, "Rejected!")));

            _approvedDecisions.Add(new ImportDecision(
                                       new LocalGame
                                       {
                                           Game = game,
                                           Path = Path.Combine(game.Path, "Portal 2 Setup.exe"),
                                           Quality = new QualityModel(),
                                           ReleaseGroup = "GAMARR"
                                       }));

            Mocker.GetMock<IUpgradeMediaFiles>()
                  .Setup(s => s.UpgradeGameFile(It.IsAny<GameFile>(), It.IsAny<LocalGame>(), It.IsAny<bool>()))
                  .Returns(new GameFileMoveResult());

            Mocker.GetMock<IHistoryService>()
                .Setup(x => x.FindByDownloadId(It.IsAny<string>()))
                .Returns(new List<GameHistory>());

            _downloadClientItem = Builder<DownloadClientItem>.CreateNew()
                .With(d => d.OutputPath = new OsPath(outputPath))
                .Build();
        }

        private void GivenNewDownload()
        {
            _approvedDecisions.ForEach(a => a.LocalGame.Path = Path.Combine(_downloadClientItem.OutputPath.ToString(), Path.GetFileName(a.LocalGame.Path)));
        }

        private void GivenExistingFileOnDisk()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesWithRelativePath(It.IsAny<int>(), It.IsAny<string>()))
                  .Returns(new List<GameFile>());
        }

        [Test]
        public void should_not_import_any_if_there_are_no_approved_decisions()
        {
            Subject.Import(_rejectedDecisions, false).Where(i => i.Result == ImportResultType.Imported).Should().BeEmpty();

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.IsAny<GameFile>()), Times.Never());
        }

        [Test]
        public void should_import_each_approved()
        {
            GivenExistingFileOnDisk();

            Subject.Import(_approvedDecisions, false).Should().HaveCount(1);
        }

        [Test]
        public void should_only_import_approved()
        {
            GivenExistingFileOnDisk();

            var all = new List<ImportDecision>();
            all.AddRange(_rejectedDecisions);
            all.AddRange(_approvedDecisions);

            var result = Subject.Import(all, false);

            result.Should().HaveCount(all.Count);
            result.Where(i => i.Result == ImportResultType.Imported).Should().HaveCount(_approvedDecisions.Count);
        }

        [Test]
        public void should_only_import_each_game_once()
        {
            GivenExistingFileOnDisk();

            var all = new List<ImportDecision>();
            all.AddRange(_approvedDecisions);
            all.Add(new ImportDecision(_approvedDecisions.First().LocalGame));

            var result = Subject.Import(all, false);

            result.Where(i => i.Result == ImportResultType.Imported).Should().HaveCount(_approvedDecisions.Count);
        }

        [Test]
        public void should_move_new_downloads()
        {
            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true);

            Mocker.GetMock<IUpgradeMediaFiles>()
                  .Verify(v => v.UpgradeGameFile(It.IsAny<GameFile>(), _approvedDecisions.First().LocalGame, false),
                          Times.Once());
        }

        [Test]
        public void should_publish_GameImportedEvent_for_new_downloads()
        {
            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true);

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.IsAny<GameFileImportedEvent>()), Times.Once());
        }

        [Test]
        public void should_not_move_existing_files()
        {
            GivenExistingFileOnDisk();

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, false);

            Mocker.GetMock<IUpgradeMediaFiles>()
                  .Verify(v => v.UpgradeGameFile(It.IsAny<GameFile>(), _approvedDecisions.First().LocalGame, false),
                          Times.Never());
        }

        [Test]
        public void should_import_larger_files_first()
        {
            GivenExistingFileOnDisk();

            var fileDecision = _approvedDecisions.First();
            fileDecision.LocalGame.Size = 1.Gigabytes();

            var sampleDecision = new ImportDecision(
                new LocalGame
                {
                    Game = fileDecision.LocalGame.Game,
                    Path = @"C:\Test\Games\Portal 2\Portal 2 Patch.exe".AsOsAgnostic(),
                    Quality = new QualityModel(),
                    Size = 80.Megabytes()
                });

            var all = new List<ImportDecision>();
            all.Add(fileDecision);
            all.Add(sampleDecision);

            var results = Subject.Import(all, false);

            results.Should().HaveCount(all.Count);
            results.Should().ContainSingle(d => d.Result == ImportResultType.Imported);
            results.Should().ContainSingle(d => d.Result == ImportResultType.Imported && d.ImportDecision.LocalGame.Size == fileDecision.LocalGame.Size);
        }

        [Test]
        public void should_copy_when_cannot_move_files_downloads()
        {
            GivenNewDownload();
            _downloadClientItem.Title = "Portal.2.v1.0";
            _downloadClientItem.CanMoveFiles = false;

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, _downloadClientItem);

            Mocker.GetMock<IUpgradeMediaFiles>()
                  .Verify(v => v.UpgradeGameFile(It.IsAny<GameFile>(), _approvedDecisions.First().LocalGame, true), Times.Once());
        }

        [Test]
        public void should_use_override_importmode()
        {
            GivenNewDownload();
            _downloadClientItem.Title = "Portal.2.v1.0";
            _downloadClientItem.CanMoveFiles = false;

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, _downloadClientItem, ImportMode.Move);

            Mocker.GetMock<IUpgradeMediaFiles>()
                  .Verify(v => v.UpgradeGameFile(It.IsAny<GameFile>(), _approvedDecisions.First().LocalGame, false), Times.Once());
        }

        [Test]
        public void should_use_file_name_only_for_download_client_item_without_a_job_folder()
        {
            var fileName = "Game.Title.v1.0.x264-Gamarr.zip";
            var path = Path.Combine(@"C:\Test\Unsorted\Games\".AsOsAgnostic(), fileName);

            _downloadClientItem.OutputPath = new OsPath(path);
            _approvedDecisions.First().LocalGame.Path = path;

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, _downloadClientItem);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == fileName)));
        }

        [Test]
        public void should_use_folder_and_file_name_only_for_download_client_item_with_a_job_folder()
        {
            var name = "Game.Title.v1.0.x264-Gamarr";
            var outputPath = Path.Combine(@"C:\Test\Unsorted\Games\".AsOsAgnostic(), name);

            _downloadClientItem.OutputPath = new OsPath(outputPath);
            _approvedDecisions.First().LocalGame.Path = Path.Combine(outputPath, name + ".zip");

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, _downloadClientItem);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == $"{name}\\{name}.zip".AsOsAgnostic())));
        }

        [Test]
        public void should_include_intermediate_folders_for_download_client_item_with_a_job_folder()
        {
            var name = "Game.Title.v1.0.x264-Gamarr";
            var outputPath = Path.Combine(@"C:\Test\Unsorted\Games\".AsOsAgnostic(), name);

            _downloadClientItem.OutputPath = new OsPath(outputPath);
            _approvedDecisions.First().LocalGame.Path = Path.Combine(outputPath, "subfolder", name + ".zip");

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, _downloadClientItem);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == $"{name}\\subfolder\\{name}.zip".AsOsAgnostic())));
        }

        [Test]
        public void should_use_folder_info_original_title_to_find_relative_path()
        {
            var name = "Transformers.2007.720p.BluRay.x264-Gamarr";
            var outputPath = Path.Combine(@"C:\Test\Unsorted\games\".AsOsAgnostic(), name);
            var localGame = _approvedDecisions.First().LocalGame;

            localGame.FolderGameInfo = new ParsedGameInfo { OriginalTitle = name };
            localGame.Path = Path.Combine(outputPath, "subfolder", name + ".mkv");

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, null);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == $"{name}\\subfolder\\{name}.mkv".AsOsAgnostic())));
        }

        [Test]
        public void should_get_relative_path_when_there_is_no_grandparent_windows()
        {
            WindowsOnly();

            var name = "Transformers.2007.720p.BluRay.x264-Gamarr";
            var outputPath = @"C:\".AsOsAgnostic();
            var localGame = _approvedDecisions.First().LocalGame;
            localGame.FolderGameInfo = new ParsedGameInfo { ReleaseTitle = name };
            localGame.Path = Path.Combine(outputPath, name + ".mkv");

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, null);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == $"{name}.mkv".AsOsAgnostic())));
        }

        [Test]
        public void should_get_relative_path_when_there_is_no_grandparent_for_UNC_path()
        {
            WindowsOnly();

            var name = "Transformers.2007.720p.BluRay.x264-Gamarr";
            var outputPath = @"\\server\share";
            var localGame = _approvedDecisions.First().LocalGame;

            localGame.FolderGameInfo = new ParsedGameInfo { ReleaseTitle = name };
            localGame.Path = Path.Combine(outputPath, name + ".mkv");

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, null);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == $"{name}.mkv")));
        }

        [Test]
        public void should_use_folder_info_original_title_to_find_relative_path_when_file_is_not_in_download_client_item_output_directory()
        {
            var name = "Transformers.2007.720p.BluRay.x264-Gamarr";
            var outputPath = Path.Combine(@"C:\Test\Unsorted\game\".AsOsAgnostic(), name);
            var localGame = _approvedDecisions.First().LocalGame;

            _downloadClientItem.OutputPath = new OsPath(Path.Combine(@"C:\Test\Unsorted\game-Other\".AsOsAgnostic(), name));
            localGame.FolderGameInfo = new ParsedGameInfo { ReleaseTitle = name };
            localGame.Path = Path.Combine(outputPath, "subfolder", name + ".mkv");

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, _downloadClientItem);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == $"subfolder\\{name}.mkv".AsOsAgnostic())));
        }

        [Test]
        public void should_use_folder_info_original_title_to_find_relative_path_when_download_client_item_has_an_empty_output_path()
        {
            var name = "Transformers.2007.720p.BluRay.x264-Gamarr";
            var outputPath = Path.Combine(@"C:\Test\Unsorted\games\".AsOsAgnostic(), name);
            var localGame = _approvedDecisions.First().LocalGame;

            _downloadClientItem.OutputPath = new OsPath(string.Empty);
            localGame.FolderGameInfo = new ParsedGameInfo { ReleaseTitle = name };
            localGame.Path = Path.Combine(outputPath, "subfolder", name + ".mkv");

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true, _downloadClientItem);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Add(It.Is<GameFile>(c => c.OriginalFilePath == $"subfolder\\{name}.mkv".AsOsAgnostic())));
        }

        [Test]
        public void should_include_scene_name_with_new_downloads()
        {
            var firstDecision = _approvedDecisions.First();
            firstDecision.LocalGame.SceneName = "Game.Title.2022.dvdrip-DRONE";

            Subject.Import(new List<ImportDecision> { _approvedDecisions.First() }, true);

            Mocker.GetMock<IUpgradeMediaFiles>()
                  .Verify(v => v.UpgradeGameFile(It.Is<GameFile>(e => e.SceneName == firstDecision.LocalGame.SceneName), _approvedDecisions.First().LocalGame, false),
                      Times.Once());
        }
    }
}
