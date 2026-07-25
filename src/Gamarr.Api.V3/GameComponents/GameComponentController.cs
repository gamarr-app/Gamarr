using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Games;
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
        private readonly IGameService _gameService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly INoIntroCatalogEntryRepository _noIntroCatalogEntryRepository;
        private readonly INoIntroCatalogSourceRepository _noIntroCatalogSourceRepository;
        private readonly INoIntroCatalogHashRepository _noIntroCatalogHashRepository;
        private readonly IDiskProvider _diskProvider;

        public GameComponentController(IGameComponentService componentService,
                                        IMediaFileService mediaFileService,
                                        IGameService gameService,
                                        IQualityProfileService qualityProfileService,
                                        INoIntroCatalogEntryRepository noIntroCatalogEntryRepository,
                                        INoIntroCatalogSourceRepository noIntroCatalogSourceRepository,
                                        INoIntroCatalogHashRepository noIntroCatalogHashRepository,
                                        IDiskProvider diskProvider)
        {
            _componentService = componentService;
            _mediaFileService = mediaFileService;
            _gameService = gameService;
            _qualityProfileService = qualityProfileService;
            _noIntroCatalogEntryRepository = noIntroCatalogEntryRepository;
            _noIntroCatalogSourceRepository = noIntroCatalogSourceRepository;
            _noIntroCatalogHashRepository = noIntroCatalogHashRepository;
            _diskProvider = diskProvider;
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
            var game = _gameService.GetGame(gameId);
            var context = new GameComponentNoIntroCatalogContext
            {
                GameFiles = files,
                Entries = noIntroEntries,
                Sources = noIntroSources,
                HashMatches = GetFileHashMatches(files, game, noIntroHashes)
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

        private List<NoIntroCatalogFileHashMatch> GetFileHashMatches(List<GameFile> files, Game game, List<NoIntroCatalogHash> catalogHashes)
        {
            var hashByKey = catalogHashes
                .GroupBy(x => $"{x.HashType}:{x.HashValue}".ToLowerInvariant())
                .ToDictionary(x => x.Key, x => x.First());
            var matches = new List<NoIntroCatalogFileHashMatch>();

            foreach (var file in files.Where(x => x.ComponentId > 0))
            {
                var path = file.GetPath(game);

                if (!_diskProvider.FileExists(path))
                {
                    continue;
                }

                using var stream = _diskProvider.OpenReadStream(path);
                var hashes = NoIntroRomHasher.Compute(stream);
                var matchedHash = FindMatch(hashes, hashByKey);

                if (matchedHash == null)
                {
                    continue;
                }

                matches.Add(new NoIntroCatalogFileHashMatch
                {
                    GameFileId = file.Id,
                    CatalogEntryId = matchedHash.CatalogEntryId,
                    HashType = matchedHash.HashType,
                    HashValue = matchedHash.HashValue
                });
            }

            return matches;
        }

        private static NoIntroCatalogHash FindMatch(NoIntroHashTriplet hashes, Dictionary<string, NoIntroCatalogHash> catalogHashes)
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
