using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Roksbox
{
    public class RoksboxSettingsValidator : AbstractValidator<RoksboxMetadataSettings>
    {
    }

    public class RoksboxMetadataSettings : IProviderConfig
    {
        private static readonly RoksboxSettingsValidator Validator = new RoksboxSettingsValidator();

        public RoksboxMetadataSettings()
        {
            GameMetadata = true;
            GameImages = true;
        }

        [FieldDefinition(0, Label = "MetadataSettingsGameMetadata", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata)]
        public bool GameMetadata { get; set; }

        [FieldDefinition(1, Label = "MetadataSettingsGameImages", Type = FieldType.Checkbox, Section = MetadataSectionType.Image)]
        public bool GameImages { get; set; }

        public bool IsValid => true;

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
