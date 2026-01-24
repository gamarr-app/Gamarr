using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Configuration;

namespace Gamarr.Http.Ping
{
    public class PingController : Controller
    {
        private readonly IConfigRepository _configRepository;
        private readonly ICached<IEnumerable<Config>> _cache;
        private readonly Logger _logger;

        public PingController(IConfigRepository configRepository, ICacheManager cacheManager, Logger logger)
        {
            _configRepository = configRepository;
            _cache = cacheManager.GetCache<IEnumerable<Config>>(GetType());
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("/ping")]
        [HttpHead("/ping")]
        [Produces("application/json")]
        public ActionResult<PingResource> GetStatus()
        {
            try
            {
                _cache.Get("ping", _configRepository.All, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Health check failed: unable to query configuration database");

                return StatusCode(StatusCodes.Status500InternalServerError, new PingResource
                {
                    Status = "Error"
                });
            }

            return StatusCode(StatusCodes.Status200OK, new PingResource
            {
                Status = "OK"
            });
        }
    }
}
