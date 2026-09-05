using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common.Cache;
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

        private readonly ICached<List<PlatformRootFolder>> _cache;

        public PlatformRootFolderService(IPlatformRootFolderRepository repository,
                                         ICacheManager cacheManager)
        {
            _repository = repository;

            _cache = cacheManager.GetCache<List<PlatformRootFolder>>(GetType());
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
        /// or null when nothing is configured and the caller should keep
        /// whatever it already had.
        ///
        /// Matching is exact rather than family-aware: PlatformMatches() treats
        /// the generic Nintendo family as compatible with every Nintendo
        /// console, which is right for release filtering but would make
        /// "which folder?" ambiguous between, say, Switch and Wii. A platform
        /// with no entry of its own falls back to the Unknown entry, which is
        /// the global default. Nothing is invented beyond that: with no
        /// defaults configured this returns null and the caller still has to
        /// supply a root folder itself, exactly as before.
        /// </summary>
        public string GetDefaultRootFolderPath(PlatformFamily platform)
        {
            var all = All();

            var match = all.FirstOrDefault(p => p.Platform == platform);

            if (match == null && platform != PlatformFamily.Unknown)
            {
                match = all.FirstOrDefault(p => p.Platform == PlatformFamily.Unknown);
            }

            return match?.Path.IsNotNullOrWhiteSpace() == true ? match.Path : null;
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
