using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Diacritical;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;

#pragma warning disable CS0618 // Disable obsolete warnings for ImdbId (kept for backward compatibility with file naming)

namespace NzbDrone.Core.Organizer
{
    public interface IBuildFileNames
    {
        string BuildFileName(Game game, GameFile gameFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null);
        string BuildFilePath(Game game, string fileName, string extension);
        string GetGameFolder(Game game, NamingConfig namingConfig = null);
    }

    public class FileNameBuilder : IBuildFileNames
    {
        private const string MediaInfoVideoDynamicRangeToken = "{MediaInfo VideoDynamicRange}";
        private const string MediaInfoVideoDynamicRangeTypeToken = "{MediaInfo VideoDynamicRangeType}";

        private readonly INamingConfigService _namingConfigService;
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly IUpdateMediaInfo _mediaInfoUpdater;
        private readonly IGameTranslationService _gameTranslationService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly Logger _logger;

        private static readonly Regex TitleRegex = new Regex(@"(?<tag>\{(?<prefix>[-{ ._\[(]*)(?:imdb(?:id)?-|edition-))?\{(?<prefix>[-{ ._\[(]*)(?<token>(?:[a-z0-9]+)(?:(?<separator>[- ._]+)(?:[a-z0-9]+))?)(?::(?<customFormat>[ ,a-z0-9|+-]+(?<![- ])))?(?<suffix>[-} ._)\]]*)\}",
                                                             RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static readonly Regex ReleaseYearRegex = new Regex(@"\{[-{ ._\[(]*Release[- ._]Year[-} ._)\]]*\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex GameTitleRegex = new Regex(@"(?<token>\{(?:Game)(?<separator>[- ._])(?:Clean)?(?:OriginalTitle|Title(?:The)?)(?::(?<customFormat>[a-z0-9|-]+))?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FileNameCleanupRegex = new Regex(@"([- ._])(\1)+", RegexOptions.Compiled);
        private static readonly Regex TrimSeparatorsRegex = new Regex(@"[- ._]+$", RegexOptions.Compiled);

        private static readonly Regex ScenifyRemoveChars = new Regex(@"(?<=\s)(,|<|>|\/|\\|;|:|'|""|\||`|’|~|!|\?|@|$|%|^|\*|-|_|=){1}(?=\s)|('|`|’|:|\?|,)(?=(?:(?:s|m|t|ve|ll|d|re)\s)|\s|$)|(\(|\)|\[|\]|\{|\})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScenifyReplaceChars = new Regex(@"[\/]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TitlePrefixRegex = new Regex(@"^(The|An|A) (.*?)((?: *\([^)]+\))*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ReservedDeviceNamesRegex = new Regex(@"^(?:aux|com[1-9]|con|lpt[1-9]|nul|prn)\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // generated from https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt
        public static readonly ImmutableDictionary<string, string> Iso639BTMap = new Dictionary<string, string>
        {
            { "alb", "sqi" },
            { "arm", "hye" },
            { "baq", "eus" },
            { "bur", "mya" },
            { "chi", "zho" },
            { "cze", "ces" },
            { "dut", "nld" },
            { "fre", "fra" },
            { "geo", "kat" },
            { "ger", "deu" },
            { "gre", "ell" },
            { "gsw", "deu" },
            { "ice", "isl" },
            { "mac", "mkd" },
            { "mao", "mri" },
            { "may", "msa" },
            { "per", "fas" },
            { "rum", "ron" },
            { "slo", "slk" },
            { "tib", "bod" },
            { "wel", "cym" },
            { "khk", "mon" },
            { "mvf", "mon" }
        }.ToImmutableDictionary();

        public static readonly ImmutableArray<string> BadCharacters = ImmutableArray.Create("\\", "/", "<", ">", "?", "*", "|", "\"");
        public static readonly ImmutableArray<string> GoodCharacters = ImmutableArray.Create("+", "+", "", "", "!", "-", "", "");

        public FileNameBuilder(INamingConfigService namingConfigService,
                               IQualityDefinitionService qualityDefinitionService,
                               IUpdateMediaInfo mediaInfoUpdater,
                               IGameTranslationService gameTranslationService,
                               ICustomFormatCalculationService formatCalculator,
                               Logger logger)
        {
            _namingConfigService = namingConfigService;
            _qualityDefinitionService = qualityDefinitionService;
            _mediaInfoUpdater = mediaInfoUpdater;
            _gameTranslationService = gameTranslationService;
            _formatCalculator = formatCalculator;
            _logger = logger;
        }

        public string BuildFileName(Game game, GameFile gameFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            if (!namingConfig.RenameGames)
            {
                return GetOriginalTitle(gameFile, false);
            }

            if (namingConfig.StandardGameFormat.IsNullOrWhiteSpace())
            {
                throw new NamingFormatException("Standard game format cannot be empty");
            }

            var pattern = namingConfig.StandardGameFormat;

            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);
            var multipleTokens = TitleRegex.Matches(pattern).Count > 1;

            UpdateMediaInfoIfNeeded(pattern, gameFile, game);

            AddGameTokens(tokenHandlers, game);
            AddReleaseDateTokens(tokenHandlers, game.Year);
            AddIdTokens(tokenHandlers, game);
            AddQualityTokens(tokenHandlers, game, gameFile);
            AddMediaInfoTokens(tokenHandlers, gameFile);
            AddGameFileTokens(tokenHandlers, gameFile, multipleTokens);
            AddEditionTagsTokens(tokenHandlers, gameFile);
            AddCustomFormats(tokenHandlers, game, gameFile, customFormats);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplaceTokens(splitPattern, tokenHandlers, namingConfig).Trim();

                component = FileNameCleanupRegex.Replace(component, match => match.Captures[0].Value[0].ToString());
                component = TrimSeparatorsRegex.Replace(component, string.Empty);
                component = component.Replace("{ellipsis}", "...");
                component = ReplaceReservedDeviceNames(component);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public string BuildFilePath(Game game, string fileName, string extension)
        {
            Ensure.That(extension, () => extension).IsNotNullOrWhiteSpace();

            var path = game.Path;

            return Path.Combine(path, fileName + extension);
        }

        public string GetGameFolder(Game game, NamingConfig namingConfig = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddGameTokens(tokenHandlers, game);
            AddReleaseDateTokens(tokenHandlers, game.Year);
            AddIdTokens(tokenHandlers, game);

            var pattern = namingConfig.GameFolderFormat;
            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplaceTokens(splitPattern, tokenHandlers, namingConfig);
                component = CleanFolderName(component);
                component = component.Replace("{ellipsis}", "...");
                component = ReplaceReservedDeviceNames(component);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public static string CleanTitle(string title)
        {
            title = title.Replace("&", "and");
            title = ScenifyReplaceChars.Replace(title, " ");
            title = ScenifyRemoveChars.Replace(title, string.Empty);

            return title.RemoveDiacritics();
        }

        public static string TitleThe(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            return TitlePrefixRegex.Replace(title, "$2, $1$3");
        }

        public static string CleanTitleThe(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            if (TitlePrefixRegex.IsMatch(title))
            {
                var splitResult = TitlePrefixRegex.Split(title);
                return $"{CleanTitle(splitResult[2]).Trim()}, {splitResult[1]}{CleanTitle(splitResult[3])}";
            }

            return CleanTitle(title);
        }

        public static string TitleFirstCharacter(string title)
        {
            if (char.IsLetterOrDigit(title[0]))
            {
                return title.Substring(0, 1).ToUpper().RemoveDiacritics()[0].ToString();
            }

            // Try the second character if the first was non alphanumeric
            if (char.IsLetterOrDigit(title[1]))
            {
                return title.Substring(1, 1).ToUpper().RemoveDiacritics()[0].ToString();
            }

            // Default to "_" if no alphanumeric character can be found in the first 2 positions
            return "_";
        }

        public static string CleanFileName(string name)
        {
            return CleanFileName(name, NamingConfig.Default);
        }

        public static string CleanFolderName(string name)
        {
            name = FileNameCleanupRegex.Replace(name, match => match.Captures[0].Value[0].ToString());

            // Remove empty brackets (e.g., when year is missing)
            name = Regex.Replace(name, @"\s*\(\s*\)", string.Empty);
            name = Regex.Replace(name, @"\s*\[\s*\]", string.Empty);
            name = Regex.Replace(name, @"\s*\{\s*\}", string.Empty);

            return name.Trim(' ', '.');
        }

        private void AddGameTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Game game)
        {
            tokenHandlers["{Game Title}"] = m => Truncate(GetLanguageTitle(game, m.CustomFormat), m.CustomFormat);
            tokenHandlers["{Game CleanTitle}"] = m => Truncate(CleanTitle(GetLanguageTitle(game, m.CustomFormat)), m.CustomFormat);
            tokenHandlers["{Game TitleThe}"] = m => Truncate(TitleThe(game.Title), m.CustomFormat);
            tokenHandlers["{Game CleanTitleThe}"] = m => Truncate(CleanTitleThe(game.Title), m.CustomFormat);
            tokenHandlers["{Game TitleFirstCharacter}"] = m => TitleFirstCharacter(TitleThe(GetLanguageTitle(game, m.CustomFormat)));
            tokenHandlers["{Game OriginalTitle}"] = m => Truncate(game.GameMetadata.Value.OriginalTitle, m.CustomFormat) ?? string.Empty;
            tokenHandlers["{Game CleanOriginalTitle}"] = m => Truncate(CleanTitle(game.GameMetadata.Value.OriginalTitle ?? string.Empty), m.CustomFormat);

            tokenHandlers["{Game Certification}"] = m => game.GameMetadata.Value.Certification ?? string.Empty;
            tokenHandlers["{Game Collection}"] = m => Truncate(game.GameMetadata.Value.CollectionTitle, m.CustomFormat) ?? string.Empty;
            tokenHandlers["{Game CollectionThe}"] = m => Truncate(TitleThe(game.GameMetadata.Value.CollectionTitle), m.CustomFormat) ?? string.Empty;
            tokenHandlers["{Game CleanCollectionThe}"] = m => Truncate(CleanTitleThe(game.GameMetadata.Value.CollectionTitle), m.CustomFormat) ?? string.Empty;
        }

        private string GetLanguageTitle(Game game, string isoCodes)
        {
            if (isoCodes.IsNotNullOrWhiteSpace())
            {
                foreach (var isoCode in isoCodes.Split('|'))
                {
                    var language = IsoLanguages.Find(isoCode.ToLower())?.Language;

                    if (language == null)
                    {
                        continue;
                    }

                    var titles = game.GameMetadata.Value.Translations.Where(t => t.Language == language).ToList();

                    if (!game.GameMetadata.Value.Translations.Any())
                    {
                        titles = _gameTranslationService.GetAllTranslationsForGameMetadata(game.GameMetadataId).Where(t => t.Language == language).ToList();
                    }

                    return titles.FirstOrDefault()?.Title ?? game.Title;
                }
            }

            return game.Title;
        }

        private void AddEditionTagsTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, GameFile gameFile)
        {
            if (gameFile.Edition.IsNotNullOrWhiteSpace())
            {
                tokenHandlers["{Edition Tags}"] = m => Truncate(GetEditionToken(gameFile), m.CustomFormat);
            }
        }

        private void AddReleaseDateTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, int releaseYear)
        {
            if (releaseYear == 0)
            {
                tokenHandlers["{Release Year}"] = m => string.Empty;
                return;
            }

            tokenHandlers["{Release Year}"] = m => string.Format("{0}", releaseYear.ToString()); // Do I need m.CustomFormat?
        }

        private void AddIdTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Game game)
        {
            tokenHandlers["{ImdbId}"] = m => game.GameMetadata.Value.ImdbId ?? string.Empty;
            tokenHandlers["{IgdbId}"] = m => game.GameMetadata.Value.IgdbId.ToString();
        }

        private void AddGameFileTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, GameFile gameFile, bool multipleTokens)
        {
            tokenHandlers["{Original Title}"] = m => GetOriginalTitle(gameFile, multipleTokens);
            tokenHandlers["{Original Filename}"] = m => GetOriginalFileName(gameFile, multipleTokens);
            tokenHandlers["{Release Group}"] = m => gameFile.ReleaseGroup.IsNullOrWhiteSpace() ? m.DefaultValue("Gamarr") : Truncate(gameFile.ReleaseGroup, m.CustomFormat);
        }

        private void AddQualityTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Game game, GameFile gameFile)
        {
            if (gameFile?.Quality?.Quality == null)
            {
                tokenHandlers["{Quality Full}"] = m => "";
                tokenHandlers["{Quality Title}"] = m => "";
                tokenHandlers["{Quality Proper}"] = m => "";
                tokenHandlers["{Quality Real}"] = m => "";
                return;
            }

            var qualityTitle = _qualityDefinitionService.Get(gameFile.Quality.Quality).Title;
            var qualityProper = GetQualityProper(game, gameFile.Quality);
            var qualityReal = GetQualityReal(game, gameFile.Quality);

            tokenHandlers["{Quality Full}"] = m => string.Format("{0} {1} {2}", qualityTitle, qualityProper, qualityReal);
            tokenHandlers["{Quality Title}"] = m => qualityTitle;
            tokenHandlers["{Quality Proper}"] = m => qualityProper;
            tokenHandlers["{Quality Real}"] = m => qualityReal;
        }

        private static readonly IReadOnlyDictionary<string, int> MinimumMediaInfoSchemaRevisions =
            new Dictionary<string, int>(FileNameBuilderTokenEqualityComparer.Instance)
        {
            { MediaInfoVideoDynamicRangeToken, 5 },
            { MediaInfoVideoDynamicRangeTypeToken, 13 }
        };

        private void AddMediaInfoTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, GameFile gameFile)
        {
            if (gameFile.MediaInfo == null)
            {
                _logger.Trace("Media info is unavailable for {0}", gameFile);

                return;
            }

            var sceneName = gameFile.GetSceneOrFileName();

            var videoCodec = MediaInfoFormatter.FormatVideoCodec(gameFile.MediaInfo, sceneName) ?? string.Empty;
            var audioCodec = MediaInfoFormatter.FormatAudioCodec(gameFile.MediaInfo, sceneName) ?? string.Empty;
            var audioChannels = MediaInfoFormatter.FormatAudioChannels(gameFile.MediaInfo);
            var audioLanguages = gameFile.MediaInfo.AudioLanguages ?? new List<string>();
            var subtitles = gameFile.MediaInfo.Subtitles ?? new List<string>();

            var videoBitDepth = gameFile.MediaInfo.VideoBitDepth > 0 ? gameFile.MediaInfo.VideoBitDepth.ToString() : 8.ToString();
            var audioChannelsFormatted = audioChannels > 0 ?
                                audioChannels.ToString("F1", CultureInfo.InvariantCulture) :
                                string.Empty;

            var mediaInfo3D = gameFile.MediaInfo.VideoMultiViewCount > 1 ? "3D" : string.Empty;

            tokenHandlers["{MediaInfo Video}"] = m => videoCodec;
            tokenHandlers["{MediaInfo VideoCodec}"] = m => videoCodec;
            tokenHandlers["{MediaInfo VideoBitDepth}"] = m => videoBitDepth;

            tokenHandlers["{MediaInfo Audio}"] = m => audioCodec;
            tokenHandlers["{MediaInfo AudioCodec}"] = m => audioCodec;
            tokenHandlers["{MediaInfo AudioChannels}"] = m => audioChannelsFormatted;
            tokenHandlers["{MediaInfo AudioLanguages}"] = m => GetLanguagesToken(audioLanguages, m.CustomFormat, true, true);
            tokenHandlers["{MediaInfo AudioLanguagesAll}"] = m => GetLanguagesToken(audioLanguages, m.CustomFormat, false, true);

            tokenHandlers["{MediaInfo SubtitleLanguages}"] = m => GetLanguagesToken(subtitles, m.CustomFormat, false, true);
            tokenHandlers["{MediaInfo SubtitleLanguagesAll}"] = m => GetLanguagesToken(subtitles, m.CustomFormat, false, true);

            tokenHandlers["{MediaInfo 3D}"] = m => mediaInfo3D;

            tokenHandlers["{MediaInfo Simple}"] = m => $"{videoCodec} {audioCodec}";
            tokenHandlers["{MediaInfo Full}"] = m => $"{videoCodec} {audioCodec}{GetLanguagesToken(audioLanguages, m.CustomFormat, true, true)} {GetLanguagesToken(subtitles, m.CustomFormat, false, true)}";

            tokenHandlers[MediaInfoVideoDynamicRangeToken] =
                m => MediaInfoFormatter.FormatVideoDynamicRange(gameFile.MediaInfo);
            tokenHandlers[MediaInfoVideoDynamicRangeTypeToken] =
                m => MediaInfoFormatter.FormatVideoDynamicRangeType(gameFile.MediaInfo);
        }

