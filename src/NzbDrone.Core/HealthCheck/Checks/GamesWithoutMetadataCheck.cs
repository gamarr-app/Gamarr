using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Games;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(Games.Events.GameUpdatedEvent))]
    [CheckOn(typeof(Games.Events.GameAddedEvent))]
    public class GamesWithoutMetadataCheck : HealthCheckBase
    {
        private readonly IGameService _gameService;
        private readonly Logger _logger;

        public GamesWithoutMetadataCheck(IGameService gameService, ILocalizationService localizationService, Logger logger)
            : base(localizationService)
        {
            _gameService = gameService;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var games = _gameService.GetAllGames();
            var gamesWithoutMetadata = games.Where(g =>
                g.GameMetadata?.Value != null &&
                string.IsNullOrWhiteSpace(g.GameMetadata.Value.Overview) &&
                (g.GameMetadata.Value.Genres == null || !g.GameMetadata.Value.Genres.Any()) &&
                g.GameMetadata.Value.Year == 0).ToList();

            if (gamesWithoutMetadata.Any())
            {
                var titles = gamesWithoutMetadata.Take(5).Select(g => g.Title).ToList();
                var message = gamesWithoutMetadata.Count > 5
                    ? string.Join(", ", titles) + $" and {gamesWithoutMetadata.Count - 5} more"
                    : string.Join(", ", titles);

                return new HealthCheck(
                    GetType(),
                    HealthCheckResult.Warning,
                    _localizationService.GetLocalizedString("GamesWithoutMetadataHealthCheckMessage",
                        new Dictionary<string, object>
                        {
                            { "gameCount", gamesWithoutMetadata.Count },
                            { "gameTitles", message }
                        }),
                    "#games-without-metadata");
            }

            return new HealthCheck(GetType());
        }
    }
}
