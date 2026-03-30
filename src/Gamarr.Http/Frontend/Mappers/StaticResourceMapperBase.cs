using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;

namespace Gamarr.Http.Frontend.Mappers
{
    public abstract class StaticResourceMapperBase : IMapHttpRequestsToDisk
    {
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;
        private readonly StringComparison _caseSensitive;
        private readonly IContentTypeProvider _mimeTypeProvider;

        protected StaticResourceMapperBase(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;

            _mimeTypeProvider = new FileExtensionContentTypeProvider();
            _caseSensitive = RuntimeInfo.IsProduction ? DiskProviderBase.PathStringComparison : StringComparison.OrdinalIgnoreCase;
        }

        public abstract string FolderPath { get; }

        public abstract string MapPath(string resourceUrl);

        public string Map(string resourceUrl)
        {
            var mappedPath = MapPath(resourceUrl);

            if (mappedPath == null)
            {
                return null;
            }

            var resolvedPath = Path.GetFullPath(mappedPath);
            var resolvedFolder = Path.GetFullPath(FolderPath) + Path.DirectorySeparatorChar;

            if (!resolvedPath.StartsWith(resolvedFolder, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warn("Path traversal attempt blocked: {0} is outside expected folder {1}", resolvedPath, resolvedFolder);
                return null;
            }

            return resolvedPath;
        }

        public abstract bool CanHandle(string resourceUrl);

        public Task<IActionResult> GetResponse(string resourceUrl)
        {
            var filePath = Map(resourceUrl);

            if (filePath == null)
            {
                return Task.FromResult<IActionResult>(null);
            }

            if (_diskProvider.FileExists(filePath, _caseSensitive))
            {
                if (!_mimeTypeProvider.TryGetContentType(filePath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return Task.FromResult<IActionResult>(new FileStreamResult(GetContentStream(filePath), new MediaTypeHeaderValue(contentType)
                {
                    Encoding = contentType == "text/plain" ? Encoding.UTF8 : null
                }));
            }

            _logger.Warn("File {0} not found", filePath);

            return Task.FromResult<IActionResult>(null);
        }

        protected virtual Stream GetContentStream(string filePath)
        {
            return File.OpenRead(filePath);
        }
    }
}
