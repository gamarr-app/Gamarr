using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.GameImport
{
    [TestFixture]
    public class DetectSampleFixture : CoreTest<DetectSample>
    {
        private GameMetadata _game;
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _game = Builder<GameMetadata>.CreateNew()
                                     .With(s => s.Runtime = 30)
                                     .Build();

            _localGame = new LocalGame
            {
                Path = @"C:\Test\30 Rock\30.rock.s01e01.avi",
                Game = new Game { GameMetadata = _game },
                Quality = new QualityModel(Quality.Uplay)
            };
        }

        [Test]
        public void should_always_return_not_sample_for_game_files()
        {
            // All game files should return NotSample since ffprobe sample detection is removed
            Subject.IsSample(_localGame.Game.GameMetadata,
                             _localGame.Path).Should().Be(DetectSampleResult.NotSample);
        }

        [Test]
        public void should_return_not_sample_for_any_path()
        {
            Subject.IsSample(_game, @"C:\Test\some.game.exe").Should().Be(DetectSampleResult.NotSample);
            Subject.IsSample(_game, @"C:\Test\some.game.iso").Should().Be(DetectSampleResult.NotSample);
            Subject.IsSample(_game, @"C:\Test\some.game.doi").Should().Be(DetectSampleResult.NotSample);
        }
    }
}
