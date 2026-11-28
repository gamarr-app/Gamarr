using System.IO;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MediaFiles
{
    /// <summary>
    /// The single place that decides whether a file is excluded from import by
    /// the user's "Ignored File Extensions" media management setting. Every
    /// import path (disk scan, downloaded import, manual import, folder copy,
    /// archive extraction) asks this rather than parsing the setting itself.
    ///
    /// The setting wins over built-in game extensions: listing .zip means a
    /// .zip is never treated as a game payload, is never inspected or extracted,
    /// and is left out of folder copies.
    /// </summary>
    public interface IIgnoredExtensionService
    {
        bool IsIgnored(string path);
    }

    public class IgnoredExtensionService : IIgnoredExtensionService
    {
        private readonly IConfigService _configService;

        public IgnoredExtensionService(IConfigService configService)
        {
            _configService = configService;
        }

        public bool IsIgnored(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var setting = _configService.UserIgnoredExtensions;

            if (string.IsNullOrWhiteSpace(setting))
            {
                return false;
            }

            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return FileExtensions.ParseIgnoredExtensions(setting).Contains(extension);
        }
    }
}
