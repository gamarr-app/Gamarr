using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Wdtv
{
    public class WdtvSettingsValidator : AbstractValidator<WdtvMetadataSettings>
    {
    }

    public class WdtvMetadataSettings : IProviderConfig
    {
        private static readonly WdtvSettingsValidator Validator = new WdtvSettingsValidator();

        public WdtvMetadataSettings()
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
