using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.EpisodeImport.Specifications
{
    [TestFixture]
    public class HasAudioTrackSpecificationFixture : CoreTest<HasAudioTrackSpecification>
    {
        private Game _game;
        private LocalGame _localGame;
        private string _rootFolder;

        [SetUp]
        public void Setup()
        {
             _rootFolder = @"C:\Test\Games".AsOsAgnostic();

             _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = Path.Combine(_rootFolder, "Game Title"))
                                     .Build();

             _localGame = new LocalGame
                                {
                                    Path = @"C:\Test\Unsorted\Game Title\game.title.2000.avi".AsOsAgnostic(),
                                    Game = _game
                                };
        }

        [Test]
        public void should_accept_if_media_info_is_null()
        {
            _localGame.MediaInfo = null;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_if_audio_stream_count_is_0()
        {
            _localGame.MediaInfo = Builder<MediaInfoModel>.CreateNew().With(m => m.AudioStreamCount = 0).Build();

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_if_audio_stream_count_is_0()
        {
            _localGame.MediaInfo = Builder<MediaInfoModel>.CreateNew().With(m => m.AudioStreamCount = 1).Build();

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }
    }
}
