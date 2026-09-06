using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.RootFolders
{
    public interface IPlatformRootFolderService
    {
        List<PlatformRootFolder> All();
        PlatformRootFolder Get(int id);
        PlatformRootFolder Add(PlatformRootFolder platformRootFolder);
        PlatformRootFolder Update(PlatformRootFolder platformRootFolder);
        void Remove(int id);

        string GetDefaultRootFolderPath(PlatformFamily platform);
    }

    public class PlatformRootFolderService : IPlatformRootFolderService
    {
        private readonly IPlatformRootFolderRepository _repository;
        private readonly IRootFolderService _rootFolderService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        private readonly ICached<List<PlatformRootFolder>> _cache;

        public PlatformRootFolderService(IPlatformRootFolderRepository repository,
                                         IRootFolderService rootFolderService,
                                         IDiskProvider diskProvider,
                                         ICacheManager cacheManager,
                                         Logger logger)
        {
            _repository = repository;
            _rootFolderService = rootFolderService;
            _diskProvider = diskProvider;
            _logger = logger;

            _cache = cacheManager.GetCache<List<PlatformRootFolder>>(GetType());
        }

        /// <summary>
        /// The failure an add hits when it supplied no root folder and none
        /// could be resolved. Phrased around the missing *mapping* rather than
        /// the empty field: "'Root Folder Path' must not be empty" sends an API
        /// caller hunting for a bug in their own request body when the real fix
        /// is a configuration one. Shared by the game POST validator (where an
        /// interactive add fails) and AddGameService (import lists, API clients).
        /// </summary>
        public static string NoDefaultRootFolderError(PlatformFamily platform)
        {
            return $"No root folder was supplied and no default is configured for platform {platform} — add a platform root folder mapping, an 'unknown' catch-all, or a root folder.";
        }

        public List<PlatformRootFolder> All()
        {
            return _cache.Get("all", () => _repository.All().ToList(), TimeSpan.FromSeconds(10));
        }

        public PlatformRootFolder Get(int id)
        {
            return _repository.Get(id);
        }

        public PlatformRootFolder Add(PlatformRootFolder platformRootFolder)
        {
            Validate(platformRootFolder, All());

            var result = _repository.Insert(platformRootFolder);

            _cache.Clear();

            return result;
        }

        public PlatformRootFolder Update(PlatformRootFolder platformRootFolder)
        {
            Validate(platformRootFolder, All().Where(p => p.Id != platformRootFolder.Id).ToList());

            var result = _repository.Update(platformRootFolder);

            _cache.Clear();

            return result;
        }

        public void Remove(int id)
        {
            _repository.Delete(id);

            _cache.Clear();
        }

        /// <summary>
        /// The root folder a newly added game of this platform should land in,
        /// or null only when the instance has no root folders at all.
        ///
        /// Resolution order: the entry for this exact PlatformFamily, then the
        /// Unknown entry (the global catch-all), then an existing configured
        /// root folder as a last resort.
        ///
        /// Matching is exact rather than family-aware: PlatformMatches() treats
        /// the generic Nintendo family as compatible with every Nintendo
        /// console, which is right for release filtering but would make
        /// "which folder?" ambiguous between, say, Switch and Wii. A platform
        /// with no entry of its own falls back to the Unknown entry, which is
        /// the global default.
        /// </summary>
        public string GetDefaultRootFolderPath(PlatformFamily platform)
        {
            var all = All();

            var match = all.FirstOrDefault(p => p.Platform == platform);

            if (match == null && platform != PlatformFamily.Unknown)
            {
                match = all.FirstOrDefault(p => p.Platform == PlatformFamily.Unknown);
            }

            if (match?.Path.IsNotNullOrWhiteSpace() == true)
            {
                return match.Path;
            }

            return GetLastResortRootFolderPath(platform);
        }

        /// <summary>
        /// With no mapping to go on, an existing root folder beats failing the
        /// add on "'Root Folder Path' must not be empty" — a user who has
        /// configured exactly one root folder plainly means that one, and with
        /// several, landing in one of them is recoverable (move the game)
        /// whereas the add failing is not (the user just loses the add).
        ///
        /// This was originally left out because it "would silently redirect
        /// adds that currently fail validation". The Info-level log below is
        /// what stops it being silent: the chosen path and the platform that
        /// had no mapping are both named, so the fix (add a mapping) is
        /// discoverable from the log rather than from surprise.
        /// </summary>
        private string GetLastResortRootFolderPath(PlatformFamily platform)
        {
            // Ordered by Id, i.e. oldest-configured first. The repository
            // returns whatever order the query happens to produce, so without
            // this the same instance could pick a different folder from one
            // add to the next; Id is stable, monotonic and never reused.
            var rootFolders = _rootFolderService.All()
                                                .Where(r => r.Path.IsNotNullOrWhiteSpace())
                                                .OrderBy(r => r.Id)
                                                .ToList();

            if (rootFolders.Empty())
            {
                return null;
            }

            // A single root folder is unambiguous, so take it without touching
            // the disk. Otherwise prefer one that is actually there, but still
            // fall back to the oldest when none of them are — an add into a
            // temporarily unmounted folder beats no add at all, and the
            // downstream path validators report that case properly.
            var chosen = rootFolders.Count == 1
                ? rootFolders[0]
                : rootFolders.FirstOrDefault(IsAccessible) ?? rootFolders[0];

            _logger.Info("No root folder configured for platform {0}; defaulting to {1}", platform, chosen.Path);

            return chosen.Path;
        }

        // RootFolder.Accessible is not persisted (TableMapping ignores it) and
        // RootFolderService.All() doesn't populate it, so it is always false
        // here — ask the disk instead of trusting the flag.
        private bool IsAccessible(RootFolder rootFolder)
        {
            try
            {
                return _diskProvider.FolderExists(rootFolder.Path);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to check root folder {0}, treating it as inaccessible", rootFolder.Path);

                return false;
            }
        }

        private static void Validate(PlatformRootFolder platformRootFolder, List<PlatformRootFolder> existing)
        {
            if (platformRootFolder.Path.IsNullOrWhiteSpace() || !Path.IsPathRooted(platformRootFolder.Path))
            {
                throw new ArgumentException("Invalid path");
            }

            if (existing.Any(p => p.Platform == platformRootFolder.Platform))
            {
                throw new InvalidOperationException($"A default root folder for {platformRootFolder.Platform} already exists.");
            }
        }
    }
}
