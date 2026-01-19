using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Xbmc;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests.Xbmc
{
    [TestFixture]
    public class OnDownloadFixture : CoreTest<Notifications.Xbmc.Xbmc>
    {
        private DownloadMessage _downloadMessage;

        [SetUp]
        public void Setup()
        {
            var game = Builder<Game>.CreateNew()
                                        .Build();

            var gameFile = Builder<GameFile>.CreateNew()
                                                   .Build();

            _downloadMessage = Builder<DownloadMessage>.CreateNew()
                                                       .With(d => d.Game = game)
                                                       .With(d => d.GameFile = gameFile)
                                                       .With(d => d.OldGameFiles = new List<DeletedGameFile>())
                                                       .Build();

            Subject.Definition = new NotificationDefinition();
            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true
                                          };
        }

        private void GivenOldFiles()
        {
            _downloadMessage.OldGameFiles = Builder<DeletedGameFile>
                .CreateListOfSize(1)
                .All()
                .WithFactory(() => new DeletedGameFile(Builder<GameFile>.CreateNew().Build(), null))
                .Build()
                .ToList();

            Subject.Definition.Settings = new XbmcSettings
                                          {
                                              Host = "localhost",
                                              UpdateLibrary = true,
                                              CleanLibrary = true
                                          };
        }

        [Test]
        public void should_not_clean_if_no_game_was_replaced()
        {
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Never());
        }

        [Test]
        public void should_clean_if_game_was_replaced()
        {
            GivenOldFiles();
            Subject.OnDownload(_downloadMessage);
            Subject.ProcessQueue();

            Mocker.GetMock<IXbmcService>().Verify(v => v.Clean(It.IsAny<XbmcSettings>()), Times.Once());
        }
    }
}
