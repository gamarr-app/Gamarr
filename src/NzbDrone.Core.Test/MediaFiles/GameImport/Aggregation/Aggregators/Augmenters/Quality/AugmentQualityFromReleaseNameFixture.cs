using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators.Augmenters.Quality;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Aggregation.Aggregators.Augmenters.Quality
{
    [TestFixture]
    public class AugmentQualityFromReleaseNameFixture : CoreTest<AugmentQualityFromReleaseName>
    {
        private LocalGame _localGame;
        private DownloadClientItem _downloadClientItem;
        private ParsedGameInfo _hdtvParsedEpisodeInfo;
        private ParsedGameInfo _webdlParsedEpisodeInfo;

        [SetUp]
        public void Setup()
        {
            _hdtvParsedEpisodeInfo = Builder<ParsedGameInfo>.CreateNew()
                                                               .With(p => p.Quality =
                                                                   new QualityModel(Core.Qualities.Quality.Uplay))
                                                               .Build();

            _webdlParsedEpisodeInfo = Builder<ParsedGameInfo>.CreateNew()
                                                                .With(p => p.Quality =
                                                                    new QualityModel(Core.Qualities.Quality.Epic))
                                                                .Build();

            _localGame = Builder<LocalGame>.CreateNew()
                                                 .Build();

            _downloadClientItem = Builder<DownloadClientItem>.CreateNew()
                                                             .Build();
        }

        [Test]
        public void should_return_null_if_download_client_item_is_null()
        {
            Subject.AugmentQuality(_localGame, null).Should().BeNull();
        }

        [Test]
        public void should_return_null_if_no_grabbed_history()
        {
            Mocker.GetMock<IDownloadHistoryService>()
                  .Setup(s => s.GetLatestGrab(It.IsAny<string>()))
                  .Returns((DownloadHistory)null);

            Subject.AugmentQuality(_localGame, _downloadClientItem).Should().BeNull();
        }

        [TestCase("Game.Title-CODEX", QualitySource.SCENE, Confidence.Tag, 0, Confidence.Fallback)]
        [TestCase("Game.Title.GOG", QualitySource.GOG, Confidence.Tag, 0, Confidence.Fallback)]
        [TestCase("Game.Title-FitGirl", QualitySource.REPACK, Confidence.Tag, 0, Confidence.Fallback)]
        public void should_return_augmented_quality(string title, QualitySource source, Confidence sourceConfidence, int resolution, Confidence resolutionConfidence)
        {
            Mocker.GetMock<IDownloadHistoryService>()
                  .Setup(s => s.GetLatestGrab(It.IsAny<string>()))
                  .Returns(Builder<DownloadHistory>.CreateNew()
                                                   .With(h => h.SourceTitle = title)
                                                   .Build());

            var result = Subject.AugmentQuality(_localGame, _downloadClientItem);

            result.Should().NotBe(null);
            result.Source.Should().Be(source);
            result.SourceConfidence.Should().Be(sourceConfidence);
            result.Resolution.Should().Be(resolution);
            result.ResolutionConfidence.Should().Be(resolutionConfidence);
        }
    }
}
