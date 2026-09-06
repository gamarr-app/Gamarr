using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using NzbDrone.Common.Disk;
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
            GivenRootFolders();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);

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

        // Handed to the service in a deliberately unhelpful order so a test
        // that expects the lowest id can't pass by accident on "first row".
        private void GivenRootFolders(params RootFolder[] rootFolders)
        {
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(rootFolders.Reverse().ToList());
        }

        private static RootFolder Folder(int id, string path)
        {
            return new RootFolder { Id = id, Path = path };
        }

        private static PlatformRootFolder Default(PlatformFamily platform, string path)
        {
            return new PlatformRootFolder { Platform = platform, Path = path };
        }

        // A LogFactory of its own, so this never touches the global NLog
        // configuration that every other fixture is logging through in
        // parallel. Must run before Subject is first touched.
        private MemoryTarget GivenCapturedLogs()
        {
            var target = new MemoryTarget { Layout = "${level}|${message}" };

            var config = new LoggingConfiguration();
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);

            var factory = new LogFactory { Configuration = config };

            Mocker.SetConstant(factory.GetLogger("PlatformRootFolderService"));

            return target;
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

        // Null is now reserved for "this instance has no root folders at all";
        // anything else resolves, because failing the add outright is worse for
        // the user than landing in a folder they can move the game out of.
        [Test]
        public void should_return_null_when_nothing_is_configured_and_there_are_no_root_folders()
        {
            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().BeNull();
        }

        [Test]
        public void should_return_null_when_only_another_platform_has_a_default_and_there_are_no_root_folders()
        {
            GivenPlatformDefaults(Default(PlatformFamily.NintendoSwitch, "/media/Switch"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.PC).Should().BeNull();
        }

        [Test]
        public void should_fall_back_to_the_only_root_folder_when_no_default_is_configured()
        {
            GivenRootFolders(Folder(3, "/media/Games"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Games");
        }

        // Not silent: the log line is the whole reason redirecting an add that
        // used to fail validation is acceptable, so assert it exists, is at
        // Info (ExceptionVerification only captures Warn and above, hence the
        // private LogFactory rather than a global NLog target — fixtures here
        // run in parallel), and names both the platform and the chosen path.
        [Test]
        public void should_log_at_info_when_falling_back_to_a_root_folder()
        {
            var logs = GivenCapturedLogs();

            GivenRootFolders(Folder(3, "/media/Games"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch);

            logs.Logs.Should().ContainSingle(l => l.StartsWith("Info|") &&
                                                  l.Contains("NintendoSwitch") &&
                                                  l.Contains("/media/Games"));
        }

        [Test]
        public void should_not_log_when_a_platform_default_resolved()
        {
            var logs = GivenCapturedLogs();

            GivenPlatformDefaults(Default(PlatformFamily.NintendoSwitch, "/media/Switch"));
            GivenRootFolders(Folder(3, "/media/Games"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch);

            logs.Logs.Should().NotContain(l => l.StartsWith("Info|"));
        }

        [Test]
        public void should_fall_back_to_the_oldest_root_folder_when_there_are_several()
        {
            GivenRootFolders(Folder(2, "/media/Games"),
                             Folder(5, "/media/Switch"),
                             Folder(9, "/media/Xbox"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Games");
        }

        [Test]
        public void should_skip_an_inaccessible_root_folder_when_falling_back()
        {
            GivenRootFolders(Folder(2, "/media/Games"),
                             Folder(5, "/media/Switch"));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists("/media/Games"))
                  .Returns(false);

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Switch");
        }

        // An unmounted root folder shouldn't turn an add into a hard failure;
        // the downstream path validators report a missing folder properly.
        [Test]
        public void should_still_fall_back_when_no_root_folder_is_accessible()
        {
            GivenRootFolders(Folder(2, "/media/Games"),
                             Folder(5, "/media/Switch"));

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Games");
        }

        // A single root folder is unambiguous, so it is taken without a disk
        // check at all — nothing to prefer it over.
        [Test]
        public void should_not_check_the_disk_when_there_is_only_one_root_folder()
        {
            GivenRootFolders(Folder(3, "/media/Games"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Games");

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FolderExists(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_prefer_the_unknown_default_over_falling_back_to_a_root_folder()
        {
            GivenPlatformDefaults(Default(PlatformFamily.Unknown, "/media/Games"));
            GivenRootFolders(Folder(2, "/media/Downloads"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Games");
        }

        [Test]
        public void should_prefer_the_platform_default_over_falling_back_to_a_root_folder()
        {
            GivenPlatformDefaults(Default(PlatformFamily.Unknown, "/media/Games"),
                                  Default(PlatformFamily.NintendoSwitch, "/media/Switch"));
            GivenRootFolders(Folder(2, "/media/Downloads"));

            Subject.GetDefaultRootFolderPath(PlatformFamily.NintendoSwitch).Should().Be("/media/Switch");
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
