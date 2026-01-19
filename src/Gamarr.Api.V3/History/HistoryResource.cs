using System;
using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.History;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Qualities;
using Gamarr.Api.V3.CustomFormats;
using Gamarr.Api.V3.Games;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.History
{
    public class HistoryResource : RestResource
    {
        public int GameId { get; set; }
        public string SourceTitle { get; set; }
        public List<Language> Languages { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public bool QualityCutoffNotMet { get; set; }
        public DateTime Date { get; set; }
        public string DownloadId { get; set; }

        public GameHistoryEventType EventType { get; set; }

        public Dictionary<string, string> Data { get; set; }

        public GameResource Game { get; set; }
    }

    public static class HistoryResourceMapper
    {
        public static HistoryResource ToResource(this GameHistory model, ICustomFormatCalculationService formatCalculator)
        {
            if (model == null)
            {
                return null;
            }

            var customFormats = formatCalculator.ParseCustomFormat(model, model.Game);
            var customFormatScore = model.Game.QualityProfile.CalculateCustomFormatScore(customFormats);

            return new HistoryResource
            {
                Id = model.Id,

                GameId = model.GameId,
                SourceTitle = model.SourceTitle,
                Languages = model.Languages,
                Quality = model.Quality,
                CustomFormats = customFormats.ToResource(false),
                CustomFormatScore = customFormatScore,

                // QualityCutoffNotMet
                Date = model.Date,
                DownloadId = model.DownloadId,

                EventType = model.EventType,

                Data = model.Data
            };
        }
    }
}
