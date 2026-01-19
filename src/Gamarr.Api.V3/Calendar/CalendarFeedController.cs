using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Tags;
using Gamarr.Http;

namespace Gamarr.Api.V3.Calendar
{
    [V3FeedController("calendar")]
    public class CalendarFeedController : Controller
    {
        private readonly IGameService _gameService;
        private readonly ITagService _tagService;

        public CalendarFeedController(IGameService gameService, ITagService tagService)
        {
            _gameService = gameService;
            _tagService = tagService;
        }

        [HttpGet("Gamarr.ics")]
        public IActionResult GetCalendarFeed(int pastDays = 7, int futureDays = 28, string tags = "", bool unmonitored = false, IReadOnlyCollection<CalendarReleaseType> releaseTypes = null)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);
            var parsedTags = new List<int>();

            if (tags.IsNotNullOrWhiteSpace())
            {
                parsedTags.AddRange(tags.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            var games = _gameService.GetGamesBetweenDates(start, end, unmonitored);
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//gamarr.video//Gamarr//EN"
            };

            var calendarName = "Gamarr Games Calendar";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var game in games.OrderBy(v => v.Added))
            {
                if (parsedTags.Any() && parsedTags.None(game.Tags.Contains))
                {
                    continue;
                }

                if (releaseTypes is not { Count: not 0 } || releaseTypes.Contains(CalendarReleaseType.CinemaRelease))
                {
                    CreateEvent(calendar, game.GameMetadata, "cinematic");
                }

                if (releaseTypes is not { Count: not 0 } || releaseTypes.Contains(CalendarReleaseType.DigitalRelease))
                {
                    CreateEvent(calendar, game.GameMetadata, "digital");
                }

                if (releaseTypes is not { Count: not 0 } || releaseTypes.Contains(CalendarReleaseType.PhysicalRelease))
                {
                    CreateEvent(calendar, game.GameMetadata, "physical");
                }
            }

            var serializer = (IStringSerializer)new SerializerFactory().Build(calendar.GetType(), new SerializationContext());
            var icalendar = serializer.SerializeToString(calendar);

            return Content(icalendar, "text/calendar");
        }

        private void CreateEvent(Ical.Net.Calendar calendar, GameMetadata game, string releaseType)
        {
            var date = game.EarlyAccess;
            var eventType = "_cinemas";
            var summaryText = "(Theatrical Release)";

            if (releaseType == "digital")
            {
                date = game.DigitalRelease;
                eventType = "_digital";
                summaryText = "(Digital Release)";
            }
            else if (releaseType == "physical")
            {
                date = game.PhysicalRelease;
                eventType = "_physical";
                summaryText = "(Physical Release)";
            }

            if (!date.HasValue)
            {
                return;
            }

            var occurrence = calendar.Create<CalendarEvent>();
            occurrence.Uid = "Gamarr_game_" + game.Id + eventType;
            occurrence.Status = game.Status == GameStatusType.Announced ? EventStatus.Tentative : EventStatus.Confirmed;

            occurrence.Start = new CalDateTime(date.Value);
            occurrence.End = occurrence.Start;
            occurrence.IsAllDay = true;

            occurrence.Description = game.Overview;
            occurrence.Categories = new List<string> { game.Studio };

            occurrence.Summary = $"{game.Title} {summaryText}";
        }
    }
}
