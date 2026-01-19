using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using Gamarr.Http;
using Gamarr.Http.Extensions;
using Gamarr.Http.REST.Attributes;

namespace Gamarr.Api.V3.Blocklist
{
    [V3ApiController]
    public class BlocklistController : Controller
    {
        private readonly IBlocklistService _blocklistService;
        private readonly ICustomFormatCalculationService _formatCalculator;

        public BlocklistController(IBlocklistService blocklistService,
                                   ICustomFormatCalculationService formatCalculator)
        {
            _blocklistService = blocklistService;
            _formatCalculator = formatCalculator;
        }

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<BlocklistResource> GetBlocklist([FromQuery] PagingRequestResource paging, [FromQuery] int[] gameIds = null, [FromQuery] DownloadProtocol[] protocols = null)
        {
            var pagingResource = new PagingResource<BlocklistResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<BlocklistResource, NzbDrone.Core.Blocklisting.Blocklist>(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "date",
                    "indexer",
                    "languages",
                    "gameMetadata.sortTitle",
                    "quality",
                    "sourceTitle"
                },
                "date",
                SortDirection.Descending);

            if (gameIds?.Any() == true)
            {
                pagingSpec.FilterExpressions.Add(b => gameIds.Contains(b.GameId));
            }

            if (protocols?.Any() == true)
            {
                pagingSpec.FilterExpressions.Add(b => protocols.Contains(b.Protocol));
            }

            return pagingSpec.ApplyToPage(b => _blocklistService.Paged(pagingSpec), b => BlocklistResourceMapper.MapToResource(b, _formatCalculator));
        }

        [HttpGet("game")]
        public List<BlocklistResource> GetGameBlocklist(int gameId)
        {
            return _blocklistService.GetByGameId(gameId).Select(h => BlocklistResourceMapper.MapToResource(h, _formatCalculator)).ToList();
        }

        [RestDeleteById]
        public void DeleteBlocklist(int id)
        {
            _blocklistService.Delete(id);
        }

        [HttpDelete("bulk")]
        [Produces("application/json")]
        public object Remove([FromBody] BlocklistBulkResource resource)
        {
            _blocklistService.Delete(resource.Ids);

            return new { };
        }
    }
}
