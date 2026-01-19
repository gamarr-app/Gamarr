using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.GamarrList
{
    public class GamarrSettingsValidator : AbstractValidator<GamarrListSettings>
    {
        public GamarrSettingsValidator()
        {
            RuleFor(c => c.Url).ValidRootUrl();
        }
    }

    public class GamarrListSettings : ImportListSettingsBase<GamarrListSettings>
    {
        private static readonly GamarrSettingsValidator Validator = new ();

        [FieldDefinition(0, Label = "List URL", HelpText = "The URL for the game list")]
        public string Url { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
