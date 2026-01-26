using FluentValidation;

namespace NzbDrone.Core.Validation
{
    public static class ValidationExtensions
    {
        public static IRuleBuilderOptions<T, string> SetValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            FolderChmodValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Invalid folder chmod value");
        }

        public static IRuleBuilderOptions<T, int> SetValidator<T>(
            this IRuleBuilder<T, int> ruleBuilder,
            QualityProfileExistsValidator validator)
        {
            return ruleBuilder.Must(id => validator.Validate(id))
                .WithMessage("Quality profile does not exist");
        }

        public static IRuleBuilderOptions<T, int> SetValidator<T>(
            this IRuleBuilder<T, int> ruleBuilder,
            DownloadClientExistsValidator validator)
        {
            return ruleBuilder.Must(id => validator.Validate(id))
                .WithMessage("Download client does not exist");
        }
    }
}
