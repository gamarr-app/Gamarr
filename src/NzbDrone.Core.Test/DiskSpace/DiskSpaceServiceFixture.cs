using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.DiskSpace;
using NzbDrone.Core.Games;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DiskSpace
{
    [TestFixture]
    public class DiskSpaceServiceFixture : CoreTest<DiskSpaceService>
    {
        private string _gamesFolder;
        private string _gamesFolder2;
        private string _rootFolder;

        [SetUp]
        public void SetUp()
        {
            _gamesFolder = @"G:\fasdlfsdf\games".AsOsAgnostic();
            _gamesFolder2 = @"G:\fasdlfsdf\games2".AsOsAgnostic();
            _rootFolder = @"G:\fasdlfsdf".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetMounts())
                  .Returns(new List<IMount>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetPathRoot(It.IsAny<string>()))
                  .Returns(@"G:\".AsOsAgnostic());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetAvailableSpace(It.IsAny<string>()))
                  .Returns(0);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetTotalSize(It.IsAny<string>()))
                  .Returns(0);

            GivenGames();
        }

        private void GivenGames(params Game[] games)
        {
            Mocker.GetMock<IGameService>()
                .Setup(v => v.AllGamePaths())
                .Returns(games.ToDictionary(x => x.Id, x => x.Path));
        }

        private void GivenRootFolder(string gamePath, string rootFolderPath)
        {
            Mocker.GetMock<IRootFolderService>()
                .Setup(v => v.GetBestRootFolderPath(gamePath, null))
                .Returns(rootFolderPath);
        }

        private void GivenExistingFolder(string folder)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FolderExists(folder))
                  .Returns(true);
        }

        [Test]
        public void should_check_diskspace_for_games_folders()
        {
            GivenGames(new Game { Path = _gamesFolder });
            GivenRootFolder(_gamesFolder, _rootFolder);
            GivenExistingFolder(_rootFolder);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().NotBeEmpty();
        }

        [Test]
        public void should_check_diskspace_for_same_root_folder_only_once()
        {
            GivenGames(new Game { Id = 1, Path = _gamesFolder }, new Game { Id = 2, Path = _gamesFolder2 });
            GivenRootFolder(_gamesFolder, _rootFolder);
            GivenRootFolder(_gamesFolder, _rootFolder);
            GivenExistingFolder(_rootFolder);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().HaveCount(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetAvailableSpace(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_check_diskspace_for_missing_game_root_folders()
        {
            GivenGames(new Game { Path = _gamesFolder });
            GivenRootFolder(_gamesFolder, _rootFolder);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().BeEmpty();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetAvailableSpace(It.IsAny<string>()), Times.Never());
        }

        [TestCase("/boot")]
        [TestCase("/var/lib/rancher")]
        [TestCase("/var/lib/rancher/volumes")]
        [TestCase("/var/lib/kubelet")]
        [TestCase("/var/lib/docker")]
        [TestCase("/some/place/docker/aufs")]
        [TestCase("/etc/network")]
        [TestCase("/Volumes/.timemachine/ABC123456-A1BC-12A3B45678C9/2025-05-13-181401.backup")]
        public void should_not_check_diskspace_for_irrelevant_mounts(string path)
        {
            var mount = new Mock<IMount>();
            mount.SetupGet(v => v.RootDirectory).Returns(path);
            mount.SetupGet(v => v.DriveType).Returns(System.IO.DriveType.Fixed);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetMounts())
                  .Returns(new List<IMount> { mount.Object });

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().BeEmpty();
        }
    }
}
