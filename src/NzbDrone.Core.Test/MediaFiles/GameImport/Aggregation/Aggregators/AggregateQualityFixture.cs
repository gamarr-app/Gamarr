using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators.Augmenters.Quality;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Aggregation.Aggregators
{
    [TestFixture]
    public class AggregateQualityFixture : CoreTest<AggregateQuality>
    {
        private Mock<IAugmentQuality> _fileExtensionAugmenter;
        private Mock<IAugmentQuality> _nameAugmenter;
        private Mock<IAugmentQuality> _releaseNameAugmenter;

        [SetUp]
        public void Setup()
        {
            _fileExtensionAugmenter = new Mock<IAugmentQuality>();
            _nameAugmenter = new Mock<IAugmentQuality>();
            _releaseNameAugmenter = new Mock<IAugmentQuality>();

            _fileExtensionAugmenter.SetupGet(s => s.Order).Returns(1);
            _nameAugmenter.SetupGet(s => s.Order).Returns(2);
            _releaseNameAugmenter.SetupGet(s => s.Order).Returns(5);

            // For games, resolution is always 0 (not applicable)
            _fileExtensionAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                                   .Returns(new AugmentQualityResult(QualitySource.SCENE, Confidence.Fallback, 0, Confidence.Fallback, Modifier.NONE, Confidence.Fallback, new Revision(), Confidence.Fallback));

            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.SCENE, Confidence.Default, 0, Confidence.Default, Modifier.NONE, Confidence.Default, new Revision(), Confidence.Default));

            // Steam source without modifier
            _releaseNameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                                 .Returns(new AugmentQualityResult(QualitySource.STEAM, Confidence.Tag, 0, Confidence.Fallback, Modifier.NONE, Confidence.Fallback, new Revision(), Confidence.Fallback));
        }

        private void GivenAugmenters(params Mock<IAugmentQuality>[] mocks)
        {
            Mocker.SetConstant<IEnumerable<IAugmentQuality>>(mocks.Select(c => c.Object));
        }

        [Test]
        public void should_return_Scene_from_extension_when_other_augments_are_null()
        {
            var nullMock = new Mock<IAugmentQuality>();
            nullMock.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                    .Returns<LocalGame, DownloadClientItem>((l, d) => null);

            GivenAugmenters(_fileExtensionAugmenter, nullMock);

            var result = Subject.Aggregate(new LocalGame(), null);

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Extension);
            result.Quality.Quality.Should().Be(Quality.Scene);
        }

        [Test]
        public void should_return_Scene_when_name_indicates_scene()
        {
            GivenAugmenters(_fileExtensionAugmenter, _nameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), null);

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Name);
            result.Quality.Quality.Should().Be(Quality.Scene);
        }

        [Test]
        public void should_return_Steam_when_release_name_indicates_steam()
        {
            GivenAugmenters(_fileExtensionAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), new DownloadClientItem());

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Name);
            result.Quality.Quality.Should().Be(Quality.Steam);
        }

        [Test]
        public void should_return_version_1_when_no_version_specified()
        {
            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(1);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Unknown);
        }

        [Test]
        public void should_return_version_2_when_name_indicates_proper()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.SCENE, Confidence.Default, 0, Confidence.Default, Modifier.NONE, Confidence.Default, new Revision(2), Confidence.Tag));

            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(2);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [Test]
        public void should_return_version_0_when_file_name_indicates_v0()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.SCENE, Confidence.Default, 0, Confidence.Default, Modifier.NONE, Confidence.Default, new Revision(0), Confidence.Tag));

            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(0);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [Test]
        public void should_return_version_2_when_file_name_indicates_v0_and_release_name_indicates_v2()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.SCENE, Confidence.Default, 0, Confidence.Default, Modifier.NONE, Confidence.Default, new Revision(0), Confidence.Tag));

            _releaseNameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                                 .Returns(new AugmentQualityResult(QualitySource.SCENE, Confidence.Default, 0, Confidence.Default, Modifier.NONE, Confidence.Default, new Revision(2), Confidence.Tag));

            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(2);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [Test]
        public void should_return_Repack_when_source_is_repack()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.REPACK, Confidence.Default, 0, Confidence.Default, Modifier.NONE, Confidence.Default, new Revision(), Confidence.Default));

            GivenAugmenters(_fileExtensionAugmenter, _nameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), null);

            result.Quality.Quality.Should().Be(Quality.Repack);
        }

        [Test]
        public void should_return_RepackAllDLC_when_source_is_repack_with_all_dlc_modifier()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.REPACK, Confidence.Default, 0, Confidence.Default, Modifier.ALL_DLC, Confidence.Default, new Revision(), Confidence.Default));

            GivenAugmenters(_fileExtensionAugmenter, _nameAugmenter);

            var result = Subject.Aggregate(new LocalGame(), null);

            result.Quality.Quality.Should().Be(Quality.RepackAllDLC);
        }
    }
}
