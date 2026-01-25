using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Common.Test.EnvironmentInfo
{
    [TestFixture]
    public class BuildInfoFixture
    {
        [Test]
        public void should_return_version()
        {
            // Major version 0 is used for dev builds (0.0.1.* in Directory.Build.props)
            // Major version 6 or 10 is used for production builds
            BuildInfo.Version.Major.Should().BeOneOf(0, 6, 10);
        }

        [Test]
        public void should_get_branch()
        {
            BuildInfo.Branch.Should().NotBe("unknown");
            BuildInfo.Branch.Should().NotBeNullOrWhiteSpace();
        }
    }
}
