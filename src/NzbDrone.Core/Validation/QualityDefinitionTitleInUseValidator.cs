using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Validation
{
    public class QualityDefinitionTitleInUseValidator
    {
        private readonly IQualityDefinitionService _qualityDefinitionService;

        public QualityDefinitionTitleInUseValidator(IQualityDefinitionService qualityDefinitionService)
        {
            _qualityDefinitionService = qualityDefinitionService;
        }

        public bool Validate(int id, string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return true;
            }

            return !_qualityDefinitionService.All().Any(d => d.Title.EqualsIgnoreCase(title) && d.Id != id);
        }

        // The quality definition UI saves every row in one request, so the batch
        // has to be checked against itself as well as against the rows it is not
        // replacing; otherwise two renamed-alike rows still reach the DB index.
        public bool ValidateBatch(IEnumerable<QualityDefinition> definitions)
        {
            var batch = definitions.ToList();
            var batchIds = batch.Select(d => d.Id).ToList();

            var titles = batch.Select(d => d.Title)
                .Where(t => t.IsNotNullOrWhiteSpace())
                .ToList();

            if (titles.Select(t => t.ToLowerInvariant()).Distinct().Count() != titles.Count)
            {
                return false;
            }

            return !_qualityDefinitionService.All()
                .Where(d => !batchIds.Contains(d.Id))
                .Any(d => titles.Any(t => d.Title.EqualsIgnoreCase(t)));
        }
    }
}
