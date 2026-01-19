using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Test.IndexerTests;

namespace NzbDrone.Core.Test.Download.DownloadClientTests
{
    public abstract class DownloadClientFixtureBase<TSubject> : CoreTest<TSubject>
        where TSubject : class, IDownloadClient
    {
        protected readonly string _title = "Cyberpunk.2077.v2.1-GOG";
        protected readonly string _downloadUrl = "http://somewhere.com/Cyberpunk.2077.v2.1-GOG.ext";

        [SetUp]
        public void SetupBase()
        {
            Mocker.GetMock<IConfigService>()
                .SetupGet(s => s.DownloadClientHistoryLimit)
                .Returns(30);

            Mocker.GetMock<IParsingService>()
                .Setup(s => s.Map(It.IsAny<ParsedGameInfo>(), It.IsAny<int>(), It.IsAny<int>(), (SearchCriteriaBase)null))
                .Returns(() => CreateRemoteGame());

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.GetAsync(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), Array.Empty<byte>())));

            Mocker.GetMock<IRemotePathMappingService>()
                .Setup(v => v.RemapRemoteToLocal(It.IsAny<string>(), It.IsAny<OsPath>()))
                .Returns<string, OsPath>((h, r) => r);
        }

        protected virtual RemoteGame CreateRemoteGame()
        {
            var remoteGame = new RemoteGame();
            remoteGame.Release = new ReleaseInfo();
            remoteGame.Release.Title = _title;
            remoteGame.Release.DownloadUrl = _downloadUrl;
            remoteGame.Release.DownloadProtocol = Subject.Protocol;

            remoteGame.ParsedGameInfo = new ParsedGameInfo();

            remoteGame.Game = new Game();

            return remoteGame;
        }

        protected virtual IIndexer CreateIndexer()
        {
            return new TestIndexer(Mocker.Resolve<IHttpClient>(),
                Mocker.Resolve<IIndexerStatusService>(),
                Mocker.Resolve<IConfigService>(),
                Mocker.Resolve<IParsingService>(),
                Mocker.Resolve<Logger>());
        }

        protected void VerifyIdentifiable(DownloadClientItem downloadClientItem)
        {
            downloadClientItem.DownloadClientInfo.Protocol.Should().Be(Subject.Protocol);
            downloadClientItem.DownloadClientInfo.Id.Should().Be(Subject.Definition.Id);
            downloadClientItem.DownloadClientInfo.Name.Should().Be(Subject.Definition.Name);
            downloadClientItem.DownloadId.Should().NotBeNullOrEmpty();
            downloadClientItem.Title.Should().NotBeNullOrEmpty();
        }

        protected void VerifyQueued(DownloadClientItem downloadClientItem)
        {
            VerifyIdentifiable(downloadClientItem);
            downloadClientItem.RemainingSize.Should().NotBe(0);

            // downloadClientItem.RemainingTime.Should().NotBe(TimeSpan.Zero);
            // downloadClientItem.OutputPath.Should().NotBeNullOrEmpty();
            downloadClientItem.Status.Should().Be(DownloadItemStatus.Queued);
        }

        protected void VerifyPaused(DownloadClientItem downloadClientItem)
        {
            VerifyIdentifiable(downloadClientItem);

            downloadClientItem.RemainingSize.Should().NotBe(0);

            // downloadClientItem.RemainingTime.Should().NotBe(TimeSpan.Zero);
            // downloadClientItem.OutputPath.Should().NotBeNullOrEmpty();
            downloadClientItem.Status.Should().Be(DownloadItemStatus.Paused);
        }

        protected void VerifyDownloading(DownloadClientItem downloadClientItem)
        {
            VerifyIdentifiable(downloadClientItem);

            downloadClientItem.RemainingSize.Should().NotBe(0);

            // downloadClientItem.RemainingTime.Should().NotBe(TimeSpan.Zero);
            // downloadClientItem.OutputPath.Should().NotBeNullOrEmpty();
            downloadClientItem.Status.Should().Be(DownloadItemStatus.Downloading);
        }

        protected void VerifyPostprocessing(DownloadClientItem downloadClientItem)
        {
            VerifyIdentifiable(downloadClientItem);

            // downloadClientItem.RemainingTime.Should().NotBe(TimeSpan.Zero);
            // downloadClientItem.OutputPath.Should().NotBeNullOrEmpty();
            downloadClientItem.Status.Should().Be(DownloadItemStatus.Downloading);
        }

        protected void VerifyCompleted(DownloadClientItem downloadClientItem)
        {
            VerifyIdentifiable(downloadClientItem);

            downloadClientItem.Title.Should().NotBeNullOrEmpty();
            downloadClientItem.RemainingSize.Should().Be(0);
            downloadClientItem.RemainingTime.Should().Be(TimeSpan.Zero);

            // downloadClientItem.OutputPath.Should().NotBeNullOrEmpty();
            downloadClientItem.Status.Should().Be(DownloadItemStatus.Completed);
        }

        protected void VerifyWarning(DownloadClientItem downloadClientItem)
        {
            VerifyIdentifiable(downloadClientItem);

            downloadClientItem.Status.Should().Be(DownloadItemStatus.Warning);
        }

        protected void VerifyFailed(DownloadClientItem downloadClientItem)
        {
            VerifyIdentifiable(downloadClientItem);

            downloadClientItem.Status.Should().Be(DownloadItemStatus.Failed);
        }
    }
}
