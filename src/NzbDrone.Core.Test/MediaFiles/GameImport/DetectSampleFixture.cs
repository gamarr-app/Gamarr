using System;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

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

        private void GivenRuntime(int seconds)
        {
            Mocker.GetMock<IVideoFileInfoReader>()
                  .Setup(s => s.GetRunTime(It.IsAny<string>()))
                  .Returns(new TimeSpan(0, 0, seconds));
        }

        [Test]
        public void should_return_false_for_flv()
        {
            _localGame.Path = @"C:\Test\some.show.s01e01.flv";

            ShouldBeNotSample();

            Mocker.GetMock<IVideoFileInfoReader>().Verify(c => c.GetRunTime(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_false_for_strm()
        {
            _localGame.Path = @"C:\Test\some.show.s01e01.strm";

            ShouldBeNotSample();

            Mocker.GetMock<IVideoFileInfoReader>().Verify(c => c.GetRunTime(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_false_for_iso()
        {
            _localGame.Path = @"C:\Test\some game (2000).iso";

            ShouldBeNotSample();

            Mocker.GetMock<IVideoFileInfoReader>().Verify(c => c.GetRunTime(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_false_for_img()
        {
            _localGame.Path = @"C:\Test\some game (2000).img";

            ShouldBeNotSample();

            Mocker.GetMock<IVideoFileInfoReader>().Verify(c => c.GetRunTime(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_false_for_m2ts()
        {
            _localGame.Path = @"C:\Test\some game (2000).m2ts";

            ShouldBeNotSample();

            Mocker.GetMock<IVideoFileInfoReader>().Verify(c => c.GetRunTime(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_use_runtime()
        {
            GivenRuntime(120);

            Subject.IsSample(_localGame.Game.GameMetadata,
                             _localGame.Path);

            Mocker.GetMock<IVideoFileInfoReader>().Verify(v => v.GetRunTime(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_return_true_if_runtime_is_less_than_minimum()
        {
            GivenRuntime(60);

            ShouldBeSample();
        }

        [Test]
        public void should_return_false_if_runtime_greater_than_minimum()
        {
            GivenRuntime(600);

            ShouldBeNotSample();
        }

        [Test]
        public void should_return_false_if_runtime_greater_than_webisode_minimum()
        {
            _game.Runtime = 6;
            GivenRuntime(299);

            ShouldBeNotSample();
        }

        [Test]
        public void should_return_false_if_runtime_greater_than_anime_short_minimum()
        {
            _game.Runtime = 2;
            GivenRuntime(60);

            ShouldBeNotSample();
        }

        [Test]
        public void should_return_true_if_runtime_less_than_anime_short_minimum()
        {
            _game.Runtime = 2;
            GivenRuntime(10);

            ShouldBeSample();
        }

        [Test]
        public void should_return_indeterminate_if_mediainfo_result_is_null()
        {
            Mocker.GetMock<IVideoFileInfoReader>()
                  .Setup(s => s.GetRunTime(It.IsAny<string>()))
                  .Returns((TimeSpan?)null);

            Subject.IsSample(_localGame.Game.GameMetadata,
                             _localGame.Path).Should().Be(DetectSampleResult.Indeterminate);

            ExceptionVerification.ExpectedErrors(1);
        }

        private void ShouldBeSample()
        {
            Subject.IsSample(_localGame.Game.GameMetadata,
                             _localGame.Path).Should().Be(DetectSampleResult.Sample);
        }

        private void ShouldBeNotSample()
        {
            Subject.IsSample(_localGame.Game.GameMetadata,
                             _localGame.Path).Should().Be(DetectSampleResult.NotSample);
        }
    }
}
