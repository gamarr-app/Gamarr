using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class GameFileExtensionsFixture : CoreTest
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                .With(g => g.Path = @"C:\Test\Games\GameTitle".AsOsAgnostic())
                .Build();
        }

        [Test]
        public void should_return_game_path_when_relative_path_is_empty()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = string.Empty)
                .Build();

            gameFile.GetPath(_game).Should().Be(_game.Path);
        }

        [Test]
        public void should_return_game_path_when_relative_path_is_null()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = null)
                .Build();

            gameFile.GetPath(_game).Should().Be(_game.Path);
        }

        [Test]
        public void should_return_combined_path_when_relative_path_is_set()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = "SubFolder")
                .Build();

            var expectedPath = Path.Combine(_game.Path, "SubFolder");
            gameFile.GetPath(_game).Should().Be(expectedPath);
        }

        [Test]
        public void should_return_combined_path_for_file_with_extension()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = "Game.exe")
                .Build();

            var expectedPath = Path.Combine(_game.Path, "Game.exe");
            gameFile.GetPath(_game).Should().Be(expectedPath);
        }

        [Test]
        public void should_return_true_for_is_folder_when_relative_path_is_empty()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = string.Empty)
                .Build();

            gameFile.IsFolder().Should().BeTrue();
        }

        [Test]
        public void should_return_true_for_is_folder_when_relative_path_is_null()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = null)
                .Build();

            gameFile.IsFolder().Should().BeTrue();
        }

        [Test]
        public void should_return_false_for_is_folder_when_relative_path_is_set()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = "Game.exe")
                .Build();

            gameFile.IsFolder().Should().BeFalse();
        }

        [Test]
        public void should_return_false_for_is_folder_when_relative_path_is_whitespace()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = "   ")
                .Build();

            // Whitespace-only is treated as empty/folder
            gameFile.IsFolder().Should().BeTrue();
        }

        [Test]
        public void should_use_attached_game_when_calling_get_path_without_game_parameter()
        {
            var gameFile = Builder<GameFile>.CreateNew()
                .With(f => f.RelativePath = "SubFolder")
                .With(f => f.Game = _game)
                .Build();

            var expectedPath = Path.Combine(_game.Path, "SubFolder");
            gameFile.GetPath().Should().Be(expectedPath);
        }
    }
}
