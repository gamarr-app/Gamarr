using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class RejectedImportServiceFixture : CoreTest<RejectedImportService>
    {
        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            _trackedDownload = new TrackedDownload
            {
                DownloadItem = new DownloadClientItem
                {
                    Title = "Test.Game-GROUP"
                },
                RemoteGame = new RemoteGame
                {
                    Release = new ReleaseInfo
                    {
                        IndexerId = 1
                    }
                },
                State = TrackedDownloadState.Importing
            };
        }

        private ImportResult CreateRejectedResult(ImportRejectionReason reason)
        {
            var rejection = new ImportRejection(reason, "Test rejection message");
            var decision = new ImportDecision(new LocalGame(), rejection);
            return new ImportResult(decision, "Error message");
        }

        private ImportResult CreateImportedResult()
        {
            var decision = new ImportDecision(new LocalGame());
            return new ImportResult(decision);
        }

        [Test]
        public void should_return_false_for_non_rejected()
        {
            var importResult = CreateImportedResult();

            Subject.Process(_trackedDownload, importResult).Should().BeFalse();
        }

        [Test]
        public void should_return_false_for_null_release()
        {
            _trackedDownload.RemoteGame.Release = null;

            var importResult = CreateRejectedResult(ImportRejectionReason.DangerousFile);

            Subject.Process(_trackedDownload, importResult).Should().BeFalse();
        }

        [Test]
        public void should_fail_for_dangerous_file()
        {
            var indexerSettings = new CachedIndexerSettings
            {
                FailDownloads = new HashSet<FailDownloads> { NzbDrone.Core.Indexers.FailDownloads.PotentiallyDangerous }
            };

            Mocker.GetMock<ICachedIndexerSettingsProvider>()
                .Setup(s => s.GetSettings(1))
                .Returns(indexerSettings);

            var importResult = CreateRejectedResult(ImportRejectionReason.DangerousFile);

            Subject.Process(_trackedDownload, importResult);

            _trackedDownload.State.Should().Be(TrackedDownloadState.FailedPending);
        }

        [Test]
        public void should_fail_for_executable_file()
        {
            var indexerSettings = new CachedIndexerSettings
            {
                FailDownloads = new HashSet<FailDownloads> { NzbDrone.Core.Indexers.FailDownloads.Executables }
            };

            Mocker.GetMock<ICachedIndexerSettingsProvider>()
                .Setup(s => s.GetSettings(1))
                .Returns(indexerSettings);

            var importResult = CreateRejectedResult(ImportRejectionReason.ExecutableFile);

            Subject.Process(_trackedDownload, importResult);

            _trackedDownload.State.Should().Be(TrackedDownloadState.FailedPending);
        }

        [Test]
        public void should_block_suspicious_structure()
        {
            var indexerSettings = new CachedIndexerSettings
            {
                FailDownloads = new HashSet<FailDownloads>()
            };

            Mocker.GetMock<ICachedIndexerSettingsProvider>()
                .Setup(s => s.GetSettings(1))
                .Returns(indexerSettings);

            var importResult = CreateRejectedResult(ImportRejectionReason.SuspiciousReleaseStructure);

            Subject.Process(_trackedDownload, importResult);

            _trackedDownload.State.Should().Be(TrackedDownloadState.ImportBlocked);
        }

        [Test]
        public void should_warn_for_other_rejections()
        {
            var indexerSettings = new CachedIndexerSettings
            {
                FailDownloads = new HashSet<FailDownloads>()
            };

            Mocker.GetMock<ICachedIndexerSettingsProvider>()
                .Setup(s => s.GetSettings(1))
                .Returns(indexerSettings);

            var importResult = CreateRejectedResult(ImportRejectionReason.UnknownGame);

            var result = Subject.Process(_trackedDownload, importResult);

            result.Should().BeTrue();
            _trackedDownload.State.Should().NotBe(TrackedDownloadState.FailedPending);
            _trackedDownload.State.Should().NotBe(TrackedDownloadState.ImportBlocked);
        }
    }
}
