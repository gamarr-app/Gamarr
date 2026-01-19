using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IFilenameSampleService
    {
        SampleResult GetGameSample(NamingConfig nameSpec);
        string GetGameFolderSample(NamingConfig nameSpec);
    }

    public class FileNameSampleService : IFilenameSampleService
    {
        private readonly IBuildFileNames _buildFileNames;

        private static GameFile _gameFile;
        private static Game _game;
        private static List<CustomFormat> _customFormats;

        public FileNameSampleService(IBuildFileNames buildFileNames)
        {
            _buildFileNames = buildFileNames;

            var mediaInfo = new MediaInfoModel
            {
                VideoFormat = "AVC",
                VideoBitDepth = 10,
                VideoMultiViewCount = 2,
                VideoColourPrimaries = "bt2020",
                VideoTransferCharacteristics = "HLG",
                AudioFormat = "DTS",
                AudioChannels = 6,
                AudioChannelPositions = "5.1",
                AudioLanguages = new List<string> { "ger" },
                Subtitles = new List<string> { "eng", "ger" }
            };

            _gameFile = new GameFile
            {
                Quality = new QualityModel(Quality.Bluray1080p, new Revision(2)),
                RelativePath = "The.Game.Title.2010.1080p.BluRay.DTS.x264-EVOLVE.mkv",
                SceneName = "The.Game.Title.2010.1080p.BluRay.DTS.x264-EVOLVE",
                ReleaseGroup = "EVOLVE",
                MediaInfo = mediaInfo,
                Edition = "Ultimate extended edition",
            };

            _game = new Game
            {
                GameMetadata = new GameMetadata
                {
                    Title = "The Game: Title",
                    OriginalTitle = "The Original Game Title",
                    CollectionTitle = "The Game Collection",
                    CollectionIgdbId = 123654,
                    Certification = "R",
                    Year = 2010,
                    ImdbId = "tt0066921",
                    IgdbId = 345691
                },
                GameMetadataId = 1
            };

            _customFormats = new List<CustomFormat>
            {
                new CustomFormat
                {
                    Name = "Surround Sound",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat
                {
                    Name = "x264",
                    IncludeCustomFormatWhenRenaming = true
                }
            };
        }

        public SampleResult GetGameSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildSample(_game, _gameFile, nameSpec),
                Game = _game,
                GameFile = _gameFile
            };

            return result;
        }

        public string GetGameFolderSample(NamingConfig nameSpec)
        {
            return _buildFileNames.GetGameFolder(_game, nameSpec);
        }

        private string BuildSample(Game game, GameFile gameFile, NamingConfig nameSpec)
        {
            try
            {
                return _buildFileNames.BuildFileName(game, gameFile, nameSpec, _customFormats);
            }
            catch (NamingFormatException)
            {
                return string.Empty;
            }
        }
    }
}
