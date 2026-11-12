using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Games;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation;
using NzbDrone.Core.MediaFiles.GameImport.Manual;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Manual
{
    [TestFixture]
    public class ManualImportServiceFixture : CoreTest<ManualImportService>
    {
        private string _outputPath;
        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            _outputPath = @"C:\DropFolder\MyDownload".AsOsAgnostic();

            var downloadItem = Builder<DownloadClientItem>.CreateNew()
                .With(d => d.OutputPath = new OsPath(_outputPath))
                .Build();

            // ImportItem is only populated once CompletedDownloadService has tried to
            // import the download, which doesn't always happen before a manual import runs.
            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                .With(t => t.ImportItem = null)
                .With(t => t.DownloadItem = downloadItem)
                .Build();

            Mocker.GetMock<ITrackedDownloadService>()
                  .Setup(s => s.Find(It.IsAny<string>()))
                  .Returns(_trackedDownload);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(false);
        }

        [Test]
        public void should_skip_vanished_file_and_continue_with_the_rest()
        {
            var gone = @"C:\DropFolder\Gone\Chinese.doi".AsOsAgnostic();
            var present = @"C:\DropFolder\Present".AsOsAgnostic();
            var game = new Game { Id = 1, Path = @"C:\Games\Game".AsOsAgnostic() };

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(It.IsAny<int>()))
                  .Returns(game);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(present))
                  .Returns(true);

            Mocker.GetMock<IAggregationService>()
                  .Setup(s => s.Augment(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                  .Returns<LocalGame, DownloadClientItem>((l, d) => l);

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(new List<ImportResult>());

            var command = new ManualImportCommand
            {
                Files = new List<ManualImportFile>
                {
                    new ManualImportFile { Path = gone, GameId = 1, Quality = new QualityModel(Quality.Unknown), Languages = new List<Language> { Language.Unknown } },
                    new ManualImportFile { Path = present, GameId = 1, Quality = new QualityModel(Quality.Unknown), Languages = new List<Language> { Language.Unknown } }
                }
            };

            FluentActions.Invoking(() => Subject.Execute(command)).Should().NotThrow();

            Mocker.GetMock<IAggregationService>()
                  .Verify(v => v.Augment(It.Is<LocalGame>(l => l.Path == gone), It.IsAny<DownloadClientItem>()), Times.Never());

            Mocker.GetMock<IImportApprovedGame>()
                  .Verify(v => v.Import(It.Is<List<ImportDecision>>(d => d.Single().LocalGame.Path == present), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_throw_when_tracked_download_import_item_is_null()
        {
            FluentActions.Invoking(() => Subject.GetMediaFiles(null, "downloadId", null, false))
                          .Should().NotThrow();
        }

        [Test]
        public void should_fall_back_to_download_item_output_path_when_import_item_is_null()
        {
            var result = Subject.GetMediaFiles(null, "downloadId", null, false);

            result.Should().BeEmpty();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FolderExists(_outputPath), Times.Once());
        }
    }
}
