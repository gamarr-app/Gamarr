using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityFinderFixture
    {
        [TestCase(QualitySource.SCENE, 0, Modifier.NONE)]
        public void should_return_Scene(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Scene);
        }

        [TestCase(QualitySource.SCENE, 0, Modifier.CRACKED)]
        public void should_return_SceneCracked(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.SceneCracked);
        }

        [TestCase(QualitySource.GOG, 0, Modifier.DRM_FREE)]
        public void should_return_GOG(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.GOG);
        }

        [TestCase(QualitySource.STEAM, 0, Modifier.NONE)]
        public void should_return_Steam(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Steam);
        }

        [TestCase(QualitySource.REPACK, 0, Modifier.NONE)]
        public void should_return_Repack(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Repack);
        }

        [TestCase(QualitySource.REPACK, 0, Modifier.ALL_DLC)]
        public void should_return_RepackAllDLC(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.RepackAllDLC);
        }

        [TestCase(QualitySource.UNKNOWN, 0, Modifier.NONE)]
        public void should_return_Unknown(QualitySource source, int resolution, Modifier modifier)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution, modifier).Should().Be(Quality.Unknown);
        }
    }
}
