using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Games;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.AutoTagging.Specifications
{
    public class GameModeSpecificationValidator : AbstractValidator<GameModeSpecification>
    {
        public GameModeSpecificationValidator()
        {
            RuleFor(c => c.Value).NotEmpty();
        }
    }

    public class GameModeSpecification : AutoTaggingSpecificationBase
    {
        private static readonly GameModeSpecificationValidator Validator = new ();

        public override int Order => 2;
        public override string ImplementationName => "Game Mode";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationGameMode", Type = FieldType.Tag)]
        public IEnumerable<string> Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Game game)
        {
            return game?.GameMetadata?.Value?.GameModes?.Any(mode => Value.ContainsIgnoreCase(mode)) ?? false;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
