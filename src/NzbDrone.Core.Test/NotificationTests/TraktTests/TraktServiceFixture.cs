using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Trakt;
using NzbDrone.Core.Notifications.Trakt.Resource;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class TraktServiceFixture : CoreTest<Trakt>
    {
        private DownloadMessage _downloadMessage;
        private NotificationDefinition _traktDefinition;

        [SetUp]
        public void Setup()
        {
            _downloadMessage = new DownloadMessage
            {
                Game = new Game(),
                GameFile = new GameFile
                {
                    MediaInfo = null,
                    Quality = new QualityModel
                    {
                        Quality = Quality.Unknown
                    }
                }
            };

            _traktDefinition = new NotificationDefinition
            {
                Settings = new TraktSettings
                {
                    AccessToken = "",
                    RefreshToken = "",
                    Expires = DateTime.Now.AddDays(1)
                }
            };

            Subject.Definition = _traktDefinition;
        }

        private void GiventValidMediaInfo(Quality quality, string audioChannels, string audioFormat, string scanType, HdrFormat hdrFormat = HdrFormat.None)
        {
            _downloadMessage.GameFile.MediaInfo = new MediaInfoModel
            {
                AudioChannelPositions = audioChannels,
                AudioFormat = audioFormat,
                ScanType = scanType,
                VideoHdrFormat = hdrFormat
            };

            _downloadMessage.GameFile.Quality.Quality = quality;
        }

        [Test]
        public void should_add_collection_game_if_null_mediainfo()
        {
            Subject.OnDownload(_downloadMessage);

            Mocker.GetMock<ITraktProxy>()
                  .Verify(v => v.AddToCollection(It.IsAny<TraktCollectGamesResource>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_add_collection_game_if_valid_mediainfo()
        {
            GiventValidMediaInfo(Quality.Bluray2160p, "5.1", "DTS", "Progressive", HdrFormat.DolbyVisionHdr10);

            Subject.OnDownload(_downloadMessage);

            Mocker.GetMock<ITraktProxy>()
                  .Verify(v => v.AddToCollection(It.Is<TraktCollectGamesResource>(t =>
                    t.Games.First().Audio == "dts" &&
                    t.Games.First().AudioChannels == "5.1" &&
                    t.Games.First().Resolution == "uhd_4k" &&
                    t.Games.First().MediaType == "bluray" &&
                    t.Games.First().Hdr == "hdr10"),
                  It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_format_audio_channels_to_one_decimal_when_adding_collection_game()
        {
            GiventValidMediaInfo(Quality.Bluray2160p, "2.0", "DTS", "Progressive", HdrFormat.DolbyVisionHdr10);

            Subject.OnDownload(_downloadMessage);

            Mocker.GetMock<ITraktProxy>()
                  .Verify(v => v.AddToCollection(It.Is<TraktCollectGamesResource>(t =>
                    t.Games.First().Audio == "dts" &&
                    t.Games.First().AudioChannels == "2.0" &&
                    t.Games.First().Resolution == "uhd_4k" &&
                    t.Games.First().MediaType == "bluray" &&
                    t.Games.First().Hdr == "hdr10"),
                  It.IsAny<string>()), Times.Once());
        }
    }
}
