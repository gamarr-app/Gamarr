using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.TorrentRss;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class TrackedDownloadServiceFixture : CoreTest<TrackedDownloadService>
    {
        [SetUp]
        public void Setup()
        {
        }

        private void GivenDownloadHistory()
        {
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId(It.Is<string>(sr => sr == "35238")))
                .Returns(new List<GameHistory>()
                {
                    new GameHistory()
                    {
                        DownloadId = "35238",
                        SourceTitle = "TV Series S01",
                        GameId = 3,
                    }
                });
        }

        [Test]
        public void should_track_downloads_using_the_source_title_if_it_cannot_be_found_using_the_download_title()
        {
            GivenDownloadHistory();

            var remoteGame = new RemoteGame
            {
                Game = new Game() { Id = 3 },

                ParsedGameInfo = new ParsedGameInfo()
                {
                    GameTitles = new List<string> { "A Game" },
                    Year = 1998
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedGameInfo>(i => i.PrimaryGameTitle == "A Game"), It.IsAny<int>(), It.IsAny<int>(), null))
                  .Returns(remoteGame);

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "A Game 1998",
                DownloadId = "35238",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = client.Protocol,
                    Id = client.Id,
                    Name = client.Name
                }
            };

            var trackedDownload = Subject.TrackDownload(client, item);

            trackedDownload.Should().NotBeNull();
            trackedDownload.RemoteGame.Should().NotBeNull();
            trackedDownload.RemoteGame.Game.Should().NotBeNull();
            trackedDownload.RemoteGame.Game.Id.Should().Be(3);
        }

        [Test]
        public void should_set_indexer()
        {
            var episodeHistory = new GameHistory()
            {
                DownloadId = "35238",
                SourceTitle = "TV Series S01",
                GameId = 3,
                EventType = GameHistoryEventType.Grabbed,
            };
            episodeHistory.Data.Add("indexer", "MyIndexer (Prowlarr)");
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId(It.Is<string>(sr => sr == "35238")))
                .Returns(new List<GameHistory>()
                {
                    episodeHistory
                });

            var indexerDefinition = new IndexerDefinition
            {
                Id = 1,
                Name = "MyIndexer (Prowlarr)",
                Settings = new TorrentRssIndexerSettings { MultiLanguages = new List<int> { Language.Original.Id, Language.French.Id } }
            };
            Mocker.GetMock<IIndexerFactory>()
                .Setup(v => v.Get(indexerDefinition.Id))
                .Returns(indexerDefinition);
            Mocker.GetMock<IIndexerFactory>()
                .Setup(v => v.All())
                .Returns(new List<IndexerDefinition>() { indexerDefinition });

            var remoteEpisode = new RemoteGame
            {
                Game = new Game() { Id = 3 },
                ParsedGameInfo = new ParsedGameInfo()
                {
                    GameTitles = new List<string> { "A Game" },
                    Year = 1998
                }
            };

            Mocker.GetMock<IParsingService>()
                .Setup(s => s.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), null))
                .Returns(remoteEpisode);

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "A Game 1998",
                DownloadId = "35238",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = client.Protocol,
                    Id = client.Id,
                    Name = client.Name
                }
            };

            var trackedDownload = Subject.TrackDownload(client, item);

            trackedDownload.Should().NotBeNull();
            trackedDownload.RemoteGame.Should().NotBeNull();
            trackedDownload.RemoteGame.Release.Should().NotBeNull();
            trackedDownload.RemoteGame.Release.Indexer.Should().Be("MyIndexer (Prowlarr)");
        }

        [Test]
        public void should_unmap_tracked_download_if_game_deleted()
        {
            GivenDownloadHistory();

            var remoteGame = new RemoteGame
            {
                Game = new Game() { Id = 3 },

                ParsedGameInfo = new ParsedGameInfo()
                {
                    GameTitles = { "A Game" },
                    Year = 1998
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), null))
                  .Returns(remoteGame);

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<GameHistory>());

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "A Game 1998",
                DownloadId = "12345",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Id = 1,
                    Type = "Blackhole",
                    Name = "Blackhole Client",
                    Protocol = DownloadProtocol.Torrent
                }
            };

            Subject.TrackDownload(client, item);
            Subject.GetTrackedDownloads().Should().HaveCount(1);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), null))
                  .Returns(default(RemoteGame));

            Subject.Handle(new GamesDeletedEvent(new List<Game> { remoteGame.Game }, false, false));

            var trackedDownloads = Subject.GetTrackedDownloads();
            trackedDownloads.Should().HaveCount(1);
            trackedDownloads.First().RemoteGame.Should().BeNull();
        }

        [Test]
        public void should_not_throw_when_processing_deleted_game()
        {
            GivenDownloadHistory();

            var remoteGame = new RemoteGame
            {
                Game = new Game() { Id = 3 },

                ParsedGameInfo = new ParsedGameInfo()
                {
                    GameTitles = { "A Game" },
                    Year = 1998
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), null))
                  .Returns(default(RemoteGame));

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<GameHistory>());

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "A Game 1998",
                DownloadId = "12345",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Id = 1,
                    Type = "Blackhole",
                    Name = "Blackhole Client",
                    Protocol = DownloadProtocol.Torrent
                }
            };

            Subject.TrackDownload(client, item);
            Subject.GetTrackedDownloads().Should().HaveCount(1);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), null))
                  .Returns(default(RemoteGame));

            Subject.Handle(new GamesDeletedEvent(new List<Game> { remoteGame.Game }, false, false));

            var trackedDownloads = Subject.GetTrackedDownloads();
            trackedDownloads.Should().HaveCount(1);
            trackedDownloads.First().RemoteGame.Should().BeNull();
        }
    }
}
