using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.MediaCover;

namespace Gamarr.Http.Frontend.Mappers
{
    public class MediaCoverProxyMapper : IMapHttpRequestsToDisk
    {
        private readonly Regex _regex = new (@"/MediaCoverProxy/(?<hash>\w+)/(?<filename>(.+)\.(jpg|png|gif))");

        private readonly IMediaCoverProxy _mediaCoverProxy;
        private readonly IContentTypeProvider _mimeTypeProvider;
        private readonly Logger _logger;

        public MediaCoverProxyMapper(IMediaCoverProxy mediaCoverProxy, Logger logger)
        {
            _mediaCoverProxy = mediaCoverProxy;
            _mimeTypeProvider = new FileExtensionContentTypeProvider();
            _logger = logger;
        }

        public string Map(string resourceUrl)
        {
            return null;
        }

        public bool CanHandle(string resourceUrl)
        {
            return resourceUrl.StartsWith("/MediaCoverProxy/", StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<IActionResult> GetResponse(string resourceUrl)
        {
            var match = _regex.Match(resourceUrl);

            if (!match.Success)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            var hash = match.Groups["hash"].Value;
            var filename = match.Groups["filename"].Value;

            try
            {
                var imageData = await _mediaCoverProxy.GetImage(hash);

                if (!_mimeTypeProvider.TryGetContentType(filename, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return new FileContentResult(imageData, contentType);
            }
            catch (KeyNotFoundException)
            {
                // URL cache expired, return 404
                _logger.Debug("Media cover proxy cache miss for hash: {0}", hash);
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }
            catch (HttpException ex)
            {
                _logger.Debug(ex, "Failed to fetch proxied media cover for hash: {0}", hash);
                return new StatusCodeResult((int)(ex.Response?.StatusCode ?? HttpStatusCode.BadGateway));
            }
        }
    }
}
