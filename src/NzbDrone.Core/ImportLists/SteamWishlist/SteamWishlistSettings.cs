using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.SteamWishlist
{
    public class SteamWishlistSettingsValidator : AbstractValidator<SteamWishlistSettings>
    {
        public SteamWishlistSettingsValidator()
        {
            RuleFor(c => c.SteamUserId).NotEmpty();
        }
    }

    public class SteamWishlistSettings : ImportListSettingsBase<SteamWishlistSettings>
    {
        private static readonly SteamWishlistSettingsValidator Validator = new ();

        public SteamWishlistSettings()
        {
            SteamUserId = "";
        }

        [FieldDefinition(0, Label = "Steam User ID", HelpText = "Your Steam vanity URL name (e.g. 'gaben') or Steam64 ID (e.g. '76561197960287930'). Your wishlist must be public.")]
        public string SteamUserId { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
