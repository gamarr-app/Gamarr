using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Core.ImportLists.ImportExclusions;

namespace Gamarr.Api.V3.ImportLists
{
    public class ImportListExclusionExistsValidator : PropertyValidator<ImportListExclusionResource, int>
    {
        private readonly IImportListExclusionService _importListExclusionService;

        public ImportListExclusionExistsValidator(IImportListExclusionService importListExclusionService)
        {
            _importListExclusionService = importListExclusionService;
        }

        public override string Name => "ImportListExclusionExistsValidator";

        public override bool IsValid(ValidationContext<ImportListExclusionResource> context, int value)
        {
            var resource = context.InstanceToValidate;

            return !_importListExclusionService.All().Exists(v => v.IgdbId == value && v.Id != resource.Id);
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "This exclusion has already been added.";
    }
}
