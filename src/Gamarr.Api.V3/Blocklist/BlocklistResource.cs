using System;
using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Qualities;
using Gamarr.Api.V3.CustomFormats;
using Gamarr.Api.V3.Games;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Blocklist
{
    public class BlocklistResource : RestResource
    {
        public int GameId { get; set; }
        public string SourceTitle { get; set; }
        public List<Language> Languages { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public DateTime Date { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public string Indexer { get; set; }
        public string Message { get; set; }

        // Stored and matched on since the table existed, but never mapped out, so the key
        // was absent from the JSON entirely -- which reads as "this row has no size" and
        // sends anyone debugging a bad block looking at the wrong field.
        public long? Size { get; set; }
        public DateTime? PublishedDate { get; set; }
        public NzbDrone.Core.Parser.Model.IndexerFlags IndexerFlags { get; set; }

        // The column has always been stored and matched on, but was never mapped out to
        // the API, so every blocklist row read over HTTP looked like it had no infohash.
        public string TorrentInfoHash { get; set; }

        public GameResource Game { get; set; }
    }

    public static class BlocklistResourceMapper
    {
        public static BlocklistResource MapToResource(this NzbDrone.Core.Blocklisting.Blocklist model, ICustomFormatCalculationService formatCalculator)
        {
            if (model == null)
            {
                return null;
            }

            return new BlocklistResource
            {
                Id = model.Id,

                GameId = model.GameId,
                SourceTitle = model.SourceTitle,
                Languages = model.Languages,
                Quality = model.Quality,
                CustomFormats = formatCalculator.ParseCustomFormat(model, model.Game).ToResource(false),
                Date = model.Date,
                Protocol = model.Protocol,
                Indexer = model.Indexer,
                Message = model.Message,
                Size = model.Size,
                PublishedDate = model.PublishedDate,
                IndexerFlags = model.IndexerFlags,
                TorrentInfoHash = model.TorrentInfoHash,

                Game = model.Game.ToResource(0)
            };
        }
    }
}
