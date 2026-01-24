using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityFinderFixture
    {
        [TestCase(QualitySource.SCENE, 0, Modifier.PRELOAD)]
        public void should_return_Preload(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Preload);
        }

        [TestCase(QualitySource.UNKNOWN, 0, Modifier.NONE)]
        public void should_return_Unknown(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Unknown);
        }

        [TestCase(QualitySource.SCENE, 0, Modifier.CRACKED)]
        public void should_return_SceneCracked(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.SceneCracked);
        }

        [TestCase(QualitySource.SCENE, 0, Modifier.NONE)]
        public void should_return_Scene(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Scene);
        }

        [TestCase(QualitySource.STEAM, 0, Modifier.NONE)]
        public void should_return_Scene_Steam(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Steam);
        }

        [TestCase(QualitySource.UPLAY, 0, Modifier.NONE)]
        public void should_return_Uplay(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Uplay);
        }

        [TestCase(QualitySource.ORIGIN, 0, Modifier.NONE)]
        public void should_return_Origin(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Origin);
        }

        [TestCase(QualitySource.REPACK, 0, Modifier.NONE)]
        public void should_return_Repack(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Repack);
        }
    }
}
