using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Extras.Metadata.Consumers.Xbmc;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras.Metadata.Consumers.Xbmc
{
    [TestFixture]
    public class XbmcNfoDetectorFixture : CoreTest<XbmcNfoDetector>
    {
        private readonly string _path = @"C:\Test\Games\The.Game\the.game.2017.nfo".AsOsAgnostic();

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(_path))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFileSize(_path))
                  .Returns(100);
        }

        [Test]
        public void should_return_true_for_valid_xbmc_nfo()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.ReadAllText(_path))
                  .Returns("<game><title>The Game</title></game>");

            Subject.IsXbmcNfoFile(_path).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_file_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(_path))
                  .Returns(false);

            Subject.IsXbmcNfoFile(_path).Should().BeFalse();
        }

        [Test]
        public void should_return_false_and_not_throw_when_read_throws_io_exception()
        {
            // The candidate file can vanish or become unreadable between the
            // existence check and the read itself (a transient mount error,
            // e.g. Sentry issue DOTNET-ASPNETCORE-38) and must not abort the
            // whole scan event.
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.ReadAllText(_path))
                  .Throws(new IOException("I/O error"));

            Subject.IsXbmcNfoFile(_path).Should().BeFalse();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_false_and_not_throw_when_read_throws_unauthorized_access_exception()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.ReadAllText(_path))
                  .Throws(new UnauthorizedAccessException("Access denied"));

            Subject.IsXbmcNfoFile(_path).Should().BeFalse();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_false_and_not_throw_when_get_file_size_throws_io_exception()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFileSize(_path))
                  .Throws(new IOException("I/O error"));

            Subject.IsXbmcNfoFile(_path).Should().BeFalse();

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
