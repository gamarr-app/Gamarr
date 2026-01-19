using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.MediaFiles;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Games
{
    public class RenameGameResource : RestResource
    {
        public int GameId { get; set; }
        public int GameFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }

    public static class RenameGameResourceMapper
    {
        public static RenameGameResource ToResource(this RenameGameFilePreview model)
        {
            if (model == null)
            {
                return null;
            }

            return new RenameGameResource
            {
                GameId = model.GameId,
                GameFileId = model.GameFileId,
                ExistingPath = model.ExistingPath,
                NewPath = model.NewPath
            };
        }

        public static List<RenameGameResource> ToResource(this IEnumerable<RenameGameFilePreview> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
