using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using Gamarr.Api.V3.CustomFormats;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Indexers
{
    public class ReleaseResource : RestResource
    {
        public string Guid { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public int QualityWeight { get; set; }
        public int Age { get; set; }
        public double AgeHours { get; set; }
        public double AgeMinutes { get; set; }
        public long Size { get; set; }
        public int IndexerId { get; set; }
        public string Indexer { get; set; }
        public string ReleaseGroup { get; set; }
        public string Version { get; set; }
        public string SubGroup { get; set; }
        public string ReleaseHash { get; set; }
        public string Title { get; set; }
        public bool SceneSource { get; set; }
        public List<string> GameTitles { get; set; }
        public List<Language> Languages { get; set; }
        public int? MappedGameId { get; set; }
        public bool Approved { get; set; }
        public bool TemporarilyRejected { get; set; }
        public bool Rejected { get; set; }

        /// <summary>
        /// Primary identifier - Steam App ID
        /// </summary>
        public int SteamAppId { get; set; }

        /// <summary>
        /// Secondary identifier - IGDB ID
        /// </summary>
        public int IgdbId { get; set; }

        public IEnumerable<string> Rejections { get; set; }
        public DateTime PublishDate { get; set; }
        public string CommentUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string InfoUrl { get; set; }
        public bool GameRequested { get; set; }
        public bool DownloadAllowed { get; set; }
        public int ReleaseWeight { get; set; }
        public string Edition { get; set; }

        public string MagnetUrl { get; set; }
        public string InfoHash { get; set; }
        public int? Seeders { get; set; }
        public int? Leechers { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public dynamic IndexerFlags { get; set; }

        // Sent when queuing an unknown release
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? GameId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? DownloadClientId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string DownloadClient { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool? ShouldOverride { get; set; }
    }

    public static class ReleaseResourceMapper
    {
        public static ReleaseResource ToResource(this DownloadDecision model)
        {
            var resource = new ReleaseResource();

            MapFromReleaseInfo(resource, model.RemoteGame.Release);
            MapFromParsedGameInfo(resource, model.RemoteGame.ParsedGameInfo);
            MapFromRemoteGame(resource, model.RemoteGame);
            MapFromDecision(resource, model);
            MapFromTorrentInfo(resource, model.RemoteGame.Release as TorrentInfo);

            return resource;
        }

        private static void MapFromReleaseInfo(ReleaseResource resource, ReleaseInfo releaseInfo)
        {
            resource.Guid = releaseInfo.Guid;
            resource.Age = releaseInfo.Age;
            resource.AgeHours = releaseInfo.AgeHours;
            resource.AgeMinutes = releaseInfo.AgeMinutes;
            resource.Size = releaseInfo.Size;
            resource.IndexerId = releaseInfo.IndexerId;
            resource.Indexer = releaseInfo.Indexer;
            resource.Title = releaseInfo.Title;
            resource.SteamAppId = releaseInfo.SteamAppId;
            resource.IgdbId = releaseInfo.IgdbId;
            resource.PublishDate = releaseInfo.PublishDate;
            resource.CommentUrl = releaseInfo.CommentUrl;
            resource.DownloadUrl = releaseInfo.DownloadUrl;
            resource.InfoUrl = releaseInfo.InfoUrl;
            resource.Protocol = releaseInfo.DownloadProtocol;
        }

        private static void MapFromParsedGameInfo(ReleaseResource resource, ParsedGameInfo parsedGameInfo)
        {
            resource.Quality = parsedGameInfo.Quality;
            resource.ReleaseGroup = parsedGameInfo.ReleaseGroup;
            resource.Version = parsedGameInfo.GameVersion?.ToString();
            resource.ReleaseHash = parsedGameInfo.ReleaseHash;
            resource.GameTitles = parsedGameInfo.GameTitles;
            resource.Edition = parsedGameInfo.Edition;
        }

        private static void MapFromRemoteGame(ReleaseResource resource, RemoteGame remoteGame)
        {
            resource.CustomFormats = remoteGame.CustomFormats.ToResource(false);
            resource.CustomFormatScore = remoteGame.CustomFormatScore;
            resource.Languages = remoteGame.Languages;
            resource.MappedGameId = remoteGame.Game?.Id;
            resource.GameRequested = remoteGame.GameRequested;
            resource.DownloadAllowed = remoteGame.DownloadAllowed;
        }

        private static void MapFromDecision(ReleaseResource resource, DownloadDecision decision)
        {
            resource.Approved = decision.Approved;
            resource.TemporarilyRejected = decision.TemporarilyRejected;
            resource.Rejected = decision.Rejected;
            resource.Rejections = decision.Rejections.Select(r => r.Message).ToList();
        }

        private static void MapFromTorrentInfo(ReleaseResource resource, TorrentInfo torrentInfo)
        {
            if (torrentInfo == null)
            {
                return;
            }

            resource.MagnetUrl = torrentInfo.MagnetUrl;
            resource.InfoHash = torrentInfo.InfoHash;
            resource.Seeders = torrentInfo.Seeders;
            resource.Leechers = (torrentInfo.Peers.HasValue && torrentInfo.Seeders.HasValue)
                ? (torrentInfo.Peers.Value - torrentInfo.Seeders.Value)
                : null;

            var indexerFlags = torrentInfo.IndexerFlags.ToString()
                .Split(new[] { ", " }, StringSplitOptions.None)
                .Where(x => x != "0");
            resource.IndexerFlags = indexerFlags;
        }

        public static ReleaseInfo ToModel(this ReleaseResource resource)
        {
            ReleaseInfo model;

            if (resource.Protocol == DownloadProtocol.Torrent)
            {
                model = new TorrentInfo
                {
                    MagnetUrl = resource.MagnetUrl,
                    InfoHash = resource.InfoHash,
                    Seeders = resource.Seeders,
                    Peers = (resource.Seeders.HasValue && resource.Leechers.HasValue) ? (resource.Seeders + resource.Leechers) : null
                };

                if (resource.IndexerFlags is JsonElement { ValueKind: JsonValueKind.Number } indexerFlags)
                {
                    model.IndexerFlags = (IndexerFlags)indexerFlags.GetInt32();
                }
            }
            else
            {
                model = new ReleaseInfo();
            }

            model.Guid = resource.Guid;
            model.Title = resource.Title;
            model.Size = resource.Size;
            model.DownloadUrl = resource.DownloadUrl;
            model.InfoUrl = resource.InfoUrl;
            model.CommentUrl = resource.CommentUrl;
            model.IndexerId = resource.IndexerId;
            model.Indexer = resource.Indexer;
            model.DownloadProtocol = resource.Protocol;
            model.SteamAppId = resource.SteamAppId;
            model.IgdbId = resource.IgdbId;
            model.PublishDate = resource.PublishDate.ToUniversalTime();

            return model;
        }
    }
}
