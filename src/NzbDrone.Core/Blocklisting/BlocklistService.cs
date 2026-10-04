using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistService
    {
        bool Blocklisted(int gameId, ReleaseInfo release);
        bool BlocklistedTorrentHash(int gameId, string hash);
        PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec);
        List<Blocklist> GetByGameId(int gameId);
        void Block(RemoteGame remoteGame, string message);
        void Delete(int id);
        void Delete(List<int> ids);
    }

    public class BlocklistService : IBlocklistService,

                                    IExecute<ClearBlocklistCommand>,
                                    IHandle<DownloadFailedEvent>,
                                    IHandleAsync<GamesDeletedEvent>
    {
        private readonly IBlocklistRepository _blocklistRepository;

        public BlocklistService(IBlocklistRepository blocklistRepository)
        {
            _blocklistRepository = blocklistRepository;
        }

        public bool Blocklisted(int gameId, ReleaseInfo release)
        {
            if (release.DownloadProtocol == DownloadProtocol.Torrent)
            {
                if (release is not TorrentInfo torrentInfo)
                {
                    return false;
                }

                // A hash hit is authoritative and deliberately ignores the title and the
                // indexer: the same torrent re-offered under a second indexer label (the
                // same infohash arriving as "1337x" after it was blocklisted as
                // "BigFANGroup") is the same bad download and must stay blocked.
                if (torrentInfo.InfoHash.IsNotNullOrWhiteSpace() &&
                    _blocklistRepository.BlocklistedByTorrentInfoHash(gameId, torrentInfo.InfoHash)
                                        .Any(b => SameTorrent(b, torrentInfo)))
                {
                    return true;
                }

                // Fall through on a miss rather than returning false. Rows with no stored
                // hash — pending releases blocklisted before a .torrent was ever fetched,
                // and everything written before the hash was recorded — are invisible to
                // the hash query, so returning early there would silently un-blocklist
                // them the moment an incoming release happened to carry a hash.
                return _blocklistRepository.BlocklistedByTitle(gameId, release.Title)
                    .Where(b => b.Protocol == DownloadProtocol.Torrent)
                    .Any(b => SameTorrent(b, torrentInfo));
            }

            return _blocklistRepository.BlocklistedByTitle(gameId, release.Title)
                .Where(b => b.Protocol == DownloadProtocol.Usenet)
                .Any(b => SameNzb(b, release));
        }

        public bool BlocklistedTorrentHash(int gameId, string hash)
        {
            return _blocklistRepository.BlocklistedByTorrentInfoHash(gameId, hash).Any(b =>
                b.TorrentInfoHash.Equals(hash, StringComparison.InvariantCultureIgnoreCase));
        }

        public PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec)
        {
            return _blocklistRepository.GetPaged(pagingSpec);
        }

        public List<Blocklist> GetByGameId(int gameId)
        {
            return _blocklistRepository.BlocklistedByGame(gameId);
        }

        public void Block(RemoteGame remoteGame, string message)
        {
            var blocklist = new Blocklist
                            {
                                GameId = remoteGame.Game.Id,
                                SourceTitle =  remoteGame.Release.Title,
                                Quality = remoteGame.ParsedGameInfo.Quality,
                                Date = DateTime.UtcNow,
                                PublishedDate = remoteGame.Release.PublishDate,
                                Size = remoteGame.Release.Size,
                                Indexer = remoteGame.Release.Indexer,
                                Protocol = remoteGame.Release.DownloadProtocol,
                                Message = message,
                                Languages = remoteGame.ParsedGameInfo.Languages
                            };

            if (remoteGame.Release is TorrentInfo torrentRelease)
            {
                blocklist.TorrentInfoHash = torrentRelease.InfoHash;
            }

            _blocklistRepository.Insert(blocklist);
        }

        public void Delete(int id)
        {
            _blocklistRepository.Delete(id);
        }

        public void Delete(List<int> ids)
        {
            _blocklistRepository.DeleteMany(ids);
        }

        private bool SameNzb(Blocklist item, ReleaseInfo release)
        {
            if (item.PublishedDate == release.PublishDate)
            {
                return true;
            }

            if (!HasSameIndexer(item, release.Indexer) &&
                HasSamePublishedDate(item, release.PublishDate) &&
                HasSameSize(item, release.Size))
            {
                return true;
            }

            return false;
        }

        private bool SameTorrent(Blocklist item, TorrentInfo release)
        {
            // Two known, different hashes are two different torrents, whatever the title
            // says. This is the half that stops a blocklisted release from blocking every
            // later release of the same game from the same indexer.
            if (release.InfoHash.IsNotNullOrWhiteSpace() && item.TorrentInfoHash.IsNotNullOrWhiteSpace())
            {
                return release.InfoHash.Equals(item.TorrentInfoHash, StringComparison.InvariantCultureIgnoreCase);
            }

            // Either side's hash is unknown, so title + indexer is all we have. Requiring
            // a hash match here instead would un-blocklist every stored row that has no
            // hash, which is the whole pre-existing blocklist on any upgraded instance.
            if (HasSameIndexer(item, release.Indexer))
            {
                return true;
            }

            // No hash to compare and a different indexer label. The source title already
            // matched exactly for this same game, so an equal size means this is the same
            // release re-offered under a second indexer name -- which is how one bad
            // torrent kept getting re-grabbed after being blocklisted. A genuinely
            // different torrent of the same title differs in size, and a row with no
            // recorded size stays indexer-scoped rather than becoming a wildcard.
            return item.Size.HasValue && HasSameSize(item, release.Size);
        }

        private bool HasSameIndexer(Blocklist item, string indexer)
        {
            // A blank stored indexer stays a wildcard. Rows reach that state legitimately
            // (manual blocklists, and indexers that report no name), and treating blank as
            // "matches nothing" would make those rows dead weight that never blocks
            // anything. It is only reached when at least one side has no infohash, so the
            // hash check above already keeps it away from the cases it used to over-block.
            if (item.Indexer.IsNullOrWhiteSpace())
            {
                return true;
            }

            return item.Indexer.Equals(indexer, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool HasSamePublishedDate(Blocklist item, DateTime publishedDate)
        {
            if (!item.PublishedDate.HasValue)
            {
                return true;
            }

            return item.PublishedDate.Value.AddMinutes(-2) <= publishedDate &&
                   item.PublishedDate.Value.AddMinutes(2) >= publishedDate;
        }

        private bool HasSameSize(Blocklist item, long size)
        {
            if (!item.Size.HasValue)
            {
                return true;
            }

            var difference = Math.Abs(item.Size.Value - size);

            return difference <= 2.Megabytes();
        }

        public void Execute(ClearBlocklistCommand message)
        {
            _blocklistRepository.Purge();
        }

        public void Handle(DownloadFailedEvent message)
        {
            var protocol = (DownloadProtocol)Convert.ToInt32(message.Data.GetValueOrDefault("protocol"));

            var blocklist = new Blocklist
            {
                GameId = message.GameId,
                SourceTitle = message.SourceTitle,
                Quality = message.Quality,
                Date = DateTime.UtcNow,
                PublishedDate = DateTime.Parse(message.Data.GetValueOrDefault("publishedDate")),
                Size = long.Parse(message.Data.GetValueOrDefault("size", "0")),
                Indexer = message.Data.GetValueOrDefault("indexer"),
                Protocol = protocol,
                Message = message.Message,
                Languages = message.Languages,
                TorrentInfoHash = GetTorrentInfoHash(message, protocol)
            };

            if (Enum.TryParse(message.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
            {
                blocklist.IndexerFlags = flags;
            }

            _blocklistRepository.Insert(blocklist);
        }

        private static string GetTorrentInfoHash(DownloadFailedEvent message, DownloadProtocol protocol)
        {
            if (message.TrackedDownload?.Protocol == DownloadProtocol.Torrent)
            {
                return message.TrackedDownload.DownloadItem.DownloadId;
            }

            var hash = message.Data.GetValueOrDefault("torrentInfoHash", null);

            if (hash.IsNotNullOrWhiteSpace())
            {
                return hash;
            }

            // Every other path that can reach here is a failure without a TrackedDownload
            // (manual "mark as failed" on a history row, most notably), and the grab only
            // recorded a torrentInfoHash when the indexer supplied one up front -- pushes
            // and most Torznab results don't. For a torrent the download client's id is
            // the infohash, which is exactly what the TrackedDownload branch above stores,
            // so use it rather than writing a torrent row that can never be hash-matched.
            if (protocol == DownloadProtocol.Torrent && message.DownloadId.IsNotNullOrWhiteSpace())
            {
                return message.DownloadId;
            }

            return null;
        }

        public void HandleAsync(GamesDeletedEvent message)
        {
            _blocklistRepository.DeleteForGames(message.Games.Select(m => m.Id).ToList());
        }
    }
}
