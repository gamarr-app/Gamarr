using NzbDrone.Core.Organizer;

namespace Gamarr.Api.V3.Config
{
    public class NamingExampleResource
    {
        public string GameExample { get; set; }
        public string GameFolderExample { get; set; }
    }

    public static class NamingConfigResourceMapper
    {
        public static NamingConfigResource ToResource(this NamingConfig model)
        {
            return new NamingConfigResource
            {
                Id = model.Id,

                RenameGames = model.RenameGames,
                ReplaceIllegalCharacters = model.ReplaceIllegalCharacters,
                ColonReplacementFormat = model.ColonReplacementFormat,
                StandardGameFormat = model.StandardGameFormat,
                GameFolderFormat = model.GameFolderFormat
            };
        }

        public static NamingConfig ToModel(this NamingConfigResource resource)
        {
            return new NamingConfig
            {
                Id = resource.Id,

                RenameGames = resource.RenameGames,
                ReplaceIllegalCharacters = resource.ReplaceIllegalCharacters,
                ColonReplacementFormat = resource.ColonReplacementFormat,
                StandardGameFormat = resource.StandardGameFormat,
                GameFolderFormat = resource.GameFolderFormat,
            };
        }
    }
}
