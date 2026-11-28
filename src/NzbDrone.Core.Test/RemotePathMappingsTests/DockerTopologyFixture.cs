using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.QBittorrent;
using NzbDrone.Core.Download.Clients.Sabnzbd;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Games;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.VirusScanning;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.RemotePathMappingsTests
{
    // How nearly every real install runs: Gamarr in the Docker image on a NAS,
    // the download client in a sibling container. The client reports paths from
    // its own filesystem view (/downloads/complete/X, or D:\Downloads\Complete\X
    // for a Windows client), Gamarr sees the same directory somewhere else
    // (/data/downloads/complete/X), and a Remote Path Mapping bridges the two.
    //
    // These tests use the REAL RemotePathMappingService (normalisation on add,
    // matching on read) wired into the real client classes and the real
    // CompletedDownloadService / DownloadedGameImportService, with only the
    // network proxies and the (heavy) import decision pieces mocked.
    public abstract class DockerTopologyFixtureBase<TSubject> : CoreTest<TSubject>
        where TSubject : class
    {
        protected const string ClientHost = "download-client";

        // What the client calls the directory vs. what Gamarr's container calls it.
        protected const string ClientComplete = "/downloads/complete";
        protected const string GamarrComplete = "/data/downloads/complete";

        private List<RemotePathMapping> _stored;

        protected RemotePathMappingService Mapping { get; private set; }

        [SetUp]
        public void SetupTopology()
        {
            // Docker is Linux; every assertion here is about Linux paths.
            PosixOnly();

            _stored = new List<RemotePathMapping>();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(true);

            Mocker.GetMock<IRemotePathMappingRepository>()
                  .Setup(s => s.All())
                  .Returns(() => _stored.ToList());

            Mocker.GetMock<IRemotePathMappingRepository>()
                  .Setup(s => s.Insert(It.IsAny<RemotePathMapping>()))
                  .Returns<RemotePathMapping>(m =>
                  {
                      _stored.Add(m);
                      return m;
                  });

            Mapping = Mocker.Resolve<RemotePathMappingService>();
            Mocker.SetConstant<IRemotePathMappingService>(Mapping);
        }

        // A trailing separator is not a different directory.
        protected static void AssertSamePath(OsPath actual, string expected)
        {
            actual.Equals(new OsPath(expected), true).Should().BeTrue("expected {0} but was {1}", expected, actual);
        }

        // Goes through Add so the same normalisation a user's mapping gets
        // (trailing separators, slashes, whitespace) is part of every test.
        protected void GivenMapping(string remotePath, string localPath, string host = ClientHost)
        {
            Mapping.Add(new RemotePathMapping { Host = host, RemotePath = remotePath, LocalPath = localPath });
        }
    }

    [TestFixture]
    public class DockerTopologyQBittorrentFixture : DockerTopologyFixtureBase<QBittorrent>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new DownloadClientDefinition
            {
                Name = "qBittorrent",
                Settings = new QBittorrentSettings
                {
                    Host = ClientHost,
                    Port = 8080,
                    GameCategory = "games-gamarr"
                }
            };

            Mocker.GetMock<IQBittorrentProxySelector>()
                  .Setup(s => s.GetProxy(It.IsAny<QBittorrentSettings>(), It.IsAny<bool>()))
                  .Returns(Mocker.GetMock<IQBittorrentProxy>().Object);

            Mocker.GetMock<IQBittorrentProxy>()
                  .Setup(s => s.GetApiVersion(It.IsAny<QBittorrentSettings>()))
                  .Returns(new Version(2, 11, 4));

            Mocker.GetMock<IQBittorrentProxy>()
                  .Setup(s => s.GetConfig(It.IsAny<QBittorrentSettings>()))
                  .Returns(new QBittorrentPreferences { DhtEnabled = true });

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.DownloadClientHistoryLimit)
                  .Returns(30);

            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("localized");
        }

        private DownloadClientItem GivenCompletedTorrent(string savePath, string contentPath, string rootPath = null)
        {
            var torrent = new QBittorrentTorrent
            {
                Hash = "CBC2F069FE8BB2F544EAE707D75BCD3DE9DCF951",
                Name = "Game.Title.v1.0-GROUP",
                Size = 1000,
                Progress = 1.0,
                State = "stalledUP",
                Label = "games-gamarr",
                SavePath = savePath,
                ContentPath = contentPath,
                RootPath = rootPath
            };

            Mocker.GetMock<IQBittorrentProxy>()
                  .Setup(s => s.GetTorrents(It.IsAny<QBittorrentSettings>()))
                  .Returns(new List<QBittorrentTorrent> { torrent });

            return Subject.GetItems().Single();
        }

        [Test]
        public void folder_release_is_remapped_into_the_gamarr_view()
        {
            GivenMapping(ClientComplete, GamarrComplete);

            var item = GivenCompletedTorrent(ClientComplete, ClientComplete + "/Game.Title.v1.0-GROUP");

            item.Status.Should().Be(DownloadItemStatus.Completed);
            AssertSamePath(item.OutputPath, GamarrComplete + "/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void root_path_of_a_wrapped_single_file_is_remapped()
        {
            // qBittorrent 5: content_path is the .iso, root_path the folder around it.
            GivenMapping(ClientComplete, GamarrComplete);

            var item = GivenCompletedTorrent(
                ClientComplete,
                ClientComplete + "/Game.Title.v1.0-GROUP/game.iso",
                ClientComplete + "/Game.Title.v1.0-GROUP");

            AssertSamePath(item.OutputPath, GamarrComplete + "/Game.Title.v1.0-GROUP");
        }

        [TestCase("/downloads/complete", "/data/downloads/complete")]
        [TestCase("/downloads/complete/", "/data/downloads/complete")]
        [TestCase("/downloads/complete", "/data/downloads/complete/")]
        [TestCase("/downloads/complete/", "/data/downloads/complete/")]
        public void trailing_slash_on_either_side_of_the_mapping_makes_no_difference(string remote, string local)
        {
            GivenMapping(remote, local);

            var item = GivenCompletedTorrent("/downloads/complete/", "/downloads/complete/Game.Title.v1.0-GROUP/");

            AssertSamePath(item.OutputPath, GamarrComplete + "/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void trailing_slash_on_a_client_reported_save_path_is_ignored()
        {
            GivenMapping(ClientComplete, GamarrComplete);

            var item = GivenCompletedTorrent(ClientComplete + "/", ClientComplete + "/Game.Title.v1.0-GROUP");

            item.Status.Should().Be(DownloadItemStatus.Completed);
            AssertSamePath(item.OutputPath, GamarrComplete + "/Game.Title.v1.0-GROUP");
        }

        [TestCase(@"D:\Downloads\Complete\", @"D:\Downloads\Complete\Game.Title.v1.0-GROUP")]
        [TestCase(@"D:\Downloads\Complete", @"D:\Downloads\Complete\Game.Title.v1.0-GROUP")]
        [TestCase(@"D:\Downloads\Complete", "D:/Downloads/Complete/Game.Title.v1.0-GROUP")]
        public void windows_client_path_is_mapped_to_a_linux_path(string remote, string contentPath)
        {
            GivenMapping(remote, GamarrComplete);

            var item = GivenCompletedTorrent(@"D:\Downloads\Complete", contentPath);

            AssertSamePath(item.OutputPath, GamarrComplete + "/Game.Title.v1.0-GROUP");
            item.OutputPath.IsUnixPath.Should().BeTrue();
        }

        [Test]
        public void windows_client_path_without_a_mapping_stays_a_windows_path()
        {
            var item = GivenCompletedTorrent(@"D:\Downloads\Complete", @"D:\Downloads\Complete\Game.Title.v1.0-GROUP");

            item.OutputPath.IsWindowsPath.Should().BeTrue();
            item.OutputPath.IsUnixPath.Should().BeFalse();
        }

        [Test]
        public void unmapped_client_path_is_passed_through_untouched()
        {
            // The client's own path is a perfectly valid Linux path, so nothing
            // here can tell it is wrong. Only the import step finds out (see the
            // pipeline fixture below).
            var item = GivenCompletedTorrent(ClientComplete, ClientComplete + "/Game.Title.v1.0-GROUP");

            AssertSamePath(item.OutputPath, ClientComplete + "/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void mapping_for_a_different_client_host_does_not_apply()
        {
            GivenMapping(ClientComplete, GamarrComplete, "another-client");

            var item = GivenCompletedTorrent(ClientComplete, ClientComplete + "/Game.Title.v1.0-GROUP");

            AssertSamePath(item.OutputPath, ClientComplete + "/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void import_item_for_an_older_client_is_remapped_too()
        {
            // qBittorrent < 2.6.1 reports no content path; GetImportItem builds it
            // from save path + first file component and must remap that as well.
            GivenMapping(ClientComplete, GamarrComplete);

            Mocker.GetMock<IQBittorrentProxy>()
                  .Setup(s => s.GetTorrentFiles(It.IsAny<string>(), It.IsAny<QBittorrentSettings>()))
                  .Returns(new List<QBittorrentTorrentFile>
                  {
                      new QBittorrentTorrentFile { Name = "Game.Title.v1.0-GROUP/disc1/game.iso" }
                  });

            Mocker.GetMock<IQBittorrentProxy>()
                  .Setup(s => s.GetTorrentProperties(It.IsAny<string>(), It.IsAny<QBittorrentSettings>()))
                  .Returns(new QBittorrentTorrentProperties { SavePath = ClientComplete + "/" });

            var item = new DownloadClientItem { DownloadId = "CBC2F069FE8BB2F544EAE707D75BCD3DE9DCF951", Title = "Game.Title.v1.0-GROUP" };

            var result = Subject.GetImportItem(item, null);

            AssertSamePath(result.OutputPath, GamarrComplete + "/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void status_reports_the_download_root_in_the_gamarr_view()
        {
            // The health check compares this against what Gamarr can actually see.
            GivenMapping(ClientComplete, GamarrComplete);

            Mocker.GetMock<IQBittorrentProxy>()
                  .Setup(s => s.GetConfig(It.IsAny<QBittorrentSettings>()))
                  .Returns(new QBittorrentPreferences { SavePath = ClientComplete + "/" });

            Mocker.GetMock<IQBittorrentProxy>()
                  .Setup(s => s.GetLabels(It.IsAny<QBittorrentSettings>()))
                  .Returns(new Dictionary<string, QBittorrentLabel>
                  {
                      { "games-gamarr", new QBittorrentLabel { Name = "games-gamarr", SavePath = ClientComplete + "/games" } }
                  });

            var status = Subject.GetStatus();

            status.IsLocalhost.Should().BeFalse();
            status.OutputRootFolders.Should().ContainSingle();
            AssertSamePath(status.OutputRootFolders.Single(), GamarrComplete + "/games");
        }
    }

    [TestFixture]
    public class DockerTopologySabnzbdFixture : DockerTopologyFixtureBase<Sabnzbd>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new DownloadClientDefinition
            {
                Name = "SABnzbd",
                Settings = new SabnzbdSettings
                {
                    Host = ClientHost,
                    Port = 8080,
                    ApiKey = "5c770e3197e4fe763423ee7c392c25d1",
                    GameCategory = "games"
                }
            };

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.DownloadClientHistoryLimit)
                  .Returns(30);

            Mocker.GetMock<ISabnzbdProxy>()
                  .Setup(s => s.GetVersion(It.IsAny<SabnzbdSettings>()))
                  .Returns("4.3.0");

            Mocker.GetMock<ISabnzbdProxy>()
                  .Setup(s => s.GetQueue(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SabnzbdSettings>()))
                  .Returns(new SabnzbdQueue { Items = new List<SabnzbdQueueItem>() });
        }

        private DownloadClientItem GivenCompletedNzb(string storage)
        {
            Mocker.GetMock<ISabnzbdProxy>()
                  .Setup(s => s.GetHistory(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SabnzbdSettings>()))
                  .Returns(new SabnzbdHistory
                  {
                      Items = new List<SabnzbdHistoryItem>
                      {
                          new SabnzbdHistoryItem
                          {
                              Status = SabnzbdDownloadStatus.Completed,
                              Size = 1000,
                              Category = "games",
                              Id = "SABnzbd_nzo_abc",
                              Title = "Game.Title.v1.0-GROUP",
                              Storage = storage
                          }
                      }
                  });

            return Subject.GetItems().Single();
        }

        [Test]
        public void folder_release_is_remapped_into_the_gamarr_view()
        {
            GivenMapping(ClientComplete, GamarrComplete);

            var item = GivenCompletedNzb(ClientComplete + "/games/Game.Title.v1.0-GROUP");

            AssertSamePath(item.OutputPath, GamarrComplete + "/games/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void storage_pointing_inside_the_release_folder_resolves_to_the_folder()
        {
            GivenMapping(ClientComplete, GamarrComplete);

            var item = GivenCompletedNzb(ClientComplete + "/games/Game.Title.v1.0-GROUP/disc1");

            AssertSamePath(item.OutputPath, GamarrComplete + "/games/Game.Title.v1.0-GROUP");
        }

        [TestCase("/downloads/complete", "/data/downloads/complete")]
        [TestCase("/downloads/complete/", "/data/downloads/complete")]
        [TestCase("/downloads/complete", "/data/downloads/complete/")]
        [TestCase("/downloads/complete/", "/data/downloads/complete/")]
        public void trailing_slash_on_either_side_of_the_mapping_makes_no_difference(string remote, string local)
        {
            GivenMapping(remote, local);

            var item = GivenCompletedNzb(ClientComplete + "/games/Game.Title.v1.0-GROUP/");

            AssertSamePath(item.OutputPath, GamarrComplete + "/games/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void windows_client_path_is_mapped_to_a_linux_path()
        {
            GivenMapping(@"D:\Downloads\Complete\", GamarrComplete);

            var item = GivenCompletedNzb(@"D:\Downloads\Complete\games\Game.Title.v1.0-GROUP");

            AssertSamePath(item.OutputPath, GamarrComplete + "/games/Game.Title.v1.0-GROUP");
        }

        [Test]
        public void unmapped_windows_client_path_stays_a_windows_path()
        {
            var item = GivenCompletedNzb(@"D:\Downloads\Complete\games\Game.Title.v1.0-GROUP");

            item.OutputPath.IsWindowsPath.Should().BeTrue();
        }

        [Test]
        public void status_reports_the_category_folder_in_the_gamarr_view()
        {
            GivenMapping(ClientComplete, GamarrComplete);

            Mocker.GetMock<ISabnzbdProxy>()
                  .Setup(s => s.GetConfig(It.IsAny<SabnzbdSettings>()))
                  .Returns(new SabnzbdConfig
                  {
                      Misc = new SabnzbdConfigMisc { complete_dir = ClientComplete },
                      Categories = new List<SabnzbdCategory> { new SabnzbdCategory { Name = "games", Dir = "games" } }
                  });

            var status = Subject.GetStatus();

            status.OutputRootFolders.Should().ContainSingle();
            AssertSamePath(status.OutputRootFolders.Single(), GamarrComplete + "/games");
        }
    }

    // The tracked download -> import step, on a real directory tree standing in
    // for the /data volume, fed by paths the real mapping service produced.
    [TestFixture]
    public class DockerTopologyImportPipelineFixture : DockerTopologyFixtureBase<CompletedDownloadService>
    {
        private string _dataRoot;
        private Game _game;
        private TrackedDownload _trackedDownload;
        private List<string> _decisionPaths;
        private bool? _decisionWasFolderImport;

        [SetUp]
        public void Setup()
        {
            _dataRoot = Path.Combine(TempFolder, "data");
            Directory.CreateDirectory(Path.Combine(_dataRoot, "downloads", "complete"));
            Directory.CreateDirectory(Path.Combine(_dataRoot, "games"));

            _game = Builder<Game>.CreateNew()
                .With(g => g.Path = Path.Combine(_dataRoot, "games", "Game Title"))
                .With(g => g.GameMetadata = new GameMetadata { Title = "Game Title" })
                .Build();

            // Real behaviour for the two disk questions the import asks.
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(It.IsAny<string>())).Returns<string>(Directory.Exists);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns<string, bool>((p, r) => Directory.GetFiles(p, "*", r ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));

            Mocker.GetMock<IDiskScanService>()
                  .Setup(s => s.GetVideoFiles(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(Array.Empty<string>());

            Mocker.GetMock<IVirusScannerService>().Setup(s => s.IsAvailable).Returns(false);

            Mocker.GetMock<IReleaseStructureValidator>()
                  .Setup(s => s.ValidateReleaseStructure(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(new ReleaseStructureValidationResult
                  {
                      IsValid = true,
                      SuspiciousFiles = new List<string>(),
                      Confidence = ReleaseStructureConfidence.Unknown
                  });

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<GameHistory>());

            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GamePathExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetGame(It.IsAny<string>()))
                  .Returns(_game);

            Mocker.GetMock<IProvideImportItemService>()
                  .Setup(s => s.ProvideImportItem(It.IsAny<DownloadClientItem>(), It.IsAny<DownloadClientItem>()))
                  .Returns<DownloadClientItem, DownloadClientItem>((i, p) => i);

            // The decision maker and importer are exercised by their own fixtures;
            // here they just record what path the pipeline handed them.
            _decisionPaths = new List<string>();
            _decisionWasFolderImport = null;

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<string>>(), It.IsAny<Game>(), It.IsAny<DownloadClientItem>(), It.IsAny<ParsedGameInfo>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .Returns((List<string> paths, Game g, DownloadClientItem i, ParsedGameInfo p, bool folder, bool f2) =>
                  {
                      _decisionPaths.AddRange(paths);
                      _decisionWasFolderImport = folder;
                      return paths.Select(path => new ImportDecision(new LocalGame { Path = path, Game = _game })).ToList();
                  });

            Mocker.GetMock<IImportApprovedGame>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.IsAny<DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns<List<ImportDecision>, bool, DownloadClientItem, ImportMode>((d, n, i, m) => d.Select(x => new ImportResult(x)).ToList());

            // Real service chain: CompletedDownloadService -> DownloadedGameImportService.
            Mocker.SetConstant<IDownloadedGameImportService>(Mocker.Resolve<DownloadedGameImportService>());
        }

        private string DownloadFolderOnDisk(string name)
        {
            var folder = Path.Combine(_dataRoot, "downloads", "complete", name);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "setup.exe"), "x");
            File.WriteAllText(Path.Combine(folder, "game.iso"), "x");
            return folder;
        }

        private TrackedDownload GivenTrackedDownload(OsPath outputPath)
        {
            var item = new DownloadClientItem
            {
                DownloadId = "CBC2F069FE8BB2F544EAE707D75BCD3DE9DCF951",
                Title = "Game.Title.v1.0-GROUP",
                Status = DownloadItemStatus.Completed,
                CanMoveFiles = true,
                OutputPath = outputPath,
                DownloadClientInfo = new DownloadClientItemClientInfo { Name = "qBittorrent" }
            };

            Mocker.GetMock<IDownloadClient>()
                  .SetupGet(c => c.Definition)
                  .Returns(new DownloadClientDefinition { Id = 1, Name = "qBittorrent" });

            _trackedDownload = new TrackedDownload
            {
                DownloadItem = item,
                RemoteGame = new RemoteGame { Game = _game },
                State = TrackedDownloadState.ImportPending
            };

            return _trackedDownload;
        }

        [Test]
        public void folder_release_reported_by_the_client_is_imported_from_the_mapped_folder()
        {
            var folder = DownloadFolderOnDisk("Game.Title.v1.0-GROUP");

            // Client says /downloads/complete/X; Gamarr's view of that is <data>/downloads/complete/X.
            GivenMapping(ClientComplete, Path.Combine(_dataRoot, "downloads", "complete"));
            var outputPath = Mapping.RemapRemoteToLocal(ClientHost, new OsPath(ClientComplete + "/Game.Title.v1.0-GROUP"));

            outputPath.FullPath.Should().Be(folder);

            var tracked = GivenTrackedDownload(outputPath);

            Subject.Import(tracked);

            _decisionPaths.Should().Equal(folder);
            _decisionWasFolderImport.Should().BeTrue("a folder release is imported as a whole, not file by file");
            tracked.State.Should().Be(TrackedDownloadState.Imported);
            tracked.StatusMessages.Should().BeEmpty();

            // The importer got the download item with the mapped folder, so it can
            // move (or hardlink, per media management) out of it into the root folder.
            Mocker.GetMock<IImportApprovedGame>()
                  .Verify(v => v.Import(It.IsAny<List<ImportDecision>>(), It.IsAny<bool>(), It.Is<DownloadClientItem>(i => i.OutputPath.Equals(new OsPath(folder), true)), ImportMode.Auto), Times.Once());
        }

        [Test]
        public void windows_client_folder_release_is_imported_from_the_mapped_folder()
        {
            var folder = DownloadFolderOnDisk("Game.Title.v1.0-GROUP");

            GivenMapping(@"D:\Downloads\Complete\", Path.Combine(_dataRoot, "downloads", "complete"));
            var outputPath = Mapping.RemapRemoteToLocal(ClientHost, new OsPath(@"D:\Downloads\Complete\Game.Title.v1.0-GROUP"));

            var tracked = GivenTrackedDownload(outputPath);

            Subject.Import(tracked);

            _decisionPaths.Should().Equal(folder);
            tracked.State.Should().Be(TrackedDownloadState.Imported);
        }

        [Test]
        public void trailing_slash_mismatch_still_imports()
        {
            var folder = DownloadFolderOnDisk("Game.Title.v1.0-GROUP");

            // Mapping typed without a trailing slash, client reports one on the folder.
            GivenMapping(ClientComplete, Path.Combine(_dataRoot, "downloads", "complete") + "/");
            var outputPath = Mapping.RemapRemoteToLocal(ClientHost, new OsPath(ClientComplete + "/Game.Title.v1.0-GROUP/"));

            var tracked = GivenTrackedDownload(outputPath);

            Subject.Import(tracked);

            _decisionPaths.Select(p => p.TrimEnd((char)47)).Should().Equal(folder);
            tracked.State.Should().Be(TrackedDownloadState.Imported);
        }

        [Test]
        public void windows_client_path_without_a_mapping_is_blocked_with_a_remote_path_mapping_hint()
        {
            var tracked = GivenTrackedDownload(new OsPath(@"D:\Downloads\Complete\Game.Title.v1.0-GROUP"));

            Subject.Import(tracked);

            tracked.StatusMessages.Should().ContainSingle()
                   .Which.Messages.Should().ContainSingle(m => m.Contains("Remote Path Mapping"));

            _decisionPaths.Should().BeEmpty();
            tracked.State.Should().NotBe(TrackedDownloadState.Imported);
        }

        [Test]
        public void client_path_that_does_not_exist_in_the_gamarr_view_is_reported_not_imported()
        {
            // The classic Docker mistake: no mapping at all. /downloads/complete/X
            // is a valid-looking Linux path, but it is not on Gamarr's filesystem.
            var tracked = GivenTrackedDownload(new OsPath(ClientComplete + "/Game.Title.v1.0-GROUP"));

            Subject.Import(tracked);

            _decisionPaths.Should().BeEmpty();
            tracked.State.Should().Be(TrackedDownloadState.ImportPending);
            tracked.StatusMessages.Should().ContainSingle()
                   .Which.Messages.Should().ContainSingle(m => m.Contains(ClientComplete + "/Game.Title.v1.0-GROUP"));

            // DownloadedGameImportService logs the "not accessible by Gamarr" error, which is
            // what feeds the Remote Path Mapping health check on ImportFailed.
            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
