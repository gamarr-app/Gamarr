using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Parser.RomanNumerals;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        Game GetGame(string title);
        RemoteGame Map(ParsedGameInfo parsedGameInfo, int steamAppId, int igdbId, SearchCriteriaBase searchCriteria = null);
        RemoteGame Map(ParsedGameInfo parsedGameInfo, int gameId);
        ParsedGameInfo ParseMinimalPathGameInfo(string path);
    }

    public class ParsingService : IParsingService
    {
        private static HashSet<ArabicRomanNumeral> _arabicRomanNumeralMappings;

        private readonly IGameService _gameService;
        private readonly Logger _logger;

        public ParsingService(IGameService gameService,
                              Logger logger)
        {
            _gameService = gameService;
            _logger = logger;

            if (_arabicRomanNumeralMappings == null)
            {
                _arabicRomanNumeralMappings = RomanNumeralParser.GetArabicRomanNumeralsMapping();
            }
        }

        public ParsedGameInfo ParseMinimalPathGameInfo(string path)
        {
            var fileInfo = new FileInfo(path);

            var result = Parser.ParseGameTitle(fileInfo.Name, true);

            if (result == null)
            {
                _logger.Debug("Attempting to parse game info using directory and file names. '{0}'", fileInfo.Directory.Name);
                result = Parser.ParseGameTitle(fileInfo.Directory.Name + " " + fileInfo.Name);
            }

            if (result == null)
            {
                _logger.Debug("Attempting to parse game info using directory name. '{0}'", fileInfo.Directory.Name);
                result = Parser.ParseGameTitle(fileInfo.Directory.Name + fileInfo.Extension);
            }

            return result;
        }

        public Game GetGame(string title)
        {
            var parsedGameInfo = Parser.ParseGameTitle(title);

            if (parsedGameInfo == null)
            {
                return _gameService.FindByTitle(title);
            }

            var result = TryGetGameByTitleAndOrYear(parsedGameInfo);

            if (result != null)
            {
                return result.Game;
            }

            return TryGetGameByTitlePrefix(parsedGameInfo);
        }

        // DLC/update/season-pass titles embed the addon's own name
        // ("Hades The Blood Price"), so the exact-title lookup fails.
        // For base-requiring content, match the library game whose clean
        // title is the longest PREFIX of the release's clean title (#149) —
        // but only where the release's title continues into an add-on marker
        // at that point, and never over a game that matches exactly (#206).
        private Game TryGetGameByTitlePrefix(ParsedGameInfo parsedGameInfo)
        {
            if (!parsedGameInfo.ContentType.RequiresBaseGame())
            {
                return null;
            }

            var cleanReleaseTitle = parsedGameInfo.PrimaryGameTitle.CleanGameTitle();

            var candidates = (_gameService.GetAllGames() ?? new List<Game>())
                .Where(g => g.GameMetadata.Value.CleanTitle.IsNotNullOrWhiteSpace() &&
                            cleanReleaseTitle.StartsWith(g.GameMetadata.Value.CleanTitle))
                .OrderByDescending(g => g.GameMetadata.Value.CleanTitle.Length)
                .ToList();

            // Prefer an exact title match: a release whose own title is a known
            // game belongs to that game, not to a shorter title it starts with.
            var exactMatch = candidates.FirstOrDefault(g => g.GameMetadata.Value.CleanTitle == cleanReleaseTitle);

            if (exactMatch != null)
            {
                return exactMatch;
            }

            var prefixMatch = candidates.FirstOrDefault(g => IsAddonOfGameTitle(parsedGameInfo, g.GameMetadata.Value.CleanTitle));

            if (prefixMatch != null)
            {
                _logger.Debug("Matched base-requiring release '{0}' to game '{1}' by title prefix", parsedGameInfo.PrimaryGameTitle, prefixMatch.Title);
            }

            return prefixMatch;
        }

        // What is left of the release's title once the game's clean title has
        // been consumed off the front, or null when the game's title is not a
        // WHOLE-WORD prefix of it. Each prefix is cleaned as a whole rather
        // than word by word, because cleaning drops articles ("of", "the") and
        // so is not distributive over words.
        private static string GetTitleRemainder(string releaseTitle, string cleanGameTitle)
        {
            if (releaseTitle.IsNullOrWhiteSpace() || cleanGameTitle.IsNullOrWhiteSpace())
            {
                return null;
            }

            var words = releaseTitle.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 1; i <= words.Length; i++)
            {
                if (string.Join(" ", words.Take(i)).CleanGameTitle() == cleanGameTitle)
                {
                    return string.Join(" ", words.Skip(i));
                }
            }

            return null;
        }

        // True when the release's title reads as an add-on OF this game: the
        // game's title, then an add-on marker ("<Game> Update v1.2", "<Game>
        // DLC <name>", "<Game> Wave 1 DLC").
        //
        // An extra word that is not a marker is what #206 was about: "<Game>
        // <Word>" is a separate IGDB entry, and its base release and 17 of its
        // updates were matched to "<Game>" and imported into its folder. A DLC
        // release is still allowed to lead with the DLC's own name, since that
        // is how DLC releases are named ("Hades The Blood Price DLC") and the
        // marker then sits at the end — an UPDATE gets no such latitude, as
        // "<Game> <Word> Update v2.3.0" is an update of "<Game> <Word>".
        private static bool IsAddonOfGameTitle(ParsedGameInfo parsedGameInfo, string cleanGameTitle)
        {
            return parsedGameInfo.GameTitles.Any(releaseTitle =>
            {
                var remainder = GetTitleRemainder(releaseTitle, cleanGameTitle);

                if (remainder == null)
                {
                    return false;
                }

                if (remainder.IsNullOrWhiteSpace() || QualityParser.StartsWithAddonMarker(remainder))
                {
                    return true;
                }

                return parsedGameInfo.ContentType.IncludesDlc() &&
                       !parsedGameInfo.ContentType.IncludesBaseGame() &&
                       QualityParser.ParseContentType(remainder).IncludesDlc();
            });
        }

        // Prefer the closest title match over a prefix match: when another game
        // in the library matches MORE of the release's title than the searched
        // game does — up to all of it — that game owns the release. "<Game>
        // <Word>" is its own IGDB entry, and a search for "<Game>" returns its
        // releases too (#206).
        private bool AnotherGameIsACloserTitleMatch(ParsedGameInfo parsedGameInfo, Game searchedGame, string matchedCleanTitle)
        {
            var allGames = _gameService.GetAllGames();

            if (allGames == null)
            {
                return false;
            }

            var cleanTitles = parsedGameInfo.GameTitles.Select(t => t.CleanGameTitle()).ToList();

            return allGames.Any(g => g.Id != searchedGame.Id &&
                                     g.GameMetadata.Value.CleanTitle.IsNotNullOrWhiteSpace() &&
                                     g.GameMetadata.Value.CleanTitle.Length > matchedCleanTitle.Length &&
                                     cleanTitles.Any(ct => ct.StartsWith(g.GameMetadata.Value.CleanTitle)));
        }

        public RemoteGame Map(ParsedGameInfo parsedGameInfo, int steamAppId, int igdbId, SearchCriteriaBase searchCriteria = null)
        {
            return Map(parsedGameInfo, steamAppId, igdbId, null, searchCriteria);
        }

        // FindGame, not GetGame: the only callers are the tracked-download resolver's
        // grab-history fallbacks, where the id comes from history and the game may since
        // have been deleted. That is an unresolved download, not an exception — GetGame
        // would throw and surface as the misleading "Unable to parse game from title".
        public RemoteGame Map(ParsedGameInfo parsedGameInfo, int gameId)
        {
            return new RemoteGame
            {
                ParsedGameInfo = parsedGameInfo,
                Game = _gameService.FindGame(gameId)
            };
        }

        private RemoteGame Map(ParsedGameInfo parsedGameInfo, int steamAppId, int igdbId, Game game, SearchCriteriaBase searchCriteria)
        {
            var remoteGame = new RemoteGame
            {
                ParsedGameInfo = parsedGameInfo
            };

            if (game == null)
            {
                var gameMatch = FindGame(parsedGameInfo, steamAppId, igdbId, searchCriteria);

                if (gameMatch != null)
                {
                    game = gameMatch.Game;
                    remoteGame.GameMatchType = gameMatch.MatchType;
                }
            }

            if (game != null)
            {
                remoteGame.Game = game;
            }

            remoteGame.Languages = parsedGameInfo.Languages;

            if (searchCriteria != null)
            {
                remoteGame.GameRequested = remoteGame.Game?.Id == searchCriteria.Game?.Id;
            }

            return remoteGame;
        }

        private FindGameResult FindGame(ParsedGameInfo parsedGameInfo, int steamAppId, int igdbId, SearchCriteriaBase searchCriteria)
        {
            FindGameResult result = null;

            // Primary identifier - Steam App ID (try first)
            if (steamAppId > 0)
            {
                result = TryGetGameBySteamAppId(parsedGameInfo, steamAppId);
            }

            // Secondary identifier - IGDB ID (fallback)
            if (result == null && igdbId > 0)
            {
                result = TryGetGameByIgdbId(parsedGameInfo, igdbId);
            }

            if (result == null)
            {
                if (searchCriteria != null)
                {
                    result = TryGetGameBySearchCriteria(parsedGameInfo, steamAppId, igdbId, searchCriteria);
                }
                else
                {
                    result = TryGetGameByTitleAndOrYear(parsedGameInfo);
                }
            }

            // RSS has no search criteria; base-requiring releases still need
            // the prefix fallback to find their game (#149).
            if (result == null)
            {
                var prefixMatch = TryGetGameByTitlePrefix(parsedGameInfo);

                if (prefixMatch != null)
                {
                    result = new FindGameResult(prefixMatch, GameMatchType.Title);
                }
            }

            if (result == null)
            {
                _logger.Debug($"No matching game for titles '{string.Join(", ", parsedGameInfo.GameTitles)} ({parsedGameInfo.Year})'");
            }

            return result;
        }

        /// <summary>
        /// Try to find game by Steam App ID - primary identifier
        /// </summary>
        private FindGameResult TryGetGameBySteamAppId(ParsedGameInfo parsedGameInfo, int steamAppId)
        {
            var game = _gameService.FindBySteamAppId(steamAppId);

            // Should fix practically all problems, where indexer is shite at adding correct IDs to games.
            if (game != null && (parsedGameInfo.Year < 1800 || game.GameMetadata.Value.Year == parsedGameInfo.Year || game.GameMetadata.Value.SecondaryYear == parsedGameInfo.Year))
            {
                return new FindGameResult(game, GameMatchType.Id);
            }

            return null;
        }

        /// <summary>
        /// Try to find game by IGDB ID - secondary identifier
        /// </summary>
        private FindGameResult TryGetGameByIgdbId(ParsedGameInfo parsedGameInfo, int igdbId)
        {
            var game = _gameService.FindByIgdbId(igdbId);

            // Should fix practically all problems, where indexer is shite at adding correct IDs to games.
            if (game != null && (parsedGameInfo.Year < 1800 || game.GameMetadata.Value.Year == parsedGameInfo.Year || game.GameMetadata.Value.SecondaryYear == parsedGameInfo.Year))
            {
                return new FindGameResult(game, GameMatchType.Id);
            }

            return null;
        }

        private FindGameResult TryGetGameByTitleAndOrYear(ParsedGameInfo parsedGameInfo)
        {
            var candidates = _gameService.FindByTitleCandidates(parsedGameInfo.GameTitles, out var otherTitles);

            Game gameByTitleAndOrYear;
            if (parsedGameInfo.Year > 1800)
            {
                gameByTitleAndOrYear = _gameService.FindByTitle(parsedGameInfo.GameTitles, parsedGameInfo.Year, otherTitles, candidates);
                if (gameByTitleAndOrYear != null)
                {
                    return new FindGameResult(gameByTitleAndOrYear, GameMatchType.Title);
                }

                return null;
            }

            gameByTitleAndOrYear = _gameService.FindByTitle(parsedGameInfo.GameTitles, null, otherTitles, candidates);
            if (gameByTitleAndOrYear != null)
            {
                return new FindGameResult(gameByTitleAndOrYear, GameMatchType.Title);
            }

            return null;
        }

        private FindGameResult TryGetGameBySearchCriteria(ParsedGameInfo parsedGameInfo, int steamAppId, int igdbId, SearchCriteriaBase searchCriteria)
        {
            Game possibleGame = null;

            var possibleTitles = new List<string>
            {
                searchCriteria.Game.GameMetadata.Value.CleanTitle
            };
            possibleTitles.AddIfNotNull(searchCriteria.Game.GameMetadata.Value.CleanOriginalTitle);
            possibleTitles.AddRange(searchCriteria.Game.GameMetadata.Value.AlternativeTitles.Select(t => t.CleanTitle));
            possibleTitles.AddRange(searchCriteria.Game.GameMetadata.Value.Translations.Select(t => t.CleanTitle));

            var cleanTitles = parsedGameInfo.GameTitles.Select(t => t.CleanGameTitle()).ToArray();

            if (possibleTitles.Any(pt =>
                cleanTitles.Contains(pt)
                || _arabicRomanNumeralMappings.Any(mn =>
                    cleanTitles.Contains(pt.Replace(mn.ArabicNumeralAsString, mn.RomanNumeralLowerCase))
                    || cleanTitles.Any(t => t.Replace(mn.ArabicNumeralAsString, mn.RomanNumeralLowerCase) == pt))))
            {
                possibleGame = searchCriteria.Game;
            }

            if (possibleGame != null)
            {
                if (parsedGameInfo.Year < 1800 || possibleGame.GameMetadata.Value.Year == parsedGameInfo.Year || possibleGame.GameMetadata.Value.SecondaryYear == parsedGameInfo.Year)
                {
                    return new FindGameResult(possibleGame, GameMatchType.Title);
                }
            }

            // DLC/update/season-pass titles embed the addon's own name
            // ("Hades The Blood Price"), so the exact-title check fails. For
            // base-requiring content, accept the searched game when its clean
            // title is a prefix of the release's clean title (#149) — same
            // fallback GetGame applies outside of search.
            //
            // The release's title has to continue into an add-on marker where
            // the searched game's title ends, and no other game in the library
            // may match more of that title: a search for "<Game>" returns the
            // releases of "<Game> <Word>" too, and those belong to that game
            // (#206).
            var addonOfTitle = parsedGameInfo.ContentType.RequiresBaseGame()
                ? possibleTitles.FirstOrDefault(pt => pt.IsNotNullOrWhiteSpace() && IsAddonOfGameTitle(parsedGameInfo, pt))
                : null;

            if (addonOfTitle != null &&
                !AnotherGameIsACloserTitleMatch(parsedGameInfo, searchCriteria.Game, addonOfTitle))
            {
                _logger.Debug("Matched base-requiring release '{0}' to searched game '{1}' by title prefix", parsedGameInfo.PrimaryGameTitle, searchCriteria.Game.Title);
                return new FindGameResult(searchCriteria.Game, GameMatchType.Title);
            }

            // Fallback: check if ALL significant words from the game title appear in the release title.
            // This handles cases where DLC/expansion names are inside parentheses and stripped by the parser.
            // e.g. "Factorio (v2.0.7 + Space Age DLC) [FitGirl Repack]" should match "Factorio: Space Age"
            if (possibleGame == null && parsedGameInfo.ReleaseTitle.IsNotNullOrWhiteSpace())
            {
                var releaseWords = Regex.Split(parsedGameInfo.ReleaseTitle.ToLowerInvariant(), @"[^a-z0-9]+")
                    .Where(w => w.Length > 0)
                    .ToHashSet();

                var gameTitlesToCheck = new List<string> { searchCriteria.Game.GameMetadata.Value.Title };
                if (searchCriteria.Game.GameMetadata.Value.OriginalTitle.IsNotNullOrWhiteSpace())
                {
                    gameTitlesToCheck.Add(searchCriteria.Game.GameMetadata.Value.OriginalTitle);
                }

                // The words the parser DID read as the release's title. The
                // fallback below is only for the words it could not see.
                var parsedTitleWords = Regex.Split(string.Join(" ", parsedGameInfo.GameTitles).ToLowerInvariant(), @"[^a-z0-9]+")
                    .Where(w => w.Length > 0)
                    .ToHashSet();

                foreach (var title in gameTitlesToCheck)
                {
                    // Keep digit-bearing words regardless of length: dropping the
                    // "2" from "Half-Life 2" would make any Half-Life release
                    // (Alyx, HL1) satisfy the all-words check and be grabbed as
                    // the searched sequel.
                    var titleWords = Regex.Split(title.ToLowerInvariant(), @"[^a-z0-9]+")
                        .Where(w => w.Length >= 2 || w.Any(char.IsDigit))
                        .ToList();

                    if (titleWords.Count >= 2 && titleWords.All(w => releaseWords.Contains(w)))
                    {
                        // This fallback exists for a title the parser could not
                        // see at all, because it sat in parentheses: "Factorio
                        // (… + Space Age DLC)" for "Factorio: Space Age". When
                        // every word of the game's title is already in the
                        // title the parser DID read, the exact-title and
                        // prefix rules above have had their say — and a parsed
                        // title that adds a word on top of the game's title is
                        // a different game. Without this, every base release
                        // of "<Game> <Word>" matched a search for "<Game>" and
                        // was imported into its folder (#206).
                        if (titleWords.All(w => parsedTitleWords.Contains(w)))
                        {
                            continue;
                        }

                        possibleGame = searchCriteria.Game;
                        break;
                    }
                }

                if (possibleGame != null)
                {
                    if (parsedGameInfo.Year < 1800 || possibleGame.GameMetadata.Value.Year == parsedGameInfo.Year || possibleGame.GameMetadata.Value.SecondaryYear == parsedGameInfo.Year)
                    {
                        return new FindGameResult(possibleGame, GameMatchType.Title);
                    }
                }
            }

            // Primary identifier - Steam App ID
            if (steamAppId > 0 && steamAppId == searchCriteria.Game.SteamAppId)
            {
                return new FindGameResult(searchCriteria.Game, GameMatchType.Id);
            }

            // Secondary identifier - IGDB ID
            if (igdbId > 0 && igdbId == searchCriteria.Game.IgdbId)
            {
                return new FindGameResult(searchCriteria.Game, GameMatchType.Id);
            }

            return null;
        }
    }
}
