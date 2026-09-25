using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.MediaCover
{
    public interface IMapCoversToLocal
    {
        void ConvertToLocalUrls(int gameId, IEnumerable<MediaCover> covers, DateTime? added = null);
        string GetCoverPath(int gameId, MediaCoverTypes coverType, int? height = null, int index = 0);
    }

    public class MediaCoverService :
        IHandleAsync<GameUpdatedEvent>,
        IHandleAsync<GamesDeletedEvent>,
        IMapCoversToLocal
    {
        private readonly IMediaCoverProxy _mediaCoverProxy;
        private readonly IImageResizer _resizer;
        private readonly IHttpClient _httpClient;
        private readonly IDiskProvider _diskProvider;
        private readonly ICoverExistsSpecification _coverExistsSpecification;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        private readonly ICached<bool> _coverExistsCache;
        private readonly string _coverRootFolder;

        // Only games added inside this window get their covers stat'ed on disk.
        // The window is load-bearing, not an optimisation: without it every
        // game list request would cost one FileExists per cover per row.
        private static readonly TimeSpan CoverExistsCheckWindow = TimeSpan.FromDays(1);

        // ImageSharp is slow on ARM (no hardware acceleration on mono yet)
        // So limit the number of concurrent resizing tasks
        private static SemaphoreSlim _semaphore = new SemaphoreSlim((int)Math.Ceiling(Environment.ProcessorCount / 2.0));

        public MediaCoverService(IMediaCoverProxy mediaCoverProxy,
                                 IImageResizer resizer,
                                 IHttpClient httpClient,
                                 IDiskProvider diskProvider,
                                 IAppFolderInfo appFolderInfo,
                                 ICoverExistsSpecification coverExistsSpecification,
                                 IConfigFileProvider configFileProvider,
                                 IEventAggregator eventAggregator,
                                 ICacheManager cacheManager,
                                 Logger logger)
        {
            _mediaCoverProxy = mediaCoverProxy;
            _resizer = resizer;
            _httpClient = httpClient;
            _diskProvider = diskProvider;
            _coverExistsSpecification = coverExistsSpecification;
            _configFileProvider = configFileProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;

            _coverExistsCache = cacheManager.GetCache<bool>(GetType(), "coverExists");
            _coverRootFolder = appFolderInfo.GetMediaCoverPath();
        }

        public string GetCoverPath(int gameId, MediaCoverTypes coverType, int? height = null, int index = 0)
        {
            var heightSuffix = height.HasValue ? "-" + height.ToString() : "";

            // Games have multiple screenshots; without an index they all mapped
            // to the same screenshot.jpg (each refresh re-downloaded every one,
            // overwriting the last). The first keeps the unindexed name so
            // existing installs don't re-fetch it.
            var indexSuffix = index >= 2 ? index.ToString() : "";

            return Path.Combine(GetGameCoverPath(gameId), coverType.ToString().ToLower() + indexSuffix + heightSuffix + GetExtension(coverType));
        }

        public void ConvertToLocalUrls(int gameId, IEnumerable<MediaCover> covers, DateTime? added = null)
        {
            if (gameId == 0)
            {
                // Game isn't in Gamarr yet, map via a proxy to circumvent referrer issues
                foreach (var mediaCover in covers)
                {
                    mediaCover.Url = _mediaCoverProxy.RegisterUrl(mediaCover.RemoteUrl);
                }
            }
            else
            {
                var screenshotCount = 0;

                foreach (var mediaCover in covers)
                {
                    if (mediaCover.CoverType == MediaCoverTypes.Unknown)
                    {
                        continue;
                    }

                    // Must match the index assignment in EnsureCovers (both
                    // enumerate the images list in order).
                    var index = mediaCover.CoverType == MediaCoverTypes.Screenshot ? ++screenshotCount : 0;
                    var indexSuffix = index >= 2 ? index.ToString() : "";

                    mediaCover.Url = _configFileProvider.UrlBase + @"/MediaCover/" + gameId + "/" + mediaCover.CoverType.ToString().ToLower() + indexSuffix + GetExtension(mediaCover.CoverType);

                    // Hash of the source url busts browser caches when the
                    // remote image changes (upstream Radarr 69f8cea). Only
                    // append it once the file is actually on disk: the hash
                    // never changes again, so tagging a not-yet-downloaded
                    // cover makes the browser cache the 404 forever.
                    if (mediaCover.RemoteUrl.IsNotNullOrWhiteSpace() && CoverExists(gameId, mediaCover.CoverType, index, added))
                    {
                        mediaCover.Url += "?h=" + mediaCover.RemoteUrl.SHA256Hash()[..20];
                    }
                }
            }
        }

        private bool CoverExists(int gameId, MediaCoverTypes coverType, int index, DateTime? added)
        {
            // Covers for anything older than the window are assumed downloaded,
            // so the common case stays free of disk access entirely.
            if (!IsRecentlyAdded(added))
            {
                return true;
            }

            var filePath = GetCoverPath(gameId, coverType, null, index);

            return _coverExistsCache.Get(filePath, () => _diskProvider.FileExists(filePath));
        }

        private static bool IsRecentlyAdded(DateTime? added)
        {
            return added > DateTime.UtcNow - CoverExistsCheckWindow;
        }

        private void RemoveCoverExistsCache(Game game)
        {
            var screenshotCount = 0;

            foreach (var cover in game.GameMetadata.Value.Images)
            {
                // Same per-screenshot index as ConvertToLocalUrls/EnsureCovers,
                // since the cache is keyed by the resulting file path.
                var index = cover.CoverType == MediaCoverTypes.Screenshot ? ++screenshotCount : 0;

                _coverExistsCache.Remove(GetCoverPath(game.Id, cover.CoverType, null, index));
            }
        }

        private string GetGameCoverPath(int gameId)
        {
            return Path.Combine(_coverRootFolder, gameId.ToString());
        }

        private bool EnsureCovers(Game game)
        {
            var updated = false;
            var toResize = new List<Tuple<MediaCover, bool, int>>();
            var downloadedTypes = new HashSet<MediaCoverTypes>();
            var screenshotCount = 0;

            foreach (var cover in game.GameMetadata.Value.Images)
            {
                if (cover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                // Skip if we already have this cover type (except screenshots which can have multiple)
                if (cover.CoverType != MediaCoverTypes.Screenshot && downloadedTypes.Contains(cover.CoverType))
                {
                    continue;
                }

                // Per-screenshot index; must match ConvertToLocalUrls.
                var index = cover.CoverType == MediaCoverTypes.Screenshot ? ++screenshotCount : 0;

                var fileName = GetCoverPath(game.Id, cover.CoverType, null, index);
                var alreadyExists = false;

                try
                {
                    alreadyExists = _coverExistsSpecification.AlreadyExists(cover.RemoteUrl, fileName);

                    if (!alreadyExists)
                    {
                        DownloadCover(game, cover, fileName);
                        updated = true;
                    }

                    // Mark this type as successfully handled (either existed or downloaded)
                    downloadedTypes.Add(cover.CoverType);

                    // The file is on disk now, so let ConvertToLocalUrls start
                    // hashing it without waiting for the cache entry to expire.
                    if (IsRecentlyAdded(game.Added))
                    {
                        _coverExistsCache.Set(fileName, true);
                    }
                }
                catch (HttpException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", game, e.Message);
                }
                catch (WebException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", game, e.Message);
                }
                catch (HttpRequestException e)
                {
                    // A connect or TLS failure never reaches the WebException arm above, so an
                    // unreachable cover host was logged at Error and shipped to Sentry.
                    _logger.Warn("Couldn't download media cover for {0}. {1}", game, e.Message);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't download media cover for {0}", game);
                }

                toResize.Add(Tuple.Create(cover, alreadyExists, index));
            }

            try
            {
                _semaphore.Wait();

                foreach (var tuple in toResize)
                {
                    EnsureResizedCovers(game, tuple.Item1, !tuple.Item2, tuple.Item3);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return updated;
        }

        private void DownloadCover(Game game, MediaCover cover, string fileName)
        {
            _logger.Info("Downloading {0} for {1} {2}", cover.CoverType, game, cover.RemoteUrl);
            _httpClient.DownloadFile(cover.RemoteUrl, fileName);
        }

        private void EnsureResizedCovers(Game game, MediaCover cover, bool forceResize, int index = 0)
        {
            int[] heights;

            switch (cover.CoverType)
            {
                default:
                    return;

                case MediaCoverTypes.Poster:
                case MediaCoverTypes.Headshot:
                    heights = new[] { 500, 250 };
                    break;

                case MediaCoverTypes.Banner:
                    heights = new[] { 70, 35 };
                    break;

                case MediaCoverTypes.Fanart:
                case MediaCoverTypes.Screenshot:
                    heights = new[] { 360, 180 };
                    break;
            }

            foreach (var height in heights)
            {
                var mainFileName = GetCoverPath(game.Id, cover.CoverType, null, index);
                var resizeFileName = GetCoverPath(game.Id, cover.CoverType, height, index);

                if (forceResize || !_diskProvider.FileExists(resizeFileName) || _diskProvider.GetFileSize(resizeFileName) == 0)
                {
                    _logger.Debug("Resizing {0}-{1} for {2}", cover.CoverType, height, game);

                    try
                    {
                        _resizer.Resize(mainFileName, resizeFileName, height);
                    }
                    catch
                    {
                        _logger.Debug("Couldn't resize media cover {0}-{1} for {2}, using full size image instead.", cover.CoverType, height, game);
                    }
                }
            }
        }

        private string GetExtension(MediaCoverTypes coverType)
        {
            return coverType switch
            {
                MediaCoverTypes.Clearlogo => ".png",
                _ => ".jpg"
            };
        }

        public void HandleAsync(GameUpdatedEvent message)
        {
            var updated = EnsureCovers(message.Game);

            _eventAggregator.PublishEvent(new MediaCoversUpdatedEvent(message.Game, updated));
        }

        public void HandleAsync(GamesDeletedEvent message)
        {
            foreach (var game in message.Games)
            {
                RemoveCoverExistsCache(game);

                var path = GetGameCoverPath(game.Id);
                if (_diskProvider.FolderExists(path))
                {
                    _diskProvider.DeleteFolder(path, true);
                }
            }
        }
    }
}
