using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tags;

namespace NzbDrone.Core.Validation
{
    public class TagLabelInUseValidator
    {
        private readonly ITagService _tagService;

        public TagLabelInUseValidator(ITagService tagService)
        {
            _tagService = tagService;
        }

        public bool Validate(int id, string label)
        {
            if (label.IsNullOrWhiteSpace())
            {
                return true;
            }

            return !_tagService.All().Any(t => t.Label.EqualsIgnoreCase(label) && t.Id != id);
        }
    }
}
