using System;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Games;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.AutoTagging.Specifications
{
    public class StatusSpecificationValidator : AbstractValidator<StatusSpecification>
    {
        public StatusSpecificationValidator()
        {
            RuleFor(c => c.Status).Custom((statusType, context) =>
            {
                if (!Enum.IsDefined(typeof(GameStatusType), statusType))
                {
                    context.AddFailure($"Invalid status type condition value: {statusType}");
                }
            });
        }
    }

    public class StatusSpecification : AutoTaggingSpecificationBase
    {
        private static readonly StatusSpecificationValidator Validator = new ();

        public override int Order => 1;
        public override string ImplementationName => "Status";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationStatus", Type = FieldType.Select, SelectOptions = typeof(GameStatusType))]
        public int Status { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Game game)
        {
            return game?.GameMetadata?.Value?.Status == (GameStatusType)Status;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
