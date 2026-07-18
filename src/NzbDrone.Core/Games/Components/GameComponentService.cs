using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Games.Components
{
    public interface IGameComponentService
    {
        List<GameComponent> GetByGame(int gameId);
        GameComponent Get(int id);
        GameComponent SetMonitored(int id, bool monitored);
        void EnsureComponents(Game game);
    }

    public class GameComponentService : IGameComponentService,
                                        IHandle<GameUpdatedEvent>,
                                        IHandle<GameScannedEvent>,
                                        IHandle<GamesDeletedEvent>
    {
        private readonly IGameComponentRepository _componentRepository;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public GameComponentService(IGameComponentRepository componentRepository,
                                    IMediaFileService mediaFileService,
                                    Logger logger)
        {
            _componentRepository = componentRepository;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public List<GameComponent> GetByGame(int gameId)
        {
            return _componentRepository.GetByGame(gameId);
        }

        public GameComponent Get(int id)
        {
            return _componentRepository.Get(id);
        }

        public GameComponent SetMonitored(int id, bool monitored)
        {
            var component = _componentRepository.Get(id);
            component.Monitored = monitored;

            return _componentRepository.Update(component);
        }

        // Idempotent reconciliation of a game's component slots:
        // - base component always exists
        // - DLC components from metadata DlcReferences (id + name, no extra
        //   API calls needed) — created monitored=false so a fresh library
        //   doesn't hunt every DLC by default; users opt in per DLC
        // - update/dlc components discovered from imported files (subfolder
        //   units), and files linked to their component via ComponentId
        public void EnsureComponents(Game game)
        {
            var existing = _componentRepository.GetByGame(game.Id);
            var files = _mediaFileService.GetFilesByGame(game.Id);
            var toInsert = new List<GameComponent>();
            var filesToUpdate = new List<GameFile>();

            var baseComponent = FindOrStage(existing, toInsert, game, GameComponentType.Base, "base", game.Title, monitored: true);

            var metadata = game.GameMetadata?.Value;

            if (metadata?.DlcReferences != null)
            {
                foreach (var dlc in metadata.DlcReferences.Where(d => d.Id > 0 && d.Name.IsNotNullOrWhiteSpace()))
                {
                    var component = FindOrStage(existing, toInsert, game, GameComponentType.Dlc, $"igdb:{dlc.Id}", dlc.Name, monitored: false);
                    component.ExternalId = dlc.Id;
                }
            }

            foreach (var file in files)
            {
                var component = GetComponentForFile(existing, toInsert, game, file, baseComponent);

                if (component != null && file.ComponentId != component.Id)
                {
                    // Component may not have an Id yet (staged); linking happens
                    // after insert below for those.
                    if (component.Id > 0)
                    {
                        file.ComponentId = component.Id;
                        filesToUpdate.Add(file);
                    }
                }
            }

            if (toInsert.Any())
            {
                _componentRepository.InsertMany(toInsert);
                _logger.Debug("Created {0} component slot(s) for {1}", toInsert.Count, game.Title);

                // Second pass: link files to freshly-inserted components.
                var all = _componentRepository.GetByGame(game.Id);

                foreach (var file in files)
                {
                    var component = GetComponentForFile(all, new List<GameComponent>(), game, file, all.FirstOrDefault(c => c.ComponentType == GameComponentType.Base));

                    if (component is { Id: > 0 } && file.ComponentId != component.Id)
                    {
                        file.ComponentId = component.Id;

                        if (!filesToUpdate.Contains(file))
                        {
                            filesToUpdate.Add(file);
                        }
                    }
                }
            }

            if (filesToUpdate.Any())
            {
                _mediaFileService.Update(filesToUpdate);
            }
        }

        private static GameComponent GetComponentForFile(List<GameComponent> existing, List<GameComponent> toInsert, Game game, GameFile file, GameComponent baseComponent)
        {
            if (file.RelativePath.IsNullOrWhiteSpace())
            {
                return baseComponent;
            }

            var normalized = file.RelativePath.Replace('\\', '/');

            if (normalized.StartsWith("Updates/"))
            {
                var key = normalized.Substring("Updates/".Length).Split('/')[0];

                return FindOrStage(existing, toInsert, game, GameComponentType.Update, key, key, monitored: true);
            }

            if (normalized.StartsWith("DLC/"))
            {
                var name = normalized.Substring("DLC/".Length).Split('/')[0];

                // Imported DLC keyed by its folder name; a later metadata match
                // could merge it with an igdb-keyed slot (future work).
                return FindOrStage(existing, toInsert, game, GameComponentType.Dlc, $"import:{name}", name, monitored: true);
            }

            // Legacy file-based records belong to the base slot.
            return baseComponent;
        }

        private static GameComponent FindOrStage(List<GameComponent> existing, List<GameComponent> toInsert, Game game, GameComponentType type, string key, string title, bool monitored)
        {
            var found = existing.FirstOrDefault(c => c.ComponentType == type && c.Key == key) ??
                        toInsert.FirstOrDefault(c => c.ComponentType == type && c.Key == key);

            if (found != null)
            {
                return found;
            }

            var component = new GameComponent
            {
                GameId = game.Id,
                ComponentType = type,
                Key = key,
                Title = title,
                Monitored = monitored,
                Added = DateTime.UtcNow
            };

            toInsert.Add(component);

            return component;
        }

        public void Handle(GameUpdatedEvent message)
        {
            EnsureComponents(message.Game);
        }

        public void Handle(GameScannedEvent message)
        {
            EnsureComponents(message.Game);
        }

        public void Handle(GamesDeletedEvent message)
        {
            foreach (var game in message.Games)
            {
                _componentRepository.DeleteByGame(game.Id);
            }
        }
    }
}
