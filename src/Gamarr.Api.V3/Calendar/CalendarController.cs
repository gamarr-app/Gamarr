using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.GameStats;
using NzbDrone.Core.Tags;
using NzbDrone.SignalR;
using Gamarr.Api.V3.Games;
using Gamarr.Http;

namespace Gamarr.Api.V3.Calendar
{
    [V3ApiController]
    public class CalendarController : GameControllerWithSignalR
    {
        private readonly IGameService _gamesService;
        private readonly ITagService _tagService;

        public CalendarController(IBroadcastSignalRMessage signalR,
                            IGameService gameService,
                            IGameTranslationService gameTranslationService,
                            IGameStatisticsService gameStatisticsService,
                            IUpgradableSpecification upgradableSpecification,
                            ICustomFormatCalculationService formatCalculator,
                            ITagService tagService,
                            IMapCoversToLocal coverMapper,
                            IConfigService configService)
            : base(gameService, gameTranslationService, gameStatisticsService, upgradableSpecification, formatCalculator, configService, coverMapper, signalR)
        {
            _gamesService = gameService;
            _tagService = tagService;
        }

        [NonAction]
        public override ActionResult<GameResource> GetResourceByIdWithErrorHandler(int id)
        {
            return base.GetResourceByIdWithErrorHandler(id);
        }

        [HttpGet]
        [Produces("application/json")]
        public List<GameResource> GetCalendar(DateTime? start, DateTime? end, bool unmonitored = false, string tags = "")
        {
            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(2);
            var games = _gamesService.GetGamesBetweenDates(startUse, endUse, unmonitored);
            var parsedTags = new List<int>();
            var results = new List<Game>();

            if (tags.IsNotNullOrWhiteSpace())
            {
                parsedTags.AddRange(tags.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            foreach (var game in games)
            {
                if (game == null)
                {
                    continue;
                }

                if (parsedTags.Any() && parsedTags.None(game.Tags.Contains))
                {
                    continue;
                }

                results.Add(game);
            }

            var resources = MapToResource(results);

            return resources
                .OrderBy(m => m.EarlyAccess)
                .ThenBy(m => m.DigitalRelease)
                .ThenBy(m => m.PhysicalRelease)
                .ToList();
        }
    }
}
