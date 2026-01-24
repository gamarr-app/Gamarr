using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Games;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.AutoTagging.Specifications
{
    public class PlatformSpecificationValidator : AbstractValidator<PlatformSpecification>
    {
        public PlatformSpecificationValidator()
        {
            RuleFor(c => c.Value).NotEmpty();
        }
    }

    public class PlatformSpecification : AutoTaggingSpecificationBase
    {
        private static readonly PlatformSpecificationValidator Validator = new ();

        public override int Order => 2;
        public override string ImplementationName => "Platform";

        [FieldDefinition(1, Label = "AutoTaggingSpecificationPlatform", Type = FieldType.Tag)]
        public IEnumerable<string> Value { get; set; }

        protected override bool IsSatisfiedByWithoutNegate(Game game)
        {
            var platforms = game?.GameMetadata?.Value?.Platforms;

            if (platforms == null || !platforms.Any())
            {
                return false;
            }

            return platforms.Any(p =>
                Value.ContainsIgnoreCase(p.Name) ||
                (!string.IsNullOrEmpty(p.Abbreviation) && Value.ContainsIgnoreCase(p.Abbreviation)) ||
                Value.ContainsIgnoreCase(p.Family.ToString()));
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