        private void AddCustomFormats(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Game game, GameFile gameFile, List<CustomFormat> customFormats = null)
        {
            if (customFormats == null)
            {
                gameFile.Game = game;
                customFormats = _formatCalculator.ParseCustomFormat(gameFile, game);
            }

            tokenHandlers["{Custom Formats}"] = m => GetCustomFormatsToken(customFormats, m.CustomFormat);
            tokenHandlers["{Custom Format}"] = m =>
            {
                if (m.CustomFormat.IsNullOrWhiteSpace())
                {
                    return string.Empty;
                }

                return customFormats.FirstOrDefault(x => x.IncludeCustomFormatWhenRenaming && x.Name == m.CustomFormat)?.ToString() ?? string.Empty;
            };
        }

        private string GetCustomFormatsToken(List<CustomFormat> customFormats, string filter)
        {
            var tokens = customFormats.Where(x => x.IncludeCustomFormatWhenRenaming).ToList();

            var filteredTokens = tokens;

            if (filter.IsNotNullOrWhiteSpace())
            {
                if (filter.StartsWith("-"))
                {
                    var splitFilter = filter.Substring(1).Split(',');
                    filteredTokens = tokens.Where(c => !splitFilter.Contains(c.Name)).ToList();
                }
                else
                {
                    var splitFilter = filter.Split(',');
                    filteredTokens = tokens.Where(c => splitFilter.Contains(c.Name)).ToList();
                }
            }

            return string.Join(" ", filteredTokens);
        }

