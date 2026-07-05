using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.GogWishlist
{
    public class GogWishlistSettingsValidator : AbstractValidator<GogWishlistSettings>
    {
        public GogWishlistSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
        }
    }

    public class GogWishlistSettings : ImportListSettingsBase<GogWishlistSettings>
    {
        private static readonly GogWishlistSettingsValidator Validator = new ();

        public GogWishlistSettings()
        {
            Username = "";
        }

        [FieldDefinition(0, Label = "GOG Username", HelpText = "Your GOG username (as in gog.com/u/<username>). Your GOG profile and wishlist must both be set to public in GOG Privacy Settings.")]
        public string Username { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
