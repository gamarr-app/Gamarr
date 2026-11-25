using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Organizer
{
    public static class FileNameValidation
    {
        public static readonly Regex DeprecatedGameFolderTokensRegex = new (@"(\{[- ._\[\(]?(?:Original[- ._](?:Title|Filename)|Release[- ._]Group|Edition[- ._]Tags|Quality[- ._](?:Full|Title|Proper|Real)|MediaInfo[- ._](?:Video|VideoCodec|VideoBitDepth|Audio|AudioCodec|AudioChannels|AudioLanguages|AudioLanguagesAll|SubtitleLanguages|SubtitleLanguagesAll|3D|Simple|Full|VideoDynamicRange|VideoDynamicRangeType))[- ._\]\)]?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static readonly Regex OriginalTokenRegex = new (@"(\{Original[- ._](?:Title|Filename)\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

        // Null contract: FluentValidation runs every rule even after one fails, so each rule
        // below receives null/blank input. NotEmpty is the only rule that reports it; every
        // other rule treats a null or blank value as "not applicable" (passes) and its message
        // factory returns an empty string, so a blank format yields exactly one error.
        public static IRuleBuilderOptions<T, string> ValidGameFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.NotEmpty()
                              .Must(value => !HasIllegalCharacters(value))
                              .WithMessage(GetIllegalCharacterMessage)
                              .Must(IsValidGameFormat)
                              .WithMessage("Must contain either game title and release year OR Original Title/Filename");
        }

        public static IRuleBuilderOptions<T, string> ValidGameFolderFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.NotEmpty()
                              .Must(value => !HasIllegalCharacters(value))
                              .WithMessage(GetIllegalCharacterMessage)
                              .Must(value => !HasDeprecatedTokens(value))
                              .WithMessage(GetDeprecatedTokenMessage)
                              .Must(HasGameTitleToken)
                              .WithMessage("Must contain game title");
        }

        private static bool IsValidGameFormat(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return true;
            }

            return (FileNameBuilder.GameTitleRegex.IsMatch(value) && FileNameBuilder.ReleaseYearRegex.IsMatch(value) && !OriginalTokenRegex.IsMatch(value)) ||
                   OriginalTokenRegex.IsMatch(value);
        }

        private static bool HasGameTitleToken(string value)
        {
            return value.IsNullOrWhiteSpace() || FileNameBuilder.GameTitleRegex.IsMatch(value);
        }

        private static bool HasIllegalCharacters(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return false;
            }

            return InvalidPathChars.Any(i => value.IndexOf(i) >= 0);
        }

        private static string GetIllegalCharacterMessage<T>(T instance, string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var invalidCharacters = InvalidPathChars.Where(i => value.IndexOf(i) >= 0).ToList();
            return $"Contains illegal characters: {string.Join("", invalidCharacters)}";
        }

        private static bool HasDeprecatedTokens(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return false;
            }

            return DeprecatedGameFolderTokensRegex.IsMatch(value);
        }

        private static string GetDeprecatedTokenMessage<T>(T instance, string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var match = DeprecatedGameFolderTokensRegex.Matches(value);
            return $"Must not contain deprecated tokens derived from file properties: {string.Join(", ", match.Select(c => c.Value).ToArray())}";
        }
    }
}
