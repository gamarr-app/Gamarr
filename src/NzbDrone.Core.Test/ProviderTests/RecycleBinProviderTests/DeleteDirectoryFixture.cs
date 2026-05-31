using System;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.RecycleBinProviderTests
{
    [TestFixture]

    public class DeleteDirectoryFixture : CoreTest
    {
        private readonly string _sourcePath = @"C:\Test\Games\Elden Ring".AsOsAgnostic();
        private readonly string _binPath = @"C:\Test\Recycle Bin".AsOsAgnostic();

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(_sourcePath)).Returns(true);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(_binPath)).Returns(true);
        }

        private void WithRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBin).Returns(_binPath);
        }

        private void WithoutRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBin).Returns(string.Empty);
        }

        [Test]
        public void should_use_delete_when_recycleBin_is_not_configured()
        {
            WithoutRecycleBin();

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(_sourcePath);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(_sourcePath, true), Times.Once());
        }

        [Test]
        public void should_use_move_when_recycleBin_is_configured()
        {
            WithRecycleBin();

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(_sourcePath);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(_sourcePath, @"C:\Test\Recycle Bin\Elden Ring".AsOsAgnostic(), TransferMode.Move), Times.Once());
        }

        [Test]
        public void should_call_directorySetLastWriteTime()
        {
            WithRecycleBin();

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(_sourcePath);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderSetLastWriteTime(@"C:\Test\Recycle Bin\Elden Ring".AsOsAgnostic(), It.IsAny<DateTime>()), Times.Once());
        }

        [Test]
        public void should_call_fileSetLastWriteTime_for_each_file()
        {
            WindowsOnly();
            WithRecycleBin();

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(@"C:\Test\Recycle Bin\Elden Ring".AsOsAgnostic(), true))
                                           .Returns(new[] { "File1", "File2", "File3" });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(_sourcePath);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Exactly(3));
        }

        [Test]
        public void should_skip_when_source_folder_does_not_exist()
        {
            WithRecycleBin();
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(_sourcePath)).Returns(false);

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(_sourcePath);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>()), Times.Never());
        }

        [Test]
        public void should_create_recycle_bin_folder_when_missing()
        {
            WithRecycleBin();
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(_binPath)).Returns(false);

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(_sourcePath);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.CreateFolder(_binPath), Times.Once());
            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(_sourcePath, @"C:\Test\Recycle Bin\Elden Ring".AsOsAgnostic(), TransferMode.Move), Times.Once());
        }
    }
}
