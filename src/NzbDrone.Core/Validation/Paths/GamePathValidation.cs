using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Validation.Paths
{
    public class GamePathValidator : PropertyValidator
    {
        private readonly IGameService _gamesService;

        public GamePathValidator(IGameService gamesService)
        {
            _gamesService = gamesService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is already configured for an existing game";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            dynamic instance = context.ParentContext.InstanceToValidate;
            var instanceId = (int)instance.Id;

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            // Skip the path for this game and any invalid paths
            return !_gamesService.AllGamePaths().Any(s => s.Key != instanceId &&
                                                            s.Value.IsPathValid(PathValidationType.CurrentOs) &&
                                                            s.Value.PathEquals(context.PropertyValue.ToString()));
        }
    }
}