        private string GetLanguagesToken(List<string> mediaInfoLanguages, string filter, bool skipEnglishOnly, bool quoted)
        {
            var tokens = new List<string>();
            foreach (var item in mediaInfoLanguages)
            {
                if (!string.IsNullOrWhiteSpace(item) && item != "und")
                {
                    tokens.Add(item.Trim());
                }
            }

            for (var i = 0; i < tokens.Count; i++)
            {
                try
                {
                    var token = tokens[i].ToLowerInvariant();
                    if (Iso639BTMap.TryGetValue(token, out var mapped))
                    {
                        token = mapped;
                    }

                    var cultureInfo = new CultureInfo(token);
                    tokens[i] = cultureInfo.TwoLetterISOLanguageName.ToUpper();
                }
                catch
                {
                }
            }

            tokens = tokens.Distinct().ToList();

            var filteredTokens = tokens;

            // Exclude or filter
            if (filter.IsNotNullOrWhiteSpace())
            {
                if (filter.StartsWith("-"))
                {
                    filteredTokens = tokens.Except(filter.Split('-')).ToList();
                }
                else
                {
                    filteredTokens = filter.Split('+').Intersect(tokens).ToList();
                }
            }

            // Replace with wildcard (maybe too limited)
            if (filter.IsNotNullOrWhiteSpace() && filter.EndsWith("+") && filteredTokens.Count != tokens.Count)
            {
                filteredTokens.Add("--");
            }

            if (skipEnglishOnly && filteredTokens.Count == 1 && filteredTokens.First() == "EN")
            {
                return string.Empty;
            }

            var response = string.Join("+", filteredTokens);

            if (quoted && response.IsNotNullOrWhiteSpace())
            {
                return $"[{response}]";
            }
            else
            {
                return response;
            }
        }

