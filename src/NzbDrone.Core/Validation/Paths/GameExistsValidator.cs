using FluentValidation.Validators;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Validation.Paths
{
    public class GameExistsValidator : PropertyValidator
    {
        private readonly IGameService _gameService;

        public GameExistsValidator(IGameService gameService)
        {
            _gameService = gameService;
        }

        protected override string GetDefaultMessageTemplate() => "This game has already been added";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var igdbId = (int)context.PropertyValue;

            return _gameService.FindByIgdbId(igdbId) == null;
        }
    }
}
