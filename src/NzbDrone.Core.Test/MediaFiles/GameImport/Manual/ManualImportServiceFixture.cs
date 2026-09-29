using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.MediaFiles.GameImport.Manual;
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
