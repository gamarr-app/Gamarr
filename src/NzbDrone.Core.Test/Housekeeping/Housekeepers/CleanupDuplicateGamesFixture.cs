using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Games;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupDuplicateGamesFixture : DbTest<CleanupDuplicateGames, Game>
    {
        private Game GivenGame(int igdbId, PlatformFamily platform, string path, int gameFileId = 0, bool monitored = false, params int[] tags)
        {
            var metadata = Builder<GameMetadata>.CreateNew()
                .With(m => m.IgdbId = igdbId)
                .With(m => m.SteamAppId = 0)
                .With(m => m.Title = "The Legend of Zelda: Ocarina of Time")
                .BuildNew();

            Db.Insert(metadata);

            var game = Builder<Game>.CreateNew()
                .With(g => g.GameMetadataId = metadata.Id)
                .With(g => g.Platform = platform)
                .With(g => g.Path = path)
                .With(g => g.GameFileId = gameFileId)
                .With(g => g.Monitored = monitored)
                .With(g => g.Tags = new HashSet<int>(tags))
                .BuildNew();

            Db.Insert(game);

            return game;
        }

        private void GivenEmptyFolders()
        {
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(It.IsAny<string>())).Returns(true);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderEmpty(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public void should_keep_the_entry_with_files_and_merge_the_other_into_it()
        {
            GivenEmptyFolders();

            var empty = GivenGame(405460, PlatformFamily.NintendoSwitch, "/games/Zelda (2026)", 0, true, 2);
            var withFile = GivenGame(405460, PlatformFamily.NintendoSwitch, "/games/Zelda", 7, false, 1);

            var pending = Builder<PendingRelease>.CreateNew()
                .With(p => p.GameId = empty.Id)
                .With(p => p.ParsedGameInfo = new ParsedGameInfo())
                .With(p => p.Release = new ReleaseInfo())
                .BuildNew();
            Db.Insert(pending);

            Subject.Clean();

            var remaining = AllStoredModels;
            remaining.Should().ContainSingle();
            remaining[0].Id.Should().Be(withFile.Id);
            remaining[0].Monitored.Should().BeTrue();
            remaining[0].Tags.Should().BeEquivalentTo(new[] { 1, 2 });

            Db.All<PendingRelease>().Single().GameId.Should().Be(withFile.Id);

            Mocker.GetMock<IDiskProvider>().Verify(s => s.DeleteFolder("/games/Zelda (2026)", false), Times.Once());
            Mocker.GetMock<IDiskProvider>().Verify(s => s.DeleteFolder("/games/Zelda", It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_keep_the_oldest_when_neither_has_files()
        {
            var first = GivenGame(405460, PlatformFamily.Unknown, "/games/a");
            GivenGame(405460, PlatformFamily.Unknown, "/games/b");

            Subject.Clean();

            AllStoredModels.Should().ContainSingle().Which.Id.Should().Be(first.Id);
        }

        [Test]
        public void should_not_touch_the_same_title_on_different_platforms()
        {
            GivenGame(405460, PlatformFamily.NintendoSwitch, "/games/switch");
            GivenGame(405460, PlatformFamily.NintendoN64, "/games/n64");
            GivenGame(405460, PlatformFamily.Unknown, "/games/any");

            Subject.Clean();

            AllStoredModels.Should().HaveCount(3);
        }

        [Test]
        public void should_leave_the_group_alone_when_several_entries_have_files()
        {
            GivenGame(405460, PlatformFamily.NintendoSwitch, "/games/a", 3);
            GivenGame(405460, PlatformFamily.NintendoSwitch, "/games/b", 4);

            Subject.Clean();

            AllStoredModels.Should().HaveCount(2);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_delete_a_folder_that_still_has_content()
        {
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(It.IsAny<string>())).Returns(true);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderEmpty(It.IsAny<string>())).Returns(false);

            GivenGame(405460, PlatformFamily.PC, "/games/a", 3);
            GivenGame(405460, PlatformFamily.PC, "/games/b");

            Subject.Clean();

            AllStoredModels.Should().ContainSingle();
            Mocker.GetMock<IDiskProvider>().Verify(s => s.DeleteFolder(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }
    }
}
