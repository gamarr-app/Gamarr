using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
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

            _gameFile = new GameFile
            {
                Quality = new QualityModel(Quality.GOG, new Revision(1)),
                RelativePath = "The.Game.Title.2010.GOG",
                SceneName = "The.Game.Title.2010.GOG",
                ReleaseGroup = "GOG",
                Edition = "Complete Edition",
            };

            _game = new Game
            {
                GameMetadata = new GameMetadata
                {
                    Title = "The Game: Title",
                    OriginalTitle = "The Original Game Title",
                    CollectionTitle = "The Game Collection",
                    CollectionIgdbId = 123654,
                    Certification = "M",  // ESRB Mature rating
                    Year = 2010,
                    IgdbId = 345691
                },
                GameMetadataId = 1
            };

            _customFormats = new List<CustomFormat>
            {
                new CustomFormat
                {
                    Name = "DRM-Free",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat
                {
                    Name = "All DLC",
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
