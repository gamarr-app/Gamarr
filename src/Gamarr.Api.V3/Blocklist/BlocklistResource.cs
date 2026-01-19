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

                Game = model.Game.ToResource(0)
            };
        }
    }
}
