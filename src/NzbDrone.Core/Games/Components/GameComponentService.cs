using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RomCatalog;

namespace NzbDrone.Core.Games.Components
{
    public interface IGameComponentService
    {
        List<GameComponent> GetByGame(int gameId);
        List<GameComponent> GetMonitoredMissingDlc();
        GameComponent Get(int id);
        GameComponent SetComponentOptions(int id, bool monitored, int qualityProfileId);
        void EnsureComponents(Game game);
    }

    public class GameComponentService : IGameComponentService,
                                        IHandle<GameUpdatedEvent>,
                                        IHandle<GameScannedEvent>,
                                        IHandle<GameFileAddedEvent>,
                                        IHandle<GamesDeletedEvent>
    {
        private readonly IGameComponentRepository _componentRepository;
        private readonly IMediaFileService _mediaFileService;
        private readonly IGameService _gameService;
        private readonly INoIntroCatalogEntryRepository _noIntroCatalogEntryRepository;
        private readonly IDiskProvider _diskProvider;
        private readonly NoIntroGameComponentPlanner _noIntroComponentPlanner;
        private readonly Logger _logger;

        public GameComponentService(IGameComponentRepository componentRepository,
                                     IMediaFileService mediaFileService,
                                     IGameService gameService,
                                     INoIntroCatalogEntryRepository noIntroCatalogEntryRepository,
                                     IDiskProvider diskProvider,
                                     Logger logger)
        {
            _componentRepository = componentRepository;
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _noIntroCatalogEntryRepository = noIntroCatalogEntryRepository;
            _diskProvider = diskProvider;
            _noIntroComponentPlanner = new NoIntroGameComponentPlanner(new NoIntroComponentClassifier());
            _logger = logger;
        }

        public List<GameComponent> GetByGame(int gameId)
        {
            return _componentRepository.GetByGame(gameId);
        }

        // Monitored DLC slots with no file linked — the component-level
        // analog of "wanted: missing".
        public List<GameComponent> GetMonitoredMissingDlc()
        {
            var slots = _componentRepository.GetMonitoredDlc();
            var missing = new List<GameComponent>();

            foreach (var gameSlots in slots.GroupBy(s => s.GameId))
            {
                var linkedIds = _mediaFileService.GetFilesByGame(gameSlots.Key)
                                                 .Select(f => f.ComponentId)
                                                 .ToHashSet();

                missing.AddRange(gameSlots.Where(s => !linkedIds.Contains(s.Id)));
            }

            return missing;
        }

        public GameComponent Get(int id)
        {
            return _componentRepository.Get(id);
        }

        public GameComponent SetComponentOptions(int id, bool monitored, int qualityProfileId)
        {
            var component = _componentRepository.Get(id);
            component.Monitored = monitored;
            component.QualityProfileId = qualityProfileId;

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
            var noIntroEntries = (_noIntroCatalogEntryRepository.All() ?? Enumerable.Empty<NoIntroCatalogEntry>()).ToList();

            MergeDuplicateDlcSlots(existing, files);

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

            foreach (var slot in _noIntroComponentPlanner.GetSlots(game, noIntroEntries))
            {
                FindOrStage(existing, toInsert, game, slot.ComponentType, slot.Key, slot.Title, monitored: true);
            }

            foreach (var file in files)
            {
                var component = GetComponentForFile(existing, toInsert, game, file, baseComponent, noIntroEntries);

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
                    var component = GetComponentForFile(all, new List<GameComponent>(), game, file, all.FirstOrDefault(c => c.ComponentType == GameComponentType.Base), noIntroEntries);

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

        // An imported DLC folder ("Hades.The.Blood.Price.DLC-GRP") and a
        // metadata slot ("igdb:111 The Blood Price") describe the same DLC.
        // Point files of matching import-keyed slots at the metadata slot and
        // drop the duplicate, so one DLC never shows as both downloaded and
        // missing.
        private void MergeDuplicateDlcSlots(List<GameComponent> existing, List<GameFile> files)
        {
            var metadataSlots = existing.Where(c => c.ComponentType == GameComponentType.Dlc && c.Key.StartsWith("igdb:")).ToList();
            var importSlots = existing.Where(c => c.ComponentType == GameComponentType.Dlc && c.Key.StartsWith("import:")).ToList();

            foreach (var importSlot in importSlots)
            {
                var match = metadataSlots.FirstOrDefault(m => GameComponentMatcher.ReleaseMatchesDlcTitle(importSlot.Title, m.Title));

                if (match == null)
                {
                    continue;
                }

                var relinked = files.Where(f => f.ComponentId == importSlot.Id).ToList();
                relinked.ForEach(f => f.ComponentId = match.Id);

                if (relinked.Any())
                {
                    _mediaFileService.Update(relinked);
                }

                if (importSlot.Monitored && !match.Monitored)
                {
                    match.Monitored = true;
                    _componentRepository.Update(match);
                }

                _componentRepository.Delete(importSlot);
                existing.Remove(importSlot);
                _logger.Debug("Merged imported DLC slot '{0}' into metadata slot '{1}'", importSlot.Title, match.Title);
            }
        }

        private GameComponent GetComponentForFile(List<GameComponent> existing, List<GameComponent> toInsert, Game game, GameFile file, GameComponent baseComponent, List<NoIntroCatalogEntry> noIntroEntries)
        {
            var noIntroSlot = _noIntroComponentPlanner.FindSlotForFile(game, noIntroEntries, file) ??
                FindSlotForFolderBackedFile(game, noIntroEntries, file);

            if (noIntroSlot != null)
            {
                return FindOrStage(existing, toInsert, game, noIntroSlot.ComponentType, noIntroSlot.Key, noIntroSlot.Title, monitored: true);
            }

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

                // Prefer an existing metadata slot for the same DLC over
                // creating an import-keyed duplicate. Having the file on disk
                // implies the user wants this DLC, so the slot turns monitored.
                var metadataSlot = existing.Concat(toInsert)
                    .Where(c => c.ComponentType == GameComponentType.Dlc && c.Key.StartsWith("igdb:"))
                    .FirstOrDefault(c => GameComponentMatcher.ReleaseMatchesDlcTitle(name, c.Title));

                if (metadataSlot != null)
                {
                    if (!metadataSlot.Monitored)
                    {
                        metadataSlot.Monitored = true;

                        if (metadataSlot.Id > 0)
                        {
                            _componentRepository.Update(metadataSlot);
                        }
                    }

                    return metadataSlot;
                }

                return FindOrStage(existing, toInsert, game, GameComponentType.Dlc, $"import:{name}", name, monitored: true);
            }

            // Legacy file-based records belong to the base slot.
            return baseComponent;
        }

        private NoIntroGameComponentSlot FindSlotForFolderBackedFile(Game game, List<NoIntroCatalogEntry> noIntroEntries, GameFile file)
        {
            if (!file.RelativePath.IsNullOrWhiteSpace() || !_diskProvider.FolderExists(game.Path))
            {
                return null;
            }

            var matchingSlots = _diskProvider.GetFiles(game.Path, true)
                .Select(Path.GetFileName)
                .Where(name => name.IsNotNullOrWhiteSpace())
                .Select(name => _noIntroComponentPlanner.FindSlotForFileName(game, noIntroEntries, name))
                .Where(slot => slot != null)
                .GroupBy(slot => slot.Key)
                .Select(group => group.First())
                .ToList();

            return matchingSlots.Count == 1 ? matchingSlots[0] : null;
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

        // Imports (downloaded-folder scans, manual imports) add files without
        // raising a scan/update event, so reconcile here too.
        public void Handle(GameFileAddedEvent message)
        {
            var game = message.GameFile.Game ?? _gameService.GetGame(message.GameFile.GameId);

            if (game == null)
            {
                return;
            }

            EnsureComponents(game);
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
