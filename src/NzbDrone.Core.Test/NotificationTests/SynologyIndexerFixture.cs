using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Synology;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class SynologyIndexerFixture : CoreTest<SynologyIndexer>
    {
        private Game _game;
        private DownloadMessage _upgrade;

        [SetUp]
        public void SetUp()
        {
            _game = new Game
            {
                Path = @"C:\Test\".AsOsAgnostic()
            };

            _upgrade = new DownloadMessage
            {
                Game = _game,

                GameFile = new GameFile
                {
                    RelativePath = "gamefile1.mkv"
                },

                OldGameFiles = new List<DeletedGameFile>
                {
                    new DeletedGameFile(new GameFile
                    {
                        RelativePath = "oldgamefile1.mkv"
                    }, null),
                    new DeletedGameFile(new GameFile
                    {
                        RelativePath = "oldgamefile2.mkv"
                    }, null)
                }
            };

            Subject.Definition = new NotificationDefinition
            {
                Settings = new SynologyIndexerSettings
                {
                    UpdateLibrary = true
                }
            };
        }

        [Test]
        public void should_not_update_library_if_disabled()
        {
            (Subject.Definition.Settings as SynologyIndexerSettings).UpdateLibrary = false;

            Subject.OnGameRename(_game, new List<RenamedGameFile>());

            Mocker.GetMock<ISynologyIndexerProxy>()
                  .Verify(v => v.UpdateFolder(_game.Path), Times.Never());
        }

        [Test]
        public void should_remove_old_game_on_upgrade()
        {
            Subject.OnDownload(_upgrade);

            Mocker.GetMock<ISynologyIndexerProxy>()
                  .Verify(v => v.DeleteFile(@"C:\Test\oldgamefile1.mkv".AsOsAgnostic()), Times.Once());

            Mocker.GetMock<ISynologyIndexerProxy>()
                  .Verify(v => v.DeleteFile(@"C:\Test\oldgamefile2.mkv".AsOsAgnostic()), Times.Once());
        }

        [Test]
        public void should_add_new_game_on_upgrade()
        {
            Subject.OnDownload(_upgrade);

            Mocker.GetMock<ISynologyIndexerProxy>()
                  .Verify(v => v.AddFile(@"C:\Test\gamefile1.mkv".AsOsAgnostic()), Times.Once());
        }

        [Test]
        public void should_update_entire_game_folder_on_rename()
        {
            Subject.OnGameRename(_game, new List<RenamedGameFile>());

            Mocker.GetMock<ISynologyIndexerProxy>()
                  .Verify(v => v.UpdateFolder(@"C:\Test\".AsOsAgnostic()), Times.Once());
        }
    }
}
