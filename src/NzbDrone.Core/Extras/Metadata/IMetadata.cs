using System.Collections.Generic;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata
{
    public interface IMetadata : IProvider
    {
        string GetFilenameAfterMove(Game game, GameFile gameFile, MetadataFile metadataFile);
        MetadataFile FindMetadataFile(Game game, string path);
        MetadataFileResult GameMetadata(Game game, GameFile gameFile);
        List<ImageFileResult> GameImages(Game game);
    }
}
