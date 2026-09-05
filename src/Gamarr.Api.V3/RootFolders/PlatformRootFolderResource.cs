using System.Collections.Generic;
using System.Linq;
using Gamarr.Http.REST;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;
using NzbDrone.Core.RootFolders;

namespace Gamarr.Api.V3.RootFolders
{
    public class PlatformRootFolderResource : RestResource
    {
        public PlatformFamily Platform { get; set; }
        public string Path { get; set; }
    }

    public static class PlatformRootFolderResourceMapper
    {
        public static PlatformRootFolderResource ToResource(this PlatformRootFolder model)
        {
            if (model == null)
            {
                return null;
            }

            return new PlatformRootFolderResource
            {
                Id = model.Id,
                Platform = model.Platform,
                Path = model.Path.GetCleanPath()
            };
        }

        public static PlatformRootFolder ToModel(this PlatformRootFolderResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new PlatformRootFolder
            {
                Id = resource.Id,
                Platform = resource.Platform,
                Path = resource.Path
            };
        }

        public static List<PlatformRootFolderResource> ToResource(this IEnumerable<PlatformRootFolder> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
