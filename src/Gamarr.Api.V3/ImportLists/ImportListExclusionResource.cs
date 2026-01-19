using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.ImportLists.ImportExclusions;

namespace Gamarr.Api.V3.ImportLists
{
    public class ImportListExclusionResource : ProviderResource<ImportListExclusionResource>
    {
        // public int Id { get; set; }
        public int IgdbId { get; set; }
        public string GameTitle { get; set; }
        public int GameYear { get; set; }
    }

    public static class ImportListExclusionResourceMapper
    {
        public static ImportListExclusionResource ToResource(this ImportListExclusion model)
        {
            if (model == null)
            {
                return null;
            }

            return new ImportListExclusionResource
            {
                Id = model.Id,
                IgdbId = model.IgdbId,
                GameTitle = model.GameTitle,
                GameYear = model.GameYear
            };
        }

        public static List<ImportListExclusionResource> ToResource(this IEnumerable<ImportListExclusion> exclusions)
        {
            return exclusions.Select(ToResource).ToList();
        }

        public static ImportListExclusion ToModel(this ImportListExclusionResource resource)
        {
            return new ImportListExclusion
            {
                Id = resource.Id,
                IgdbId = resource.IgdbId,
                GameTitle = resource.GameTitle,
                GameYear = resource.GameYear
            };
        }

        public static List<ImportListExclusion> ToModel(this IEnumerable<ImportListExclusionResource> resources)
        {
            return resources.Select(ToModel).ToList();
        }
    }
}
