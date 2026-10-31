using System;
using System.IO;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Xbmc
{
    public interface IDetectXbmcNfo
    {
        bool IsXbmcNfoFile(string path);
    }

    public class XbmcNfoDetector : IDetectXbmcNfo
    {
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        private readonly Regex _regex = new Regex("<(game|tvshow|episodedetails|artist|album|musicvideo)>", RegexOptions.Compiled);

        public XbmcNfoDetector(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public bool IsXbmcNfoFile(string path)
        {
            // The candidate list is built before the extras pipeline runs, so a
            // file another importer already consumed/renamed may be gone by now;
            // that must not abort the whole scan event.
            if (!_diskProvider.FileExists(path))
            {
                return false;
            }

            // The file can still vanish or become unreadable between the
            // existence check above and either of these disk calls (remote
            // mount hiccup, concurrent delete/rename by another importer);
            // that must not abort the whole scan event, same as the metadata
            // write side in MetadataService.
            try
            {
                // Lets make sure we're not reading huge files.
                if (_diskProvider.GetFileSize(path) > 10.Megabytes())
                {
                    return false;
                }

                // Check if it contains some of the kodi/xbmc xml tags
                var content = _diskProvider.ReadAllText(path);

                return _regex.IsMatch(content);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _logger.Warn(ex, "Unable to read possible nfo file: {0}", path);

                return false;
            }
        }
    }
}
