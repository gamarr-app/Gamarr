using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpdateGameFileService
    {
        void ChangeFileDateForFile(GameFile gameFile, Game game);
    }

    public class UpdateGameFileService : IUpdateGameFileService,
                                            IHandle<GameScannedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public UpdateGameFileService(IDiskProvider diskProvider,
                                      IConfigService configService,
                                      IMediaFileService mediaFileService,
                                      Logger logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public void ChangeFileDateForFile(GameFile gameFile, Game game)
        {
            ChangeFileDate(gameFile, game);
        }

        private bool ChangeFileDate(GameFile gameFile, Game game)
        {
            var gameFilePath = Path.Combine(game.Path, gameFile.RelativePath);

            switch (_configService.FileDate)
            {
                case FileDateType.Release:
                    {
                        var releaseDate = game.GameMetadata.Value.PhysicalRelease ?? game.GameMetadata.Value.DigitalRelease;

                        if (releaseDate.HasValue == false)
                        {
                            return false;
                        }

                        return ChangeFileDate(gameFilePath, releaseDate.Value);
                    }

                case FileDateType.Cinemas:
                    {
                        var airDate = game.GameMetadata.Value.InDevelopment;

                        if (airDate.HasValue == false)
                        {
                            return false;
                        }

                        return ChangeFileDate(gameFilePath, airDate.Value);
                    }
            }

            return false;
        }

        private bool ChangeFileDate(string filePath, DateTime date)
        {
            if (DateTime.TryParse(_diskProvider.FileGetLastWrite(filePath).ToLongDateString(), out var oldDateTime))
            {
                if (!DateTime.Equals(date, oldDateTime))
                {
                    try
                    {
                        _diskProvider.FileSetLastWriteTime(filePath, date);
                        _logger.Debug("Date of file [{0}] changed from '{1}' to '{2}'", filePath, oldDateTime, date);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Unable to set date of file [" + filePath + "]");
                    }
                }
            }

            return false;
        }

        public void Handle(GameScannedEvent message)
        {
            if (_configService.FileDate == FileDateType.None)
            {
                return;
            }

            var gameFiles = _mediaFileService.GetFilesByGame(message.Game.Id);
            var updated = new List<GameFile>();

            foreach (var gameFile in gameFiles)
            {
                if (ChangeFileDate(gameFile, message.Game))
                {
                    updated.Add(gameFile);
                }
            }

            if (updated.Any())
            {
                _logger.ProgressDebug("Changed file date for {0} files of {1} in {2}", updated.Count, gameFiles.Count, message.Game.Title);
            }
            else
            {
                _logger.ProgressDebug("No file dates changed for {0}", message.Game.Title);
            }
        }
    }
}
