using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Kometa
{
    public class KometaMetadata : MetadataBase<KometaMetadataSettings>
    {
        private static readonly Regex GameImagesRegex = new (@"^(?:poster|background)\.(?:png|jpe?g)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILocalizationService _localizationService;

        public override string Name => "Kometa";

        public override ProviderMessage Message => new (_localizationService.GetLocalizedString("MetadataKometaDeprecated"), ProviderMessageType.Warning);

        public KometaMetadata(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public override MetadataFile FindMetadataFile(Game game, string path)
        {
            var filename = Path.GetFileName(path);

            if (filename == null)
            {
                return null;
            }

            var metadata = new MetadataFile
            {
                GameId = game.Id,
                Consumer = GetType().Name,
                RelativePath = game.Path.GetRelativePath(path)
            };

            if (GameImagesRegex.IsMatch(filename))
            {
                metadata.Type = MetadataType.GameImage;
                return metadata;
            }

            return null;
        }

        public override MetadataFileResult GameMetadata(Game game, GameFile gameFile)
        {
            return null;
        }

        public override List<ImageFileResult> GameImages(Game game)
        {
            return new List<ImageFileResult>();
        }
    }
}
