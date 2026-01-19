using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Gamarr.Http;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Games
{
    [V3ApiController("rename")]
    public class RenameGameController : Controller
    {
        private readonly IRenameGameFileService _renameGameFileService;

        public RenameGameController(IRenameGameFileService renameGameFileService)
        {
            _renameGameFileService = renameGameFileService;
        }

        [HttpGet]
        public List<RenameGameResource> GetGames([FromQuery(Name = "gameId")] List<int> gameIds)
        {
            if (gameIds is not { Count: not 0 })
            {
                throw new BadRequestException("gameId must be provided");
            }

            return _renameGameFileService.GetRenamePreviews(gameIds).ToResource();
        }
    }
}
