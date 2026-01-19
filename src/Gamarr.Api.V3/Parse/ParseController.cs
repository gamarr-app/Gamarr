using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.Parser;
using Gamarr.Api.V3.CustomFormats;
using Gamarr.Api.V3.Games;
using Gamarr.Http;

namespace Gamarr.Api.V3.Parse
{
    [V3ApiController]
    public class ParseController : Controller
    {
        private readonly IParsingService _parsingService;
        private readonly IConfigService _configService;
        private readonly IRemoteGameAggregationService _aggregationService;
        private readonly ICustomFormatCalculationService _formatCalculator;

        public ParseController(IParsingService parsingService,
                               IConfigService configService,
                               IRemoteGameAggregationService aggregationService,
                               ICustomFormatCalculationService formatCalculator)
        {
            _parsingService = parsingService;
            _configService = configService;
            _aggregationService = aggregationService;
            _formatCalculator = formatCalculator;
        }

        [HttpGet]
        public ParseResource Parse(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parsedGameInfo = Parser.ParseGameTitle(title);

            if (parsedGameInfo == null)
            {
                return new ParseResource
                {
                    Title = title
                };
            }

            var remoteGame = _parsingService.Map(parsedGameInfo, "", 0);

            if (remoteGame != null)
            {
                _aggregationService.Augment(remoteGame);

                remoteGame.CustomFormats = _formatCalculator.ParseCustomFormat(remoteGame, 0);
                remoteGame.CustomFormatScore = remoteGame.Game?.QualityProfile?.CalculateCustomFormatScore(remoteGame.CustomFormats) ?? 0;

                return new ParseResource
                {
                    Title = title,
                    ParsedGameInfo = remoteGame.ParsedGameInfo,
                    Game = remoteGame.Game.ToResource(_configService.AvailabilityDelay),
                    Languages = remoteGame.Languages,
                    CustomFormats = remoteGame.CustomFormats?.ToResource(false),
                    CustomFormatScore = remoteGame.CustomFormatScore
                };
            }
            else
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedGameInfo = parsedGameInfo
                };
            }
        }
    }
}
