using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Games;
using Gamarr.Http;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Games
{
    [V3ApiController("game/import")]
    public class GameImportController : RestController<GameResource>
    {
        private readonly IAddGameService _addGameService;

        public GameImportController(IAddGameService addGameService)
        {
            _addGameService = addGameService;
        }

        [NonAction]
        public override ActionResult<GameResource> GetResourceByIdWithErrorHandler(int id)
        {
            return base.GetResourceByIdWithErrorHandler(id);
        }

        protected override GameResource GetResourceById(int id)
        {
            throw new NotFoundException();
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IEnumerable<GameResource> Import([FromBody] List<GameResource> resource)
        {
            var newGames = resource.ToModel();

            return _addGameService.AddGames(newGames).ToResource(0);
        }
    }
}
