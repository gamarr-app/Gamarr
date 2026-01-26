using FluentValidation;

namespace NzbDrone.Core.Validation.Paths
{
    public static class PathValidationExtensions
    {
        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            RootFolderValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path is a root folder");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            MappedNetworkDriveValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path is a mapped network drive");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            StartupFolderValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path cannot be the startup folder");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            RecycleBinValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path is the recycle bin folder");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            PathExistsValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path does not exist");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            SystemFolderValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path is a system folder");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            FolderWritableValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Folder is not writable");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            RootFolderAncestorValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path is an ancestor of a root folder");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            RootFolderExistsValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Root folder does not exist");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            GameAncestorValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("Path is an ancestor of a game folder");
        }

        public static IRuleBuilderOptions<T, int> SetPathValidator<T>(
            this IRuleBuilder<T, int> ruleBuilder,
            GameExistsValidator validator)
        {
            return ruleBuilder.Must(id => validator.Validate(id))
                .WithMessage("Game does not exist");
        }

        public static IRuleBuilderOptions<T, string> SetPathValidator<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            FileExistsValidator validator)
        {
            return ruleBuilder.Must(path => validator.Validate(path))
                .WithMessage("File does not exist");
        }
    }
}
