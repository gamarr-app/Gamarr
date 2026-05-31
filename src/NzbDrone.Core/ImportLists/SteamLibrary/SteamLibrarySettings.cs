using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.SteamLibrary
{
    public class SteamLibrarySettingsValidator : AbstractValidator<SteamLibrarySettings>
    {
        public SteamLibrarySettingsValidator()
        {
            RuleFor(c => c.SteamUserId).NotEmpty();
            RuleFor(c => c.SteamApiKey).NotEmpty();
        }
    }

    public class SteamLibrarySettings : ImportListSettingsBase<SteamLibrarySettings>
    {
        private static readonly SteamLibrarySettingsValidator Validator = new ();

        public SteamLibrarySettings()
        {
            SteamUserId = "";
            SteamApiKey = "";
            IncludePlayedOnly = false;
        }

        [FieldDefinition(0, Label = "Steam User ID", HelpText = "Your Steam vanity URL name (e.g. 'gaben') or Steam64 ID (e.g. '76561197960287930').")]
        public string SteamUserId { get; set; }

        [FieldDefinition(1, Label = "Steam API Key", Type = FieldType.Textbox, Privacy = PrivacyLevel.ApiKey, HelpText = "Steam Web API key. Get one at https://steamcommunity.com/dev/apikey (free, takes 30 seconds).")]
        public string SteamApiKey { get; set; }

        [FieldDefinition(2, Label = "Include Played Games Only", Type = FieldType.Checkbox, HelpText = "Only import games you have played at least once. Useful to skip free-to-play games you accepted but never launched.")]
        public bool IncludePlayedOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
