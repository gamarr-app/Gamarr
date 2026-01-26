using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Games
{
    public interface IAddGameValidator
    {
        ValidationResult Validate(Game instance);
    }

    public class AddGameValidator : AbstractValidator<Game>, IAddGameValidator
    {
        public AddGameValidator(RootFolderValidator rootFolderValidator,
                                 RecycleBinValidator recycleBinValidator,
                                 GamePathValidator gamePathValidator,
                                 GameAncestorValidator gameAncestorValidator)
        {
            RuleFor(c => c.Path).Cascade(CascadeMode.Stop)
                                .IsValidPath()
                                .Must(value => rootFolderValidator.Validate(value))
                                .WithMessage("Path is already configured as a root folder")
                                .Must(value => recycleBinValidator.Validate(value))
                                .WithMessage("Path is configured recycle bin folder")
                                .Must((game, value) => gamePathValidator.Validate(value, game.Id))
                                .WithMessage("Path is already configured for an existing game")
                                .Must(value => gameAncestorValidator.Validate(value))
                                .WithMessage("Path is an ancestor of an existing game");
        }
    }
}
