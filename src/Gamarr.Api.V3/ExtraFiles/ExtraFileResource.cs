using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Others;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.ExtraFiles
{
    public class ExtraFileResource : RestResource
    {
        public int GameId { get; set; }
        public int? GameFileId { get; set; }
        public string RelativePath { get; set; }
        public string Extension { get; set; }
        public ExtraFileType Type { get; set; }
    }

    public static class ExtraFileResourceMapper
    {
        public static ExtraFileResource ToResource(this MetadataFile model)
        {
            if (model == null)
            {
                return null;
            }

            return new ExtraFileResource
            {
                Id = model.Id,
                GameId = model.GameId,
                GameFileId = model.GameFileId,
                RelativePath = model.RelativePath,
                Extension = model.Extension,
                Type = ExtraFileType.Metadata
            };
        }

        public static ExtraFileResource ToResource(this OtherExtraFile model)
        {
            if (model == null)
            {
                return null;
            }

            return new ExtraFileResource
            {
                Id = model.Id,
                GameId = model.GameId,
                GameFileId = model.GameFileId,
                RelativePath = model.RelativePath,
                Extension = model.Extension,
                Type = ExtraFileType.Other
            };
        }

        public static List<ExtraFileResource> ToResource(this IEnumerable<MetadataFile> games)
        {
            return games.Select(ToResource).ToList();
        }

        public static List<ExtraFileResource> ToResource(this IEnumerable<OtherExtraFile> games)
        {
            return games.Select(ToResource).ToList();
        }
    }
}
