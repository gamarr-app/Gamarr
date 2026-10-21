using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.RomCatalog;
using Gamarr.Http;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.GameComponents
{
    [V3ApiController("gamecomponent")]
    public class GameComponentController : Controller
    {
        private readonly IGameComponentService _componentService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly INoIntroCatalogEntryRepository _noIntroCatalogEntryRepository;
        private readonly INoIntroCatalogSourceRepository _noIntroCatalogSourceRepository;
        private readonly INoIntroCatalogHashRepository _noIntroCatalogHashRepository;
        private readonly IGameFileHashService _gameFileHashService;
        private readonly Logger _logger;

        public GameComponentController(IGameComponentService componentService,
                                        IMediaFileService mediaFileService,
                                        IQualityProfileService qualityProfileService,
                                        INoIntroCatalogEntryRepository noIntroCatalogEntryRepository,
                                        INoIntroCatalogSourceRepository noIntroCatalogSourceRepository,
                                        INoIntroCatalogHashRepository noIntroCatalogHashRepository,
                                        IGameFileHashService gameFileHashService,
                                        Logger logger)
        {
            _componentService = componentService;
            _mediaFileService = mediaFileService;
            _qualityProfileService = qualityProfileService;
            _noIntroCatalogEntryRepository = noIntroCatalogEntryRepository;
            _noIntroCatalogSourceRepository = noIntroCatalogSourceRepository;
            _noIntroCatalogHashRepository = noIntroCatalogHashRepository;
            _gameFileHashService = gameFileHashService;
            _logger = logger;
        }

        [HttpGet]
        [Produces("application/json")]
        public List<GameComponentResource> GetComponents([FromQuery] int gameId)
        {
            if (gameId <= 0)
            {
                throw new BadRequestException("gameId must be provided");
            }

            var files = _mediaFileService.GetFilesByGame(gameId);
            var noIntroEntries = _noIntroCatalogEntryRepository.All().ToList();
            var noIntroSources = _noIntroCatalogSourceRepository.All().ToList();
            var noIntroHashes = _noIntroCatalogHashRepository.GetByEntryIds(noIntroEntries.Select(x => x.Id).ToList());
            var context = new GameComponentNoIntroCatalogContext
            {
                GameFiles = files,
                Entries = noIntroEntries,
                Sources = noIntroSources,
                HashMatches = GetFileHashMatches(files, gameId, noIntroHashes)
            };

            return _componentService.GetByGame(gameId)
                .OrderBy(c => c.ComponentType)
                .ThenBy(c => c.Title)
                .Select(c => c.ToResource(context))
                .ToList();
        }

        [HttpPut("{id:int}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public GameComponentResource SetComponent(int id, [FromBody] GameComponentResource resource)
        {
            if (resource.QualityProfileId != 0 && !_qualityProfileService.Exists(resource.QualityProfileId))
            {
                throw new BadRequestException("Quality profile does not exist");
            }

            var component = _componentService.SetComponentOptions(id, resource.Monitored, resource.QualityProfileId);
            var files = _mediaFileService.GetFilesByGame(component.GameId);

            return component.ToResource(new GameComponentNoIntroCatalogContext { GameFiles = files });
        }

        /// <remarks>
        /// Reads stored hashes only. This used to open every component file and
        /// compute MD5 + SHA1 + CRC32 over it, which cost 40-400 s per request
        /// and a pinned CPU core for a multi-GB base file (#205). Files with no
        /// current triplet are simply left out of the response — the catalog
        /// resource falls back to title matching and reports no exact hash — and
        /// a background hash pass is queued so a later load has them.
        /// </remarks>
        private List<NoIntroCatalogFileHashMatch> GetFileHashMatches(List<GameFile> files, int gameId, List<NoIntroCatalogHash> catalogHashes)
        {
            var componentFiles = files.Where(x => x.ComponentId > 0).ToList();

            if (componentFiles.Count == 0)
            {
                return new List<NoIntroCatalogFileHashMatch>();
            }

            var storedHashes = _gameFileHashService.GetCurrentHashes(componentFiles);

            if (storedHashes.Count < componentFiles.Count)
            {
                _logger.Debug("{0} of {1} component files of game {2} have no current hashes, queueing a hash pass",
                    componentFiles.Count - storedHashes.Count,
                    componentFiles.Count,
                    gameId);

                _gameFileHashService.QueueHashing(gameId);
            }

            var hashByKey = catalogHashes
                .GroupBy(x => $"{x.HashType}:{x.HashValue}".ToLowerInvariant())
                .ToDictionary(x => x.Key, x => x.First());

            return storedHashes
                .Select(stored => new { stored.GameFileId, Match = FindMatch(stored, hashByKey) })
                .Where(x => x.Match != null)
                .Select(x => new NoIntroCatalogFileHashMatch
                {
                    GameFileId = x.GameFileId,
                    CatalogEntryId = x.Match.CatalogEntryId,
                    HashType = x.Match.HashType,
                    HashValue = x.Match.HashValue
                })
                .ToList();
        }

        private static NoIntroCatalogHash FindMatch(GameFileHash hashes, Dictionary<string, NoIntroCatalogHash> catalogHashes)
        {
            return TryGetHash("sha1", hashes.Sha1, catalogHashes) ??
                   TryGetHash("md5", hashes.Md5, catalogHashes) ??
                   TryGetHash("crc32", hashes.Crc32, catalogHashes);
        }

        private static NoIntroCatalogHash TryGetHash(string hashType, string hashValue, Dictionary<string, NoIntroCatalogHash> catalogHashes)
        {
            if (string.IsNullOrWhiteSpace(hashValue))
            {
                return null;
            }

            catalogHashes.TryGetValue($"{hashType}:{hashValue}".ToLowerInvariant(), out var matchedHash);
            return matchedHash;
        }
    }
}
