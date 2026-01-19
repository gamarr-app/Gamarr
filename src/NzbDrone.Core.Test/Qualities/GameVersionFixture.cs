using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class GameVersionFixture : CoreTest
    {
        [TestCase("v1.0", 1, 0, 0, 0)]
        [TestCase("v1.0.1", 1, 0, 1, 0)]
        [TestCase("v2.10.5", 2, 10, 5, 0)]
        [TestCase("v1.2.3.4", 1, 2, 3, 4)]
        [TestCase("1.0", 1, 0, 0, 0)]
        [TestCase("1.0.1", 1, 0, 1, 0)]
        [TestCase("2.10.5", 2, 10, 5, 0)]
        [TestCase("V1.5.2", 1, 5, 2, 0)]
        public void should_parse_semantic_version(string versionString, int major, int minor, int patch, int build)
        {
            var version = GameVersion.Parse(versionString);

            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
            version.HasValue.Should().BeTrue();
        }

        [TestCase("Build 12345", 0, 0, 0, 12345)]
        [TestCase("build 99999", 0, 0, 0, 99999)]
        [TestCase("B12345", 0, 0, 0, 12345)]
        [TestCase("b.12345", 0, 0, 0, 12345)]
        public void should_parse_build_number(string versionString, int major, int minor, int patch, int build)
        {
            var version = GameVersion.Parse(versionString);

            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Build.Should().Be(build);
            version.HasValue.Should().BeTrue();
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("invalid")]
        [TestCase("version one")]
        public void should_return_empty_version_for_invalid_input(string versionString)
        {
            var version = GameVersion.Parse(versionString);

            version.HasValue.Should().BeFalse();
            version.Major.Should().Be(0);
            version.Minor.Should().Be(0);
            version.Patch.Should().Be(0);
            version.Build.Should().Be(0);
        }

        [Test]
        public void should_compare_major_versions()
        {
            var v1 = new GameVersion(1, 0, 0, 0);
            var v2 = new GameVersion(2, 0, 0, 0);

            v2.Should().BeGreaterThan(v1);
            v1.Should().BeLessThan(v2);
        }

        [Test]
        public void should_compare_minor_versions()
        {
            var v1 = new GameVersion(1, 5, 0, 0);
            var v2 = new GameVersion(1, 10, 0, 0);

            v2.Should().BeGreaterThan(v1);
            v1.Should().BeLessThan(v2);
        }

        [Test]
        public void should_compare_patch_versions()
        {
            var v1 = new GameVersion(1, 0, 1, 0);
            var v2 = new GameVersion(1, 0, 2, 0);

            v2.Should().BeGreaterThan(v1);
            v1.Should().BeLessThan(v2);
        }

        [Test]
        public void should_compare_build_versions()
        {
            var v1 = new GameVersion(0, 0, 0, 100);
            var v2 = new GameVersion(0, 0, 0, 200);

            v2.Should().BeGreaterThan(v1);
            v1.Should().BeLessThan(v2);
        }

        [Test]
        public void should_be_equal_when_all_parts_match()
        {
            var v1 = new GameVersion(1, 2, 3, 4);
            var v2 = new GameVersion(1, 2, 3, 4);

            v1.CompareTo(v2).Should().Be(0);
            (v1 == v2).Should().BeTrue();
            (v1 != v2).Should().BeFalse();
        }

        [Test]
        public void should_prefer_major_over_minor()
        {
            var v1 = new GameVersion(2, 0, 0, 0);
            var v2 = new GameVersion(1, 99, 99, 99);

            v1.Should().BeGreaterThan(v2);
        }

        [Test]
        public void should_prefer_minor_over_patch()
        {
            var v1 = new GameVersion(1, 2, 0, 0);
            var v2 = new GameVersion(1, 1, 99, 99);

            v1.Should().BeGreaterThan(v2);
        }

        [Test]
        public void equal_operator_tests()
        {
            var v1 = new GameVersion(1, 0, 0, 0);
            var v2 = new GameVersion(1, 0, 0, 0);

            (v1 > v2).Should().BeFalse();
            (v1 < v2).Should().BeFalse();
            (v1 != v2).Should().BeFalse();
            (v1 >= v2).Should().BeTrue();
            (v1 <= v2).Should().BeTrue();
            (v1 == v2).Should().BeTrue();
        }

        [Test]
        public void greater_than_operator_tests()
        {
            var v1 = new GameVersion(1, 1, 0, 0);
            var v2 = new GameVersion(1, 0, 0, 0);

            (v1 > v2).Should().BeTrue();
            (v1 < v2).Should().BeFalse();
            (v1 != v2).Should().BeTrue();
            (v1 >= v2).Should().BeTrue();
            (v1 <= v2).Should().BeFalse();
            (v1 == v2).Should().BeFalse();
        }

        [Test]
        public void less_than_operator_tests()
        {
            var v1 = new GameVersion(1, 0, 0, 0);
            var v2 = new GameVersion(1, 1, 0, 0);

            (v1 > v2).Should().BeFalse();
            (v1 < v2).Should().BeTrue();
            (v1 != v2).Should().BeTrue();
            (v1 >= v2).Should().BeFalse();
            (v1 <= v2).Should().BeTrue();
            (v1 == v2).Should().BeFalse();
        }

        [Test]
        public void operating_on_nulls()
        {
            var version = new GameVersion(1, 0, 0, 0);

            (version < null).Should().BeFalse();
            (version <= null).Should().BeFalse();
            (version > null).Should().BeTrue();
            (version >= null).Should().BeTrue();
        }

        [Test]
        public void empty_version_should_not_have_value()
        {
            var version = new GameVersion();

            version.HasValue.Should().BeFalse();
        }

        [Test]
        public void version_with_any_nonzero_should_have_value()
        {
            new GameVersion(1, 0, 0, 0).HasValue.Should().BeTrue();
            new GameVersion(0, 1, 0, 0).HasValue.Should().BeTrue();
            new GameVersion(0, 0, 1, 0).HasValue.Should().BeTrue();
            new GameVersion(0, 0, 0, 1).HasValue.Should().BeTrue();
        }

        [TestCase(1, 0, 0, 0, "v1.0")]
        [TestCase(1, 2, 0, 0, "v1.2")]
        [TestCase(1, 2, 3, 0, "v1.2.3")]
        [TestCase(1, 2, 3, 4, "v1.2.3.4")]
        [TestCase(0, 0, 0, 12345, "Build 12345")]
        public void should_format_to_string(int major, int minor, int patch, int build, string expected)
        {
            var version = new GameVersion(major, minor, patch, build);

            version.ToString().Should().Be(expected);
        }

        [Test]
        public void empty_version_should_return_empty_string()
        {
            var version = new GameVersion();

            version.ToString().Should().BeEmpty();
        }
    }
}
