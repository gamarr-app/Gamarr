using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Extras.Files
{
    public interface IManageExtraFiles
    {
        int Order { get; }
        IEnumerable<ExtraFile> CreateAfterMediaCoverUpdate(Game game);
        IEnumerable<ExtraFile> CreateAfterGameScan(Game game, List<GameFile> gameFiles);
        IEnumerable<ExtraFile> CreateAfterGameImport(Game game, GameFile gameFile);
        IEnumerable<ExtraFile> CreateAfterGameFolder(Game game, string gameFolder);
        IEnumerable<ExtraFile> MoveFilesAfterRename(Game game, List<GameFile> gameFiles);
        bool CanImportFile(LocalGame localGame, GameFile gameFile, string path, string extension, bool readOnly);
        IEnumerable<ExtraFile> ImportFiles(LocalGame localGame, GameFile gameFile, List<string> files, bool isReadOnly);
    }

    public abstract class ExtraFileManager<TExtraFile> : IManageExtraFiles
        where TExtraFile : ExtraFile, new()
    {
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskTransferService _diskTransferService;
        private readonly Logger _logger;

        public ExtraFileManager(IConfigService configService,
                                IDiskProvider diskProvider,
                                IDiskTransferService diskTransferService,
                                Logger logger)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _diskTransferService = diskTransferService;
            _logger = logger;
        }

        public abstract int Order { get; }
        public abstract IEnumerable<ExtraFile> CreateAfterMediaCoverUpdate(Game game);
        public abstract IEnumerable<ExtraFile> CreateAfterGameScan(Game game, List<GameFile> gameFiles);
        public abstract IEnumerable<ExtraFile> CreateAfterGameImport(Game game, GameFile gameFile);
        public abstract IEnumerable<ExtraFile> CreateAfterGameFolder(Game game, string gameFolder);
        public abstract IEnumerable<ExtraFile> MoveFilesAfterRename(Game game, List<GameFile> gameFiles);
        public abstract bool CanImportFile(LocalGame localGame, GameFile gameFile, string path, string extension, bool readOnly);
        public abstract IEnumerable<ExtraFile> ImportFiles(LocalGame localGame, GameFile gameFile, List<string> files, bool isReadOnly);

        protected TExtraFile ImportFile(Game game, GameFile gameFile, string path, bool readOnly, string extension, string fileNameSuffix = null)
        {
            var gameFilePath = Path.Combine(game.Path, gameFile.RelativePath);
            var newFolder = Path.GetDirectoryName(gameFilePath);
            var filenameBuilder = new StringBuilder(Path.GetFileNameWithoutExtension(gameFile.RelativePath));

            if (fileNameSuffix.IsNotNullOrWhiteSpace())
            {
                filenameBuilder.Append(fileNameSuffix);
            }

            filenameBuilder.Append(extension);

            var newFileName = Path.Combine(newFolder, filenameBuilder.ToString());

            if (newFileName == gameFilePath)
            {
                _logger.Debug("Extra file {0} not imported, due to naming interference with game file", path);
                return null;
            }

            var transferMode = TransferMode.Move;

            if (readOnly)
            {
                transferMode = _configService.CopyUsingHardlinks ? TransferMode.HardLinkOrCopy : TransferMode.Copy;
            }

            _diskTransferService.TransferFile(path, newFileName, transferMode, true);

            return new TExtraFile
            {
                GameId = game.Id,
                GameFileId = gameFile.Id,
                RelativePath = game.Path.GetRelativePath(newFileName),
                Extension = extension
            };
        }

        protected TExtraFile MoveFile(Game game, GameFile gameFile, TExtraFile extraFile, string fileNameSuffix = null)
        {
            _logger.Trace("Renaming extra file: {0}", extraFile);

            var newFolder = Path.GetDirectoryName(Path.Combine(game.Path, gameFile.RelativePath));
            var filenameBuilder = new StringBuilder(Path.GetFileNameWithoutExtension(gameFile.RelativePath));

            if (fileNameSuffix.IsNotNullOrWhiteSpace())
            {
                filenameBuilder.Append(fileNameSuffix);
            }

            filenameBuilder.Append(extraFile.Extension);

            var existingFileName = Path.Combine(game.Path, extraFile.RelativePath);
            var newFileName = Path.Combine(newFolder, filenameBuilder.ToString());

            if (newFileName.PathNotEquals(existingFileName))
            {
                try
                {
                    _logger.Trace("Renaming extra file: {0} to {1}", extraFile, newFileName);

                    _diskProvider.MoveFile(existingFileName, newFileName);
                    extraFile.RelativePath = game.Path.GetRelativePath(newFileName);

                    _logger.Trace("Renamed extra file from: {0}", extraFile);

                    return extraFile;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to move file after rename: {0}", existingFileName);
                }
            }

            return null;
        }
    }
}
