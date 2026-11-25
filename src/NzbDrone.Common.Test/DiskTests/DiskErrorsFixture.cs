using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.DiskTests
{
    [TestFixture]
    public class DiskErrorsFixture : TestBase
    {
        private static IOException WithHResult(int hResult)
        {
            return new IOException("opaque", hResult);
        }

        [Test]
        public void should_treat_unauthorized_access_as_environmental()
        {
            DiskErrors.IsEnvironmental(new UnauthorizedAccessException()).Should().BeTrue();
        }

        [Test]
        public void should_not_treat_path_too_long_as_environmental()
        {
            DiskErrors.IsEnvironmental(new PathTooLongException()).Should().BeFalse();
        }

        [Test]
        public void should_not_treat_missing_paths_as_environmental_by_default()
        {
            DiskErrors.IsEnvironmental(new DirectoryNotFoundException()).Should().BeFalse();
            DiskErrors.IsEnvironmental(new FileNotFoundException()).Should().BeFalse();
        }

        [Test]
        public void should_treat_missing_paths_as_environmental_when_allowed()
        {
            DiskErrors.IsEnvironmental(new DirectoryNotFoundException(), true).Should().BeTrue();
            DiskErrors.IsEnvironmental(new FileNotFoundException(), true).Should().BeTrue();
        }

        [Test]
        public void should_not_treat_non_io_exceptions_as_environmental()
        {
            DiskErrors.IsEnvironmental(new InvalidOperationException()).Should().BeFalse();
            DiskErrors.IsEnvironmental(new ArgumentException()).Should().BeFalse();
            DiskErrors.IsEnvironmental(null).Should().BeFalse();
        }

        [Test]
        public void should_not_classify_by_message_text()
        {
            DiskErrors.IsEnvironmental(new IOException("Permission denied")).Should().BeFalse();
            DiskErrors.IsEnvironmental(new IOException("No space left on device")).Should().BeFalse();
        }

        [Test]
        [Platform(Exclude = "Win")]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(13)]
        [TestCase(16)]
        [TestCase(28)]
        [TestCase(30)]
        public void should_treat_unix_environmental_errno_as_environmental(int errno)
        {
            DiskErrors.IsEnvironmental(WithHResult(errno)).Should().BeTrue();
        }

        [Test]
        [Platform(Exclude = "Win")]
        [TestCase(0)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(22)]
        [TestCase(36)]
        public void should_not_treat_other_unix_errno_as_environmental(int errno)
        {
            DiskErrors.IsEnvironmental(WithHResult(errno)).Should().BeFalse();
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void should_treat_enoent_as_environmental_only_when_allowed()
        {
            DiskErrors.IsEnvironmental(WithHResult(2)).Should().BeFalse();
            DiskErrors.IsEnvironmental(WithHResult(2), true).Should().BeTrue();
        }

        [Test]
        [Platform(Include = "Linux")]
        public void should_treat_linux_estale_as_environmental()
        {
            DiskErrors.IsEnvironmental(WithHResult(116)).Should().BeTrue();
        }

        [Test]
        [Platform(Include = "MacOsX")]
        public void should_treat_macos_estale_as_environmental()
        {
            DiskErrors.IsEnvironmental(WithHResult(70)).Should().BeTrue();
        }

        [Test]
        [Platform(Include = "Win")]
        [TestCase(0x80070005)]
        [TestCase(0x80070013)]
        [TestCase(0x80070020)]
        [TestCase(0x80070021)]
        [TestCase(0x80070070)]
        [TestCase(0x8007045D)]
        public void should_treat_windows_environmental_hresults_as_environmental(uint hResult)
        {
            DiskErrors.IsEnvironmental(WithHResult(unchecked((int)hResult))).Should().BeTrue();
        }

        [Test]
        [Platform(Include = "Win")]
        public void should_not_treat_unrelated_windows_hresult_as_environmental()
        {
            DiskErrors.IsEnvironmental(WithHResult(unchecked((int)0x80070057))).Should().BeFalse();
        }

        [Test]
        public void is_missing_should_match_not_found_types()
        {
            DiskErrors.IsMissing(new DirectoryNotFoundException()).Should().BeTrue();
            DiskErrors.IsMissing(new FileNotFoundException()).Should().BeTrue();
            DiskErrors.IsMissing(new IOException("x")).Should().BeFalse();
            DiskErrors.IsMissing(new UnauthorizedAccessException()).Should().BeFalse();
        }
    }
}
