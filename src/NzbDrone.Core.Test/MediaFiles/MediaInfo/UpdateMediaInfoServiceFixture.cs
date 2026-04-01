using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaInfo
{
    [TestFixture]
    public class UpdateMediaInfoServiceFixture : CoreTest<UpdateMediaInfoService>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 1,
                Path = @"C:\game".AsOsAgnostic()
            };
        }

        [Test]
        public void handle_should_be_noop()
        {
            // Handle is now a no-op since ffprobe scanning is removed
            Subject.Handle(new GameScannedEvent(_game, new List<string>()));
        }

        [Test]
        public void update_should_return_false()
        {
            var gameFile = new GameFile { RelativePath = "game.doi" };

            Subject.Update(gameFile, _game).Should().BeFalse();
        }

        [Test]
        public void update_media_info_should_return_false()
        {
            var gameFile = new GameFile { RelativePath = "game.doi" };

            Subject.UpdateMediaInfo(gameFile, _game).Should().BeFalse();
        }
    }
}
