using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class RecyclingBinCheckFixture : CoreTest<RecyclingBinCheck>
    {
        private string _rootFolderPath;
        private string _fallbackPath;

        [SetUp]
        public void Setup()
        {
            _rootFolderPath = @"C:\Games".AsOsAgnostic();
            _fallbackPath = Path.Combine(_rootFolderPath, ".recycle");

            // Echo the phrase and its tokens so the assertions can check the path is named.
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                  .Returns((string phrase, Dictionary<string, object> tokens) => $"{phrase}: {string.Join(", ", tokens.Values)}");

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.RecycleBinEnabled)
                  .Returns(true);

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder>());
        }

        private void GivenConfiguredRecycleBin(string path, bool writable)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.RecycleBin)
                  .Returns(path);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(path))
                  .Returns(writable);
        }

        private void GivenRootFolders(params string[] paths)
        {
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(paths.Select(p => new RootFolder { Path = p }).ToList());
        }

        private void GivenFolderExists(string path)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(path))
                  .Returns(true);
        }

        private void GivenFolderWritable(string path)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(path))
                  .Returns(true);
        }

        [Test]
        public void should_be_ok_when_configured_recycling_bin_is_writable()
        {
            GivenConfiguredRecycleBin(@"C:\Recycle".AsOsAgnostic(), true);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_error_when_configured_recycling_bin_is_not_writable()
        {
            var recycleBin = @"C:\Recycle".AsOsAgnostic();

            GivenConfiguredRecycleBin(recycleBin, false);

            Subject.Check().ShouldBeError(recycleBin, "#cannot-write-recycle-bin");
        }

        [Test]
        public void should_not_check_root_folders_when_recycling_bin_is_configured()
        {
            GivenConfiguredRecycleBin(@"C:\Recycle".AsOsAgnostic(), true);
            GivenRootFolders(_rootFolderPath);
            GivenFolderExists(_rootFolderPath);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_be_ok_when_no_recycling_bin_configured_and_root_folder_is_writable()
        {
            GivenRootFolders(_rootFolderPath);
            GivenFolderExists(_rootFolderPath);
            GivenFolderWritable(_rootFolderPath);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_error_when_existing_fallback_folder_is_not_writable()
        {
            GivenRootFolders(_rootFolderPath);
            GivenFolderExists(_rootFolderPath);
            GivenFolderExists(_fallbackPath);
            GivenFolderWritable(_rootFolderPath);

            Subject.Check().ShouldBeError(_fallbackPath, "#cannot-write-recycle-bin");
        }

        [Test]
        public void should_return_error_when_fallback_folder_cannot_be_created()
        {
            GivenRootFolders(_rootFolderPath);
            GivenFolderExists(_rootFolderPath);

            Subject.Check().ShouldBeError(_rootFolderPath, "#cannot-write-recycle-bin");
        }

        [Test]
        public void should_name_every_unwritable_root_folder()
        {
            var otherRootFolderPath = @"C:\MoreGames".AsOsAgnostic();

            GivenRootFolders(_rootFolderPath, otherRootFolderPath);
            GivenFolderExists(_rootFolderPath);
            GivenFolderExists(otherRootFolderPath);

            var result = Subject.Check();

            result.ShouldBeError(_rootFolderPath);
            result.Message.Should().Contain(otherRootFolderPath);
        }

        [Test]
        public void should_be_ok_when_root_folder_does_not_exist()
        {
            GivenRootFolders(_rootFolderPath);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_be_ok_when_recycling_bin_is_disabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.RecycleBinEnabled)
                  .Returns(false);

            GivenRootFolders(_rootFolderPath);
            GivenFolderExists(_rootFolderPath);

            Subject.Check().ShouldBeOk();
        }
    }
}
