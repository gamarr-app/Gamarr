using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Xbmc
{
    public class XbmcSettingsValidator : AbstractValidator<XbmcMetadataSettings>
    {
    }

    public class XbmcMetadataSettings : IProviderConfig
    {
        private static readonly XbmcSettingsValidator Validator = new XbmcSettingsValidator();

        public XbmcMetadataSettings()
        {
            GameMetadata = true;
            UseGameNfo = false;
            GameMetadataLanguage = (int)Language.English;
            GameMetadataURL = false;
            AddCollectionName = true;
            GameImages = true;
        }

        [FieldDefinition(0, Label = "MetadataSettingsGameMetadata", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "MetadataXmbcSettingsGameMetadataHelpText")]
        public bool GameMetadata { get; set; }

        [FieldDefinition(1, Label = "MetadataSettingsGameMetadataNfo", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "MetadataXmbcSettingsGameMetadataNfoHelpText")]
        public bool UseGameNfo { get; set; }

        [FieldDefinition(2, Label = "MetadataSettingsGameMetadataLanguage", Type = FieldType.Select, SelectOptions = typeof(RealLanguageFieldConverter), Section = MetadataSectionType.Metadata, HelpText = "MetadataXmbcSettingsGameMetadataLanguageHelpText")]
        public int GameMetadataLanguage { get; set; }

        [FieldDefinition(3, Label = "MetadataSettingsGameMetadataUrl", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "MetadataXmbcSettingsGameMetadataUrlHelpText", Advanced = true)]
        public bool GameMetadataURL { get; set; }

        [FieldDefinition(4, Label = "MetadataSettingsGameMetadataCollectionName", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "MetadataXmbcSettingsGameMetadataCollectionNameHelpText", Advanced = true)]
        public bool AddCollectionName { get; set; }

        [FieldDefinition(5, Label = "MetadataSettingsGameImages", Type = FieldType.Checkbox, Section = MetadataSectionType.Image, HelpText = "fanart.jpg, poster.jpg")]
        public bool GameImages { get; set; }

        public bool IsValid => true;

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
