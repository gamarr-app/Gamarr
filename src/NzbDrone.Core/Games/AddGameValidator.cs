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
                                .SetValidator(rootFolderValidator)
                                .SetValidator(recycleBinValidator)
                                .SetValidator(gamePathValidator)
                                .SetValidator(gameAncestorValidator);
        }
    }
}
