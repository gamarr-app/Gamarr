using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.GogLibrary
{
    public class GogLibrarySettingsValidator : AbstractValidator<GogLibrarySettings>
    {
        public GogLibrarySettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
        }
    }

    public class GogLibrarySettings : ImportListSettingsBase<GogLibrarySettings>
    {
        private static readonly GogLibrarySettingsValidator Validator = new ();

        public GogLibrarySettings()
        {
            Username = "";
        }

        [FieldDefinition(0, Label = "GOG Username", HelpText = "Your GOG username (as in gog.com/u/<username>). Your GOG profile and games list must both be set to public in GOG Privacy Settings.")]
        public string Username { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
