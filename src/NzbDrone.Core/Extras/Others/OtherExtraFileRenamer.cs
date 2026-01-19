using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Extras.Others
{
    public interface IOtherExtraFileRenamer
    {
        void RenameOtherExtraFile(Game game, string path);
    }

    public class OtherExtraFileRenamer : IOtherExtraFileRenamer
    {
        private readonly Logger _logger;
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IOtherExtraFileService _otherExtraFileService;

        public OtherExtraFileRenamer(IOtherExtraFileService otherExtraFileService,
                                     IRecycleBinProvider recycleBinProvider,
                                     IDiskProvider diskProvider,
                                     Logger logger)
        {
            _logger = logger;
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _otherExtraFileService = otherExtraFileService;
        }

        public void RenameOtherExtraFile(Game game, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = game.Path.GetRelativePath(path);
            var otherExtraFile = _otherExtraFileService.FindByPath(game.Id, relativePath);

            if (otherExtraFile != null)
            {
                var newPath = path + "-orig";

                // Recycle an existing -orig file.
                RemoveOtherExtraFile(game, newPath);

                // Rename the file to .*-orig
                _diskProvider.MoveFile(path, newPath);
                otherExtraFile.RelativePath = relativePath + "-orig";
                otherExtraFile.Extension += "-orig";
                _otherExtraFileService.Upsert(otherExtraFile);
            }
        }

        private void RemoveOtherExtraFile(Game game, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = game.Path.GetRelativePath(path);
            var otherExtraFile = _otherExtraFileService.FindByPath(game.Id, relativePath);

            if (otherExtraFile != null)
            {
                _recycleBinProvider.DeleteFile(path);
            }
        }
    }
}
