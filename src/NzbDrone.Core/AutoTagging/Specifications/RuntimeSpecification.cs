using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Games;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.AutoTagging.Specifications
{
    public class RuntimeSpecificationValidator : AbstractValidator<RuntimeSpecification>
    {
        public RuntimeSpecificationValidator()
        {
            RuleFor(c => c.Min).GreaterThanOrEqualTo(0);

            RuleFor(c => c.Max).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .GreaterThanOrEqualTo(c => c.Min);
        }
    }

    public class RuntimeSpecification : AutoTaggingSpecificationBase
    {
        private static readonly RuntimeSpecificationValidator Validator = new ();

        public override int Order => 1;
        public override string ImplementationName => "Runtime";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationMinimumRuntime", Type = FieldType.Number, Unit = "minutes")]
        public int Min { get; set; }

        [FieldDefinition(2, Label = "AutoTaggingSpecificationMaximumRuntime", Type = FieldType.Number, Unit = "minutes")]
        public int Max { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Game game)
        {
            return game?.GameMetadata?.Value?.Runtime != null &&
                   game.GameMetadata.Value.Runtime >= Min &&
                   game.GameMetadata.Value.Runtime <= Max;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
