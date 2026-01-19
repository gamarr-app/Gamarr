using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Organizer
{
    public static class FileNameValidation
    {
        public static readonly Regex DeprecatedGameFolderTokensRegex = new (@"(\{[- ._\[\(]?(?:Original[- ._](?:Title|Filename)|Release[- ._]Group|Edition[- ._]Tags|Quality[- ._](?:Full|Title|Proper|Real)|MediaInfo[- ._](?:Video|VideoCodec|VideoBitDepth|Audio|AudioCodec|AudioChannels|AudioLanguages|AudioLanguagesAll|SubtitleLanguages|SubtitleLanguagesAll|3D|Simple|Full|VideoDynamicRange|VideoDynamicRangeType))[- ._\]\)]?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static readonly Regex OriginalTokenRegex = new (@"(\{Original[- ._](?:Title|Filename)\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IRuleBuilderOptions<T, string> ValidGameFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));
            ruleBuilder.SetValidator(new IllegalCharactersValidator());

            return ruleBuilder.SetValidator(new ValidGameFormatValidator());
        }

        public static IRuleBuilderOptions<T, string> ValidGameFolderFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));
            ruleBuilder.SetValidator(new IllegalCharactersValidator());
            ruleBuilder.SetValidator(new IllegalGameFolderTokensValidator());

            return ruleBuilder.SetValidator(new ValidGameFolderFormatValidator());
        }
    }

    public class ValidGameFormatValidator : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Must contain either game title and release year OR Original Title/Filename";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue is not string value)
            {
                return false;
            }

            return (FileNameBuilder.GameTitleRegex.IsMatch(value) && FileNameBuilder.ReleaseYearRegex.IsMatch(value) && !FileNameValidation.OriginalTokenRegex.IsMatch(value)) ||
                   FileNameValidation.OriginalTokenRegex.IsMatch(value);
        }
    }

    public class ValidGameFolderFormatValidator : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Must contain game title";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue is not string value)
            {
                return false;
            }

            return FileNameBuilder.GameTitleRegex.IsMatch(value);
        }
    }

    public class IllegalGameFolderTokensValidator : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Must not contain deprecated tokens derived from file properties: {tokens}";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue is not string value)
            {
                return false;
            }

            var match = FileNameValidation.DeprecatedGameFolderTokensRegex.Matches(value);

            if (match.Any())
            {
                context.MessageFormatter.AppendArgument("tokens", string.Join(", ", match.Select(c => c.Value).ToArray()));

                return false;
            }

            return true;
        }
    }

    public class IllegalCharactersValidator : PropertyValidator
    {
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

        protected override string GetDefaultMessageTemplate() => "Contains illegal characters: {InvalidCharacters}";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var value = context.PropertyValue as string;
            if (value.IsNullOrWhiteSpace())
            {
                return true;
            }

            var invalidCharacters = InvalidPathChars.Where(i => value!.IndexOf(i) >= 0).ToList();
            if (invalidCharacters.Any())
            {
                context.MessageFormatter.AppendArgument("InvalidCharacters", string.Join("", invalidCharacters));
                return false;
            }

            return true;
        }
    }
}
