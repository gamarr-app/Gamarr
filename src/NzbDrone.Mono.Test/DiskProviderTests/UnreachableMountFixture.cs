using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Mono.Disk;
using NzbDrone.Test.Common;

namespace NzbDrone.Mono.Test.DiskProviderTests
{
    [TestFixture]
    [Platform(Exclude = "Win")]
    public class UnreachableMountFixture : TestBase<DiskProvider>
    {
        private const string MountPath = "/mnt/unreachable";
        private const string GamePath = "/mnt/unreachable/games/Some Game";

        [SetUp]
        public void Setup()
        {
            PosixOnly();

            var mount = new Mock<IMount>();
            mount.SetupGet(v => v.RootDirectory).Returns(MountPath);

            // A share whose server went away: listed in /proc/mounts, but statfs fails.
            mount.SetupGet(v => v.AvailableFreeSpace)
                 .Throws(new InvalidOperationException("Socket not connected"));
            mount.SetupGet(v => v.TotalSize)
                 .Throws(new InvalidOperationException("Socket not connected"));

            Mocker.GetMock<IProcMountProvider>()
                  .Setup(v => v.GetMounts())
                  .Returns(new List<IMount> { mount.Object });

            Mocker.GetMock<ISymbolicLinkResolver>()
                  .Setup(v => v.GetCompleteRealPath(It.IsAny<string>()))
                  .Returns<string>(s => s);
        }

        [Test]
        public void should_return_null_free_space_when_mount_is_not_responding()
        {
            Subject.GetAvailableSpace(GamePath).Should().BeNull();
        }

        [Test]
        public void should_return_null_total_size_when_mount_is_not_responding()
        {
            Subject.GetTotalSize(GamePath).Should().BeNull();
        }
    }
}
