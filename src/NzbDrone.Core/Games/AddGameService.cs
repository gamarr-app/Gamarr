using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Games
{
    public interface IAddGameService
    {
        Game AddGame(Game newGame);
        List<Game> AddGames(List<Game> newGames, bool ignoreErrors = false);
    }

    public class AddGameService : IAddGameService
    {
        private readonly IGameService _gameService;
        private readonly IGameMetadataService _gameMetadataService;
        private readonly IProvideGameInfo _gameInfo;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IAddGameValidator _addGameValidator;
        private readonly IPlatformRootFolderService _platformRootFolderService;
        private readonly Logger _logger;

        public AddGameService(IGameService gameService,
                                IGameMetadataService gameMetadataService,
                                IProvideGameInfo gameInfo,
                                IBuildFileNames fileNameBuilder,
                                IAddGameValidator addGameValidator,
                                IPlatformRootFolderService platformRootFolderService,
                                Logger logger)
        {
            _gameService = gameService;
            _gameMetadataService = gameMetadataService;
            _gameInfo = gameInfo;
            _fileNameBuilder = fileNameBuilder;
            _addGameValidator = addGameValidator;
            _platformRootFolderService = platformRootFolderService;
            _logger = logger;
        }

        public Game AddGame(Game newGame)
        {
            Ensure.That(newGame, () => newGame).IsNotNull();

            newGame = AddSkyhookData(newGame);
            newGame = SetPropertiesAndValidate(newGame);

            _logger.Info("Adding Game {0} Path: [{1}]", newGame, newGame.Path);

            _gameMetadataService.Upsert(newGame.GameMetadata.Value);

            // Platform-specific duplicate adds (#150): Games.GameMetadataId is
            // unique, so a second entry for the same title gets its own copy of
            // the metadata row (each entry refreshes its copy independently).
            if (newGame.Platform != PlatformFamily.Unknown &&
                _gameService.GetAllGames().Any(g => g.GameMetadataId == newGame.GameMetadata.Value.Id))
            {
                newGame.GameMetadata = CloneMetadata(newGame.GameMetadata.Value);
            }

            newGame.GameMetadataId = newGame.GameMetadata.Value.Id;

            _gameService.UpdateTags(newGame);

            _gameService.AddGame(newGame);

            return newGame;
        }

        public List<Game> AddGames(List<Game> newGames, bool ignoreErrors = false)
        {
            var added = DateTime.UtcNow;
            var gamesToAdd = new List<Game>();

            foreach (var m in newGames)
            {
                if (m.Path.IsNullOrWhiteSpace())
                {
                    _logger.Info("Adding Game {0} Root Folder Path: [{1}]", m, m.RootFolderPath);
                }
                else
                {
                    _logger.Info("Adding Game {0} Path: [{1}]", m, m.Path);
                }

                try
                {
                    var game = AddSkyhookData(m);
                    game = SetPropertiesAndValidate(game);

                    game.Added = added;

                    EnsureNotBatchDuplicate(game, gamesToAdd);

                    gamesToAdd.Add(game);
                }
                catch (ValidationException ex)
                {
                    if (!ignoreErrors)
                    {
                        throw;
                    }

                    _logger.Debug("IgdbId {0} was not added due to validation failures. {1}", m.IgdbId, ex.Message);
                }
            }

            // UpsertMany matches rows by SteamAppId/IgdbId, so two entries for
            // the same title would share one metadata row, and
            // Games.GameMetadataId is unique. Only the first entry of each
            // title is upserted; later (different-platform) entries get a
            // cloned row, as in the single-add path.
            var clonedFrom = new Dictionary<Game, Game>();

            foreach (var game in gamesToAdd)
            {
                var first = gamesToAdd.First(x => ReferenceEquals(x, game) || SameGame(x, game));

                if (!ReferenceEquals(first, game))
                {
                    clonedFrom[game] = first;
                }
            }

            _gameMetadataService.UpsertMany(gamesToAdd.Where(x => !clonedFrom.ContainsKey(x)).Select(x => x.GameMetadata.Value).ToList());

            var usedMetadataIds = new HashSet<int>();
            var result = new List<Game>();

            foreach (var game in gamesToAdd)
            {
                if (clonedFrom.TryGetValue(game, out var source))
                {
                    game.GameMetadata = CloneMetadata(source.GameMetadata.Value);
                }
                else
                {
                    var metadataId = game.GameMetadata.Value.Id;

                    // The validator only checks paths, so an existing game on
                    // this metadata row (or an earlier entry in this batch)
                    // would hit the unique index on insert.
                    if (metadataId > 0 && (usedMetadataIds.Contains(metadataId) || _gameService.ExistsByMetadataId(metadataId)))
                    {
                        if (game.Platform == PlatformFamily.Unknown)
                        {
                            var failure = new ValidationException(new List<ValidationFailure>
                            {
                                new ValidationFailure("IgdbId", $"'{game.Title}' is already in the library.", game.IgdbId)
                            });

                            if (!ignoreErrors)
                            {
                                throw failure;
                            }

                            _logger.Debug("IgdbId {0} was not added, the game already exists. {1}", game.IgdbId, failure.Message);
                            continue;
                        }

                        game.GameMetadata = CloneMetadata(game.GameMetadata.Value);
                    }
                }

                if (game.GameMetadata.Value.Id > 0)
                {
                    usedMetadataIds.Add(game.GameMetadata.Value.Id);
                }

                game.GameMetadataId = game.GameMetadata.Value.Id;
                result.Add(game);
            }

            return _gameService.AddGames(result);
        }

        private GameMetadata CloneMetadata(GameMetadata metadata)
        {
            var metadataCopy = metadata.JsonClone();
            metadataCopy.Id = 0;

            return _gameMetadataService.Insert(metadataCopy);
        }

        private static bool SameGame(Game a, Game b)
        {
            var am = a.GameMetadata.Value;
            var bm = b.GameMetadata.Value;

            return (am.IgdbId > 0 && am.IgdbId == bm.IgdbId) ||
                   (am.SteamAppId > 0 && am.SteamAppId == bm.SteamAppId);
        }

        // Same title in the same batch is only allowed once per platform. An
        // Unknown platform is platform-agnostic and conflicts with any other.
        private static void EnsureNotBatchDuplicate(Game game, List<Game> accepted)
        {
            var clash = accepted.Any(x => SameGame(x, game) &&
                                          (x.Platform == game.Platform ||
                                           x.Platform == PlatformFamily.Unknown ||
                                           game.Platform == PlatformFamily.Unknown));

            if (clash)
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new ValidationFailure("IgdbId", $"'{game.Title}' appears more than once in this request for platform {game.Platform}. Path: {game.Path}", game.IgdbId)
                });
            }
        }

        private Game AddSkyhookData(Game newGame)
        {
            var game = new Game();

            // Get steamAppId from either the game directly or from its metadata
            var steamAppId = newGame.SteamAppId > 0 ? newGame.SteamAppId : newGame.GameMetadata?.Value?.SteamAppId ?? 0;

            // If we have a SteamAppId but no IgdbId, fetch full metadata from Steam
            if (newGame.IgdbId <= 0 && steamAppId > 0)
            {
                _logger.Debug("Adding Steam game without IGDB ID. SteamAppId: {0}", steamAppId);
                var metadata = _gameInfo.GetGameBySteamAppId(steamAppId);

                if (metadata == null)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure("SteamAppId", $"A game with Steam App ID {steamAppId} was not found.", steamAppId)
                    });
                }

                EnsureNotDlc(metadata);

                game.GameMetadata = metadata;
                game.ApplyChanges(newGame);
                return ApplyMetadataPlatform(game);
            }

            GameMetadata igdbMetadata;

            try
            {
                igdbMetadata = _gameInfo.GetGameInfoByIgdbId(newGame.IgdbId);
            }
            catch (GameNotFoundException)
            {
                igdbMetadata = null;
            }

            // The aggregate proxy returns null (rather than throwing) when the
            // game wasn't found or IGDB credentials are unset; wrapping null in
            // the lazy metadata crashed later with an NRE instead of a
            // validation error (and killed whole import-list sync batches).
            if (igdbMetadata == null)
            {
                _logger.Error("IgdbId {0} was not found, it may have been removed from IGDB. Path: {1}", newGame.IgdbId, newGame.Path);

                throw new ValidationException(new List<ValidationFailure>
                                              {
                                                  new ValidationFailure("IgdbId", $"A game with this ID was not found. Path: {newGame.Path}", newGame.IgdbId)
                                              });
            }

            EnsureNotDlc(igdbMetadata);

            game.GameMetadata = igdbMetadata;
            game.ApplyChanges(newGame);

            return ApplyMetadataPlatform(game);
        }

        // A library entry's Platform decides which releases it accepts
        // (PlatformSpecification tier 1), but nothing ever derived it from the
        // metadata we just fetched, so every add landed on Unknown even for a
        // console exclusive. Only fill it in when the metadata is unambiguous;
        // an explicit choice from the caller always wins.
        private Game ApplyMetadataPlatform(Game game)
        {
            if (game.Platform != PlatformFamily.Unknown)
            {
                return game;
            }

            var family = GamePlatform.UnambiguousFamily(game.GameMetadata?.Value?.Platforms);

            if (family != PlatformFamily.Unknown)
            {
                _logger.Debug("Platform for '{0}' resolved to {1} from metadata", game.GameMetadata.Value.Title, family);
                game.Platform = family;
            }

            return game;
        }

        // DLC belongs under its base game as a component slot, not as a
        // standalone library entry (#149). Blocking here covers interactive
        // adds, Discover, and import-list syncs (which skip failures), so a
        // Steam library list no longer floods the library with DLC entries.
        private static void EnsureNotDlc(GameMetadata metadata)
        {
            if (metadata.GameType != GameType.DlcAddon)
            {
                return;
            }

            throw new ValidationException(new List<ValidationFailure>
            {
                new ValidationFailure("GameType", $"'{metadata.Title}' is DLC, not a standalone game. Add the base game instead and monitor this DLC in its Components panel.", metadata.Title)
            });
        }

        // The root folder stays a per-entry choice made at add time; this only
        // supplies the default when the caller didn't pick one (API clients and
        // import-list syncs; the UI pre-fills the same value in the add dialog).
        // Platform has already been resolved from metadata by this point, so a
        // Switch exclusive lands in the Switch folder rather than the global one.
        private void ApplyDefaultRootFolder(Game newGame)
        {
            if (newGame.RootFolderPath.IsNotNullOrWhiteSpace())
            {
                return;
            }

            var defaultRootFolderPath = _platformRootFolderService.GetDefaultRootFolderPath(newGame.Platform);

            if (defaultRootFolderPath.IsNullOrWhiteSpace())
            {
                return;
            }

            _logger.Debug("Using default root folder [{0}] for platform {1}", defaultRootFolderPath, newGame.Platform);

            newGame.RootFolderPath = defaultRootFolderPath;
        }

        private Game SetPropertiesAndValidate(Game newGame)
        {
            if (string.IsNullOrWhiteSpace(newGame.Path))
            {
                ApplyDefaultRootFolder(newGame);

                // Nothing left to build a path from. Report the missing
                // configuration rather than letting Path.Combine throw an
                // ArgumentNullException at a caller that never touched a path.
                if (newGame.RootFolderPath.IsNullOrWhiteSpace())
                {
                    throw new ValidationException(new List<ValidationFailure>
                                                  {
                                                      new ValidationFailure(nameof(newGame.RootFolderPath), PlatformRootFolderService.NoDefaultRootFolderError(newGame.Platform))
                                                  });
                }

                var folderName = _fileNameBuilder.GetGameFolder(newGame);
                newGame.Path = Path.Combine(newGame.RootFolderPath, folderName);
            }

            newGame.GameMetadata.Value.CleanTitle = newGame.Title.CleanGameTitle();
            newGame.GameMetadata.Value.SortTitle = GameTitleNormalizer.Normalize(newGame.Title, newGame.IgdbId);
            newGame.Added = DateTime.UtcNow;

            var validationResult = _addGameValidator.Validate(newGame);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            return newGame;
        }
    }
}