        private string GetEditionToken(GameFile gameFile)
        {
            var edition = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(gameFile.Edition.ToLowerInvariant());

            edition = Regex.Replace(edition, @"((?:\b|_)\d{1,3}(?:st|th|rd|nd)(?:\b|_))", match => match.Groups[1].Value.ToLowerInvariant(), RegexOptions.IgnoreCase);
            edition = Regex.Replace(edition, @"((?:\b|_)(?:IMAX|3D|SDR|HDR|DV)(?:\b|_))", match => match.Groups[1].Value.ToUpperInvariant(), RegexOptions.IgnoreCase);

            return edition;
        }

        private void UpdateMediaInfoIfNeeded(string pattern, GameFile gameFile, Game game)
        {
            if (game.Path.IsNullOrWhiteSpace())
            {
                return;
            }

            var schemaRevision = gameFile.MediaInfo != null ? gameFile.MediaInfo.SchemaRevision : 0;
            var matches = TitleRegex.Matches(pattern);

            var shouldUpdateMediaInfo = matches.Cast<Match>()
                .Select(m => MinimumMediaInfoSchemaRevisions.GetValueOrDefault(m.Value, -1))
                .Any(r => schemaRevision < r);

            if (shouldUpdateMediaInfo)
            {
                _mediaInfoUpdater.Update(gameFile, game);
            }
        }

