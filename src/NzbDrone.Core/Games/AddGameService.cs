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
        private readonly Logger _logger;

        public AddGameService(IGameService gameService,
                                IGameMetadataService gameMetadataService,
                                IProvideGameInfo gameInfo,
                                IBuildFileNames fileNameBuilder,
                                IAddGameValidator addGameValidator,
                                Logger logger)
        {
            _gameService = gameService;
            _gameMetadataService = gameMetadataService;
            _gameInfo = gameInfo;
            _fileNameBuilder = fileNameBuilder;
            _addGameValidator = addGameValidator;
            _logger = logger;
        }

        public Game AddGame(Game newGame)
        {
            Ensure.That(newGame, () => newGame).IsNotNull();

            newGame = AddSkyhookData(newGame);
            newGame = SetPropertiesAndValidate(newGame);

            _logger.Info("Adding Game {0} Path: [{1}]", newGame, newGame.Path);

            _gameMetadataService.Upsert(newGame.GameMetadata.Value);
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

            _gameMetadataService.UpsertMany(gamesToAdd.Select(x => x.GameMetadata.Value).ToList());
            gamesToAdd.ForEach(x => x.GameMetadataId = x.GameMetadata.Value.Id);

            return _gameService.AddGames(gamesToAdd);
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

                game.GameMetadata = metadata;
                game.ApplyChanges(newGame);
                return game;
            }

            try
            {
                game.GameMetadata = _gameInfo.GetGameInfo(newGame.IgdbId);
            }
            catch (GameNotFoundException)
            {
                _logger.Error("IgdbId {0} was not found, it may have been removed from IGDB. Path: {1}", newGame.IgdbId, newGame.Path);

                throw new ValidationException(new List<ValidationFailure>
                                              {
                                                  new ValidationFailure("IgdbId", $"A game with this ID was not found. Path: {newGame.Path}", newGame.IgdbId)
                                              });
            }

            game.ApplyChanges(newGame);

            return game;
        }

        private Game SetPropertiesAndValidate(Game newGame)
        {
            if (string.IsNullOrWhiteSpace(newGame.Path))
            {
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
