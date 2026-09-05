using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.RootFolderTests
{
    [TestFixture]
    public class PlatformRootFolderServiceFixture : CoreTest<PlatformRootFolderService>
    {
        [SetUp]
        public void Setup()
        {
            GivenPlatformDefaults();

            Mocker.GetMock<IPlatformRootFolderRepository>()
                  .Setup(s => s.Insert(It.IsAny<PlatformRootFolder>()))
                  .Returns<PlatformRootFolder>(p => p);

            Mocker.GetMock<IPlatformRootFolderRepository>()
                  .Setup(s => s.Update(It.IsAny<PlatformRootFolder>()))
                  .Returns<PlatformRootFolder>(p => p);
        }

        private void GivenPlatformDefaults(params PlatformRootFolder[] defaults)
        {
            Mocker.GetMock<IPlatformRootFolderRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<PlatformRootFolder>(defaults));
        }

        private static PlatformRootFolder Default(PlatformFamily platform, string path)
        {
            return new PlatformRootFolder { Platform = platform, Path = path };
        }

        [Test]
        public void should_return_the_default_configured_for_the_platform()
        {
            GivenPlatformDefaults(Default(PlatformFamily.NintendoSwitch, "/media/Switch"),
                                  Default(PlatformFamily.PC, "/media/Games"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Switch");
        }

        [Test]
        public void should_fall_back_to_the_unknown_default_when_the_platform_has_none()
        {
            GivenPlatformDefaults(Default(PlatformFamily.Unknown, "/media/Games"),
                                  Default(PlatformFamily.NintendoSwitch, "/media/Switch"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.Xbox).Should().Be("/media/Games");
        }

        [Test]
        public void should_prefer_the_platform_default_over_the_unknown_default()
        {
            GivenPlatformDefaults(Default(PlatformFamily.Unknown, "/media/Games"),
                                  Default(PlatformFamily.NintendoSwitch, "/media/Switch"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Switch");
        }

        // PlatformMatches() treats the generic Nintendo family as compatible
        // with every Nintendo console, which would make a Switch default apply
        // to a Wii game. Folder choice has to be exact.
        [Test]
        public void should_not_use_another_nintendo_platforms_default()
        {
            GivenPlatformDefaults(Default(PlatformFamily.NintendoSwitch, "/media/Switch"),
                                  Default(PlatformFamily.Unknown, "/media/Games"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoWii).Should().Be("/media/Games");
        }

        // Without a default configured the caller keeps whatever root folder it
        // already had, so an add with none still fails validation as before
        // rather than silently landing in an arbitrary root folder.
        [Test]
        public void should_return_null_when_nothing_is_configured()
        {
            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().BeNull();
        }

        [Test]
        public void should_return_null_when_only_another_platform_has_a_default()
        {
            GivenPlatformDefaults(Default(PlatformFamily.NintendoSwitch, "/media/Switch"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.PC).Should().BeNull();
        }

        [Test]
        public void should_add_a_default_for_a_platform()
        {
            var path = @"C:\media\Switch".AsOsAgnostic();

            Subject.Add(Default(PlatformFamily.NintendoSwitch, path));

            Mocker.GetMock<IPlatformRootFolderRepository>()
                  .Verify(s => s.Insert(It.Is<PlatformRootFolder>(p => p.Platform == PlatformFamily.NintendoSwitch && p.Path == path)), Times.Once());
        }

        [Test]
        public void should_not_add_a_second_default_for_the_same_platform()
        {
            GivenPlatformDefaults(Default(PlatformFamily.NintendoSwitch, @"C:\media\Switch".AsOsAgnostic()));

            Assert.Throws<InvalidOperationException>(() => Subject.Add(Default(PlatformFamily.NintendoSwitch, @"C:\media\Switch2".AsOsAgnostic())));
        }

        [Test]
        public void should_not_add_a_relative_path()
        {
            Assert.Throws<ArgumentException>(() => Subject.Add(Default(PlatformFamily.NintendoSwitch, "Switch")));
        }

        [Test]
        public void should_allow_updating_the_path_of_an_existing_platform_default()
        {
            var existing = Default(PlatformFamily.NintendoSwitch, @"C:\media\Switch".AsOsAgnostic());
            existing.Id = 1;

            GivenPlatformDefaults(existing);

            var updated = Default(PlatformFamily.NintendoSwitch, @"C:\media\NintendoSwitch".AsOsAgnostic());
            updated.Id = 1;

            Subject.Update(updated);

            Mocker.GetMock<IPlatformRootFolderRepository>()
                  .Verify(s => s.Update(It.Is<PlatformRootFolder>(p => p.Path == updated.Path)), Times.Once());
        }
    }
}
