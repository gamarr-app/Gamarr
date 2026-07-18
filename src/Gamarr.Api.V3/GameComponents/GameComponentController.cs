using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
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

        public GameComponentController(IGameComponentService componentService,
                                       IMediaFileService mediaFileService,
                                       IQualityProfileService qualityProfileService)
        {
            _componentService = componentService;
            _mediaFileService = mediaFileService;
            _qualityProfileService = qualityProfileService;
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

            return _componentService.GetByGame(gameId)
                .OrderBy(c => c.ComponentType)
                .ThenBy(c => c.Title)
                .Select(c => c.ToResource(files))
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

            return component.ToResource(files);
        }
    }
}
