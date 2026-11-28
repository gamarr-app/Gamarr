using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Games;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Extras
{
    [TestFixture]
    public class ExtraFileOwnerFixture
    {
        private static GameFile File(int id, string relativePath)
        {
            return new GameFile { Id = id, GameId = 1, RelativePath = relativePath };
        }

        private readonly List<GameFile> _files = new List<GameFile>
        {
            File(1, "Game"),
            File(2, "Updates/1.1"),
            File(3, "Updates/1.2/Game v1.2.nsp"),
            File(4, "DLC/Pack One"),
            File(5, "DLC/Pack Two.nsp")
        };

        [TestCase("Updates/1.1/cover.jpg", 2)]
        [TestCase("Updates/1.1/sub/readme.nfo", 2)]
        [TestCase("Updates\\1.1\\cover.jpg", 2)]
        [TestCase("Updates/1.2/cover.jpg", 3)]
        [TestCase("DLC/Pack One/info.nfo", 4)]
        [TestCase("DLC/Pack Two.nfo", 5)]
        public void should_attribute_extra_to_the_component_that_contains_it(string path, int expected)
        {
            ExtraFileOwner.FindComponentOwner(_files, path).Id.Should().Be(expected);
        }

        [TestCase("cover.jpg")]
        [TestCase("Game.nfo")]
        [TestCase("Updates/readme.txt")]
        [TestCase("Updates/1.10/cover.jpg")]
        [TestCase("DLC/Other.nfo")]
        public void should_not_claim_extras_no_component_contains(string path)
        {
            ExtraFileOwner.FindComponentOwner(_files, path).Should().BeNull();
        }

        [Test]
        public void should_prefer_the_most_specific_component()
        {
            var files = new List<GameFile> { File(1, "DLC/Pack"), File(2, "DLC/Pack/Sub") };

            ExtraFileOwner.FindComponentOwner(files, "DLC/Pack/Sub/a.nfo").Id.Should().Be(2);
            ExtraFileOwner.FindComponentOwner(files, "DLC/Pack/a.nfo").Id.Should().Be(1);
        }

        [Test]
        public void should_fall_back_to_primary_when_no_component_contains_extra()
        {
            var game = new Game { Id = 1, GameFileId = 1 };

            ExtraFileOwner.FindOwnerId(game, _files, "cover.jpg").Should().Be(1);
        }

        [Test]
        public void should_fall_back_to_base_when_primary_points_at_a_component()
        {
            var game = new Game { Id = 1, GameFileId = 2 };

            ExtraFileOwner.FindOwnerId(game, _files, "cover.jpg").Should().Be(1);
        }

        [Test]
        public void should_keep_primary_pointer_when_game_has_no_files()
        {
            var game = new Game { Id = 1, GameFileId = 7 };

            ExtraFileOwner.FindOwnerId(game, new List<GameFile>(), "cover.jpg").Should().Be(7);
        }
    }

    [TestFixture]
    public class FixExtraFileOwnersFixture : CoreTest<FixExtraFileOwners>
    {
        [Test]
        public void should_move_mis_attributed_rows_and_leave_correct_ones()
        {
            var files = new List<GameFile>
            {
                new GameFile { Id = 1, GameId = 1, RelativePath = "Game" },
                new GameFile { Id = 2, GameId = 1, RelativePath = "Updates/1.1" }
            };

            var wrong = new MetadataFile { Id = 10, GameId = 1, GameFileId = 1, RelativePath = "Updates/1.1/cover.jpg" };
            var right = new MetadataFile { Id = 11, GameId = 1, GameFileId = 1, RelativePath = "cover.jpg" };
            var already = new MetadataFile { Id = 12, GameId = 1, GameFileId = 2, RelativePath = "Updates/1.1/x.nfo" };

            Mocker.GetMock<IMediaFileRepository>().Setup(r => r.All()).Returns(files);
            Mocker.GetMock<IExtraFileRepository<MetadataFile>>().Setup(r => r.All()).Returns(new List<MetadataFile> { wrong, right, already });

            Subject.Clean();

            wrong.GameFileId.Should().Be(2);
            right.GameFileId.Should().Be(1);
            already.GameFileId.Should().Be(2);

            Mocker.GetMock<IExtraFileRepository<MetadataFile>>()
                  .Verify(r => r.UpdateMany(It.Is<IList<MetadataFile>>(l => l.Count == 1 && l[0].Id == 10)), Times.Once());
        }

        [Test]
        public void should_not_touch_anything_when_no_game_has_components()
        {
            Mocker.GetMock<IMediaFileRepository>().Setup(r => r.All())
                  .Returns(new List<GameFile> { new GameFile { Id = 1, GameId = 1, RelativePath = "Game" } });

            Subject.Clean();

            Mocker.GetMock<IExtraFileRepository<MetadataFile>>().Verify(r => r.All(), Times.Never());
        }
    }
}
