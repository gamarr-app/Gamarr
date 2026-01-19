using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using Gamarr.Http;

namespace Gamarr.Api.V3.Games;

[V3ApiController("game")]
public class GameFolderController : Controller
{
    private readonly IGameService _gameService;
    private readonly IBuildFileNames _fileNameBuilder;

    public GameFolderController(IGameService gameService, IBuildFileNames fileNameBuilder)
    {
        _gameService = gameService;
        _fileNameBuilder = fileNameBuilder;
    }

    [HttpGet("{id}/folder")]
    [Produces("application/json")]
    public object GetFolder([FromRoute] int id)
    {
        var series = _gameService.GetGame(id);
        var folder = _fileNameBuilder.GetGameFolder(series);

        return new
        {
            folder
        };
    }
}
