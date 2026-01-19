using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class GameFolderPathBuilderFixture : CoreTest<GamePathBuilder>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Title = "Game Title")
                                     .With(s => s.Path = @"C:\Test\Games\Game.Title".AsOsAgnostic())
                                     .With(s => s.RootFolderPath = null)
                                     .Build();
        }

        public void GivenGameFolderName(string name)
        {
            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetGameFolder(_game, null))
                  .Returns(name);
        }

        public void GivenExistingRootFolder(string rootFolder)
        {
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), null))
                  .Returns(rootFolder);
        }

        [Test]
        public void should_create_new_game_path()
        {
            var rootFolder = @"C:\Test\Games2".AsOsAgnostic();

            GivenGameFolderName(_game.Title);
            _game.RootFolderPath = rootFolder;

            Subject.BuildPath(_game, false).Should().Be(Path.Combine(rootFolder, _game.Title));
        }

        [Test]
        public void should_reuse_existing_relative_folder_name()
        {
            var folderName = Path.GetFileName(_game.Path);
            var rootFolder = @"C:\Test\Games2".AsOsAgnostic();

            GivenExistingRootFolder(Path.GetDirectoryName(_game.Path));
            GivenGameFolderName(_game.Title);
            _game.RootFolderPath = rootFolder;

            Subject.BuildPath(_game, true).Should().Be(Path.Combine(rootFolder, folderName));
        }

        [Test]
        public void should_reuse_existing_relative_folder_structure()
        {
            var existingRootFolder = @"C:\Test\Games".AsOsAgnostic();
            var existingRelativePath = @"M\Game.Title";
            var rootFolder = @"C:\Test\Games2".AsOsAgnostic();

            GivenExistingRootFolder(existingRootFolder);
            GivenGameFolderName(_game.Title);
            _game.RootFolderPath = rootFolder;
            _game.Path = Path.Combine(existingRootFolder, existingRelativePath);

            Subject.BuildPath(_game, true).Should().Be(Path.Combine(rootFolder, existingRelativePath));
        }

        [Test]
        public void should_use_built_path_for_new_game()
        {
            var rootFolder = @"C:\Test\Games2".AsOsAgnostic();

            GivenGameFolderName(_game.Title);
            _game.RootFolderPath = rootFolder;
            _game.Path = null;

            Subject.BuildPath(_game, true).Should().Be(Path.Combine(rootFolder, _game.Title));
        }
    }
}