        private string ReplaceTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            return TitleRegex.Replace(pattern, match => ReplaceToken(match, tokenHandlers, namingConfig));
        }

        private string ReplaceToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            var tokenMatch = new TokenMatch
            {
                RegexMatch = match,
                Tag = match.Groups["tag"].Value,
                Prefix = match.Groups["prefix"].Value,
                Separator = match.Groups["separator"].Value,
                Suffix = match.Groups["suffix"].Value,
                Token = match.Groups["token"].Value,
                CustomFormat = match.Groups["customFormat"].Value
            };

            if (tokenMatch.CustomFormat.IsNullOrWhiteSpace())
            {
                tokenMatch.CustomFormat = null;
            }

            var tokenHandler = tokenHandlers.GetValueOrDefault(tokenMatch.Token, m => string.Empty);

            var replacementText = tokenHandler(tokenMatch).Trim();

            if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsLower(t)))
            {
                replacementText = replacementText.ToLower();
            }
            else if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsUpper(t)))
            {
                replacementText = replacementText.ToUpper();
            }

            if (!tokenMatch.Separator.IsNullOrWhiteSpace())
            {
                replacementText = replacementText.Replace(" ", tokenMatch.Separator);
            }

            replacementText = CleanFileName(replacementText, namingConfig);

            if (!replacementText.IsNullOrWhiteSpace())
            {
                replacementText = tokenMatch.Tag + tokenMatch.Prefix + replacementText + tokenMatch.Suffix;
            }

            return replacementText;
        }

        private string ReplaceNumberToken(string token, int value)
        {
            var split = token.Trim('{', '}').Split(':');
            if (split.Length == 1)
            {
                return value.ToString("0");
            }

            return value.ToString(split[1]);
        }

        private string GetQualityProper(Game game, QualityModel quality)
        {
            if (quality.Revision.Version > 1)
            {
                return "Proper";
            }

            return string.Empty;
        }

        private string GetQualityReal(Game game, QualityModel quality)
        {
            if (quality.Revision.Real > 0)
            {
                return "REAL";
            }

            return string.Empty;
        }

        private string GetOriginalTitle(GameFile gameFile, bool multipleTokens)
        {
            if (gameFile.SceneName.IsNullOrWhiteSpace())
            {
                return CleanFileName(GetOriginalFileName(gameFile, multipleTokens));
            }

            return CleanFileName(gameFile.SceneName);
        }

        private string GetOriginalFileName(GameFile gameFile, bool multipleTokens)
        {
            if (multipleTokens)
            {
                return string.Empty;
            }

            if (gameFile.RelativePath.IsNullOrWhiteSpace())
            {
                return Path.GetFileNameWithoutExtension(gameFile.Path);
            }

            return Path.GetFileNameWithoutExtension(gameFile.RelativePath);
        }

        private string ReplaceReservedDeviceNames(string input)
        {
            // Replace reserved windows device names with an alternative
            return ReservedDeviceNamesRegex.Replace(input, match => match.Value.Replace(".", "_"));
        }

        private static string CleanFileName(string name, NamingConfig namingConfig)
        {
            var result = name;

            if (namingConfig.ReplaceIllegalCharacters)
            {
                // Smart replaces a colon followed by a space with space dash space for a better appearance
                if (namingConfig.ColonReplacementFormat == ColonReplacementFormat.Smart)
                {
                    result = result.Replace(": ", " - ");
                    result = result.Replace(":", "-");
                }
                else
                {
                    var replacement = string.Empty;

                    switch (namingConfig.ColonReplacementFormat)
                    {
                        case ColonReplacementFormat.Dash:
                            replacement = "-";
                            break;
                        case ColonReplacementFormat.SpaceDash:
                            replacement = " -";
                            break;
                        case ColonReplacementFormat.SpaceDashSpace:
                            replacement = " - ";
                            break;
                    }

                    result = result.Replace(":", replacement);
                }
            }
            else
            {
                result = result.Replace(":", string.Empty);
            }

            for (var i = 0; i < BadCharacters.Length; i++)
            {
                result = result.Replace(BadCharacters[i], namingConfig.ReplaceIllegalCharacters ? GoodCharacters[i] : string.Empty);
            }

            return result.TrimStart(' ', '.').TrimEnd(' ');
        }

        private string Truncate(string input, string formatter)
        {
            if (input.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var maxLength = GetMaxLengthFromFormatter(formatter);

            if (maxLength == 0 || input.Length <= Math.Abs(maxLength))
            {
                return input;
            }

            if (maxLength < 0)
            {
                return $"{{ellipsis}}{input.Reverse().Truncate(Math.Abs(maxLength) - 3).TrimEnd(' ', '.').Reverse()}";
            }

            return $"{input.Truncate(maxLength - 3).TrimEnd(' ', '.')}{{ellipsis}}";
        }

        private int GetMaxLengthFromFormatter(string formatter)
        {
            int.TryParse(formatter, out var maxCustomLength);

            return maxCustomLength;
        }
    }

    internal sealed class TokenMatch
    {
        public Match RegexMatch { get; set; }
        public string Tag { get; set; }
        public string Prefix { get; set; }
        public string Separator { get; set; }
        public string Suffix { get; set; }
        public string Token { get; set; }
        public string CustomFormat { get; set; }

        public string DefaultValue(string defaultValue)
        {
            if (string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix))
            {
                return defaultValue;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public enum ColonReplacementFormat
    {
        Delete = 0,
        Dash = 1,
        SpaceDash = 2,
        SpaceDashSpace = 3,
        Smart = 4
    }
}
