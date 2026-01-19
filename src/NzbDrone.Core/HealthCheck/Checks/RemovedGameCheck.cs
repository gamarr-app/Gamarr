using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(GameUpdatedEvent))]
    [CheckOn(typeof(GamesDeletedEvent))]
    [CheckOn(typeof(GameRefreshCompleteEvent))]
    public class RemovedGameCheck : HealthCheckBase, ICheckOnCondition<GameUpdatedEvent>, ICheckOnCondition<GamesDeletedEvent>
    {
        private readonly IGameService _gameService;

        public RemovedGameCheck(IGameService gameService, ILocalizationService localizationService)
            : base(localizationService)
        {
            _gameService = gameService;
        }

        public override HealthCheck Check()
        {
            var deletedGame = _gameService.GetAllGames().Where(v => v.GameMetadata.Value.Status == GameStatusType.Deleted).ToList();

            if (deletedGame.Empty())
            {
                return new HealthCheck(GetType());
            }

            var gameText = deletedGame.Select(s => $"{s.Title} (igdbid {s.IgdbId})").Join(", ");

            if (deletedGame.Count == 1)
            {
                return new HealthCheck(GetType(),
                    HealthCheckResult.Error,
                    _localizationService.GetLocalizedString("RemovedGameCheckSingleMessage", new Dictionary<string, object>
                    {
                        { "game", gameText }
                    }),
                    "#game-was-removed-from-igdb");
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Error,
                _localizationService.GetLocalizedString("RemovedGameCheckMultipleMessage", new Dictionary<string, object>
                {
                    { "games", gameText }
                }),
                "#game-was-removed-from-igdb");
        }

        public bool ShouldCheckOnEvent(GamesDeletedEvent message)
        {
            return message.Games.Any(m => m.GameMetadata.Value.Status == GameStatusType.Deleted);
        }

        public bool ShouldCheckOnEvent(GameUpdatedEvent message)
        {
            return message.Game.GameMetadata.Value.Status == GameStatusType.Deleted;
        }
    }
}
