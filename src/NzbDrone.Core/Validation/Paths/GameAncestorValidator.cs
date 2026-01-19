using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Validation.Paths
{
    public class GameAncestorValidator : PropertyValidator
    {
        private readonly IGameService _gameService;

        public GameAncestorValidator(IGameService gameService)
        {
            _gameService = gameService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is an ancestor of an existing game";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            return !_gameService.AllGamePaths().Any(s => context.PropertyValue.ToString().IsParentPath(s.Value));
        }
    }
}
