using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.RootFolders
{
    [TestFixture]
    public class RootFolderServiceFixture : CoreTest<RootFolderService>
    {
        private RootFolder _rootFolder;

        [SetUp]
        public void Setup()
        {
            _rootFolder = new RootFolder
            {
                Id = 1,
                Path = "/games"
            };

            Mocker.GetMock<IRootFolderRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IGameRepository>()
                  .Setup(s => s.AllGamePaths())
                  .Returns(new Dictionary<int, string>());

            Mocker.GetMock<INamingConfigService>()
                  .Setup(s => s.GetConfig())
                  .Returns(new NamingConfig { GameFolderFormat = "{Game Title} ({Release Year})" });

            Mocker.GetMock<ICacheManager>()
                  .Setup(s => s.GetCache<string>(It.IsAny<Type>()))
                  .Returns(new Cached<string>());
        }

        [Test]
        public void should_return_all_root_folders()
        {
            Mocker.GetMock<IRootFolderRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder> { _rootFolder });

            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_add_root_folder()
        {
            Mocker.GetMock<IRootFolderRepository>()
                  .Setup(s => s.Insert(It.IsAny<RootFolder>()))
                  .Returns(_rootFolder);

            var result = Subject.Add(_rootFolder);

            result.Should().NotBeNull();
            Mocker.GetMock<IRootFolderRepository>()
                  .Verify(s => s.Insert(_rootFolder), Times.Once());
        }

        [Test]
        public void should_throw_if_path_is_null_or_empty()
        {
            _rootFolder.Path = null;

            Assert.Throws<ArgumentException>(() => Subject.Add(_rootFolder));
        }

        [Test]
        public void should_throw_if_path_is_not_rooted()
        {
            _rootFolder.Path = "relative/path";

            Assert.Throws<ArgumentException>(() => Subject.Add(_rootFolder));
        }

        [Test]
        public void should_throw_if_folder_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_rootFolder.Path))
                  .Returns(false);

            Assert.Throws<DirectoryNotFoundException>(() => Subject.Add(_rootFolder));
        }

        [Test]
        public void should_throw_if_folder_already_exists()
        {
            Mocker.GetMock<IRootFolderRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder> { _rootFolder });

            Assert.Throws<InvalidOperationException>(() => Subject.Add(_rootFolder));
        }

        [Test]
        public void should_throw_if_folder_not_writable()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(_rootFolder.Path))
                  .Returns(false);

            Assert.Throws<UnauthorizedAccessException>(() => Subject.Add(_rootFolder));
        }

        [Test]
        public void should_remove_root_folder()
        {
            Subject.Remove(1);

            Mocker.GetMock<IRootFolderRepository>()
                  .Verify(s => s.Delete(1), Times.Once());
        }

        [Test]
        public void should_get_root_folder_by_id()
        {
            Mocker.GetMock<IRootFolderRepository>()
                  .Setup(s => s.Get(1))
                  .Returns(_rootFolder);

            var result = Subject.Get(1, true);

            result.Should().Be(_rootFolder);
        }

        [Test]
        public void should_get_best_root_folder_path()
        {
            Mocker.GetMock<IRootFolderRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder>
                  {
                      new RootFolder { Path = "/games" },
                      new RootFolder { Path = "/games/steam" }
                  });

            var result = Subject.GetBestRootFolderPath("/games/steam/Half-Life");

            result.Should().Be("/games/steam");
        }
    }
}
