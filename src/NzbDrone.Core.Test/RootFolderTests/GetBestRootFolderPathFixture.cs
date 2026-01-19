using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.RootFolderTests
{
    [TestFixture]
    public class GetBestRootFolderPathFixture : CoreTest<RootFolderService>
    {
        private void GivenRootFolders(params string[] paths)
        {
            Mocker.GetMock<IRootFolderRepository>()
                .Setup(s => s.All())
                .Returns(paths.Select(p => new RootFolder { Path = p }));
        }

        [Test]
        public void should_return_root_folder_that_is_parent_path()
        {
            GivenRootFolders(@"C:\Test\Games".AsOsAgnostic(), @"D:\Test\Games".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Games\Game Title".AsOsAgnostic()).Should().Be(@"C:\Test\Games".AsOsAgnostic());
        }

        [Test]
        public void should_return_root_folder_that_is_grandparent_path()
        {
            GivenRootFolders(@"C:\Test\Games".AsOsAgnostic(), @"D:\Test\Games".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Games\M\Game Title".AsOsAgnostic()).Should().Be(@"C:\Test\Games".AsOsAgnostic());
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found()
        {
            var gamePath = @"T:\Test\Games\Game Title".AsOsAgnostic();

            GivenRootFolders(@"C:\Test\Games".AsOsAgnostic(), @"D:\Test\Games".AsOsAgnostic());
            Subject.GetBestRootFolderPath(gamePath).Should().Be(@"T:\Test\Games".AsOsAgnostic());
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found_for_posix_path()
        {
            WindowsOnly();

            var gamePath = "/mnt/games/Game Title";

            GivenRootFolders(@"C:\Test\Games".AsOsAgnostic(), @"D:\Test\Games".AsOsAgnostic());
            Subject.GetBestRootFolderPath(gamePath).Should().Be(@"/mnt/games");
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found_for_windows_path()
        {
            PosixOnly();

            var gamePath = @"T:\Test\Games\Game Title";

            GivenRootFolders(@"C:\Test\Games".AsOsAgnostic(), @"D:\Test\Games".AsOsAgnostic());
            Subject.GetBestRootFolderPath(gamePath).Should().Be(@"T:\Test\Games");
        }
    }
}
