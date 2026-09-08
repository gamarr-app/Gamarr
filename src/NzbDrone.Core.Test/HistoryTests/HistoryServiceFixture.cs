using System;
using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Test.Qualities;

namespace NzbDrone.Core.Test.HistoryTests
{
    public class HistoryServiceFixture : CoreTest<HistoryService>
    {
        private QualityProfile _profile;
        private QualityProfile _profileCustom;

        [SetUp]
        public void Setup()
        {
            _profile = new QualityProfile { Cutoff = Quality.Epic.Id, Items = QualityFixture.GetDefaultQualities() };
            _profileCustom = new QualityProfile { Cutoff = Quality.Epic.Id, Items = QualityFixture.GetDefaultQualities(Quality.Scene) };
        }

        [Test]
        public void should_return_null_if_no_history()
        {
            Mocker.GetMock<IHistoryRepository>()
                .Setup(v => v.GetBestQualityInHistory(2))
                .Returns(new List<QualityModel>());

            var quality = Subject.GetBestQualityInHistory(_profile, 2);

            quality.Should().BeNull();
        }

        [Test]
        public void should_return_best_quality()
        {
            Mocker.GetMock<IHistoryRepository>()
                .Setup(v => v.GetBestQualityInHistory(2))
                .Returns(new List<QualityModel> { new QualityModel(Quality.Scene), new QualityModel(Quality.GOG) });

            var quality = Subject.GetBestQualityInHistory(_profile, 2);

            quality.Should().Be(new QualityModel(Quality.GOG));
        }

        [Test]
        public void should_return_best_quality_with_custom_order()
        {
            Mocker.GetMock<IHistoryRepository>()
                .Setup(v => v.GetBestQualityInHistory(2))
                .Returns(new List<QualityModel> { new QualityModel(Quality.Scene), new QualityModel(Quality.GOG) });

            var quality = Subject.GetBestQualityInHistory(_profileCustom, 2);

            quality.Should().Be(new QualityModel(Quality.Scene));
        }

        [Test]
        public void should_use_file_name_for_source_title_if_scene_name_is_null()
        {
            var game = Builder<Game>.CreateNew().Build();
            var gameFile = Builder<GameFile>.CreateNew()
                                                  .With(f => f.SceneName = null)
                                                  .Build();

            var localGame = new LocalGame()
            {
                Game = game,
                Path = @"C:\Test\Unsorted\Game.2011.mkv"
            };

            var downloadClientItem = new DownloadClientItem
            {
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = DownloadProtocol.Usenet,
                    Id = 1,
                    Name = "sab"
                },
                DownloadId = "abcd"
            };

            Subject.Handle(new GameFileImportedEvent(localGame, gameFile, new List<DeletedGameFile>(), true, downloadClientItem));

            Mocker.GetMock<IHistoryRepository>()
                .Verify(v => v.Insert(It.Is<GameHistory>(h => h.SourceTitle == Path.GetFileNameWithoutExtension(localGame.Path))));
        }

        [Test]
        public void should_insert_grab_failed_history_row_with_reason()
        {
            var game = Builder<Game>.CreateNew().Build();
            var remoteGame = new RemoteGame
            {
                Game = game,
                ParsedGameInfo = new ParsedGameInfo
                {
                    Quality = new QualityModel(Quality.Uplay)
                },
                Release = new ReleaseInfo
                {
                    Title = "A.Game.1998",
                    Indexer = "Some Indexer",
                    IndexerId = 2,
                    Guid = "abcd-guid",
                    Size = 200,
                    DownloadUrl = "http://example.com/download",
                    DownloadProtocol = DownloadProtocol.Torrent,
                    PublishDate = DateTime.UtcNow
                }
            };

            Subject.Handle(new GameGrabFailedEvent(remoteGame, "Indexer returned a 500"));

            // The reason is the whole point of the row: without it this history entry says no
            // more than the log line it replaces.
            Mocker.GetMock<IHistoryRepository>()
                .Verify(v => v.Insert(It.Is<GameHistory>(h =>
                    h.EventType == GameHistoryEventType.GrabFailed &&
                    h.Data["Message"] == "Indexer returned a 500")));
        }
    }
}
