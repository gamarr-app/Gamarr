using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Gamarr.Http;
using NzbDrone.Core.RomCatalog;

namespace Gamarr.Api.V3.RomCatalog
{
    [V3ApiController("romcatalog")]
    public class NoIntroCatalogController : Controller
    {
        private readonly INoIntroCatalogSourceRepository _sourceRepository;
        private readonly INoIntroCatalogEntryRepository _entryRepository;
        private readonly INoIntroVerificationResultRepository _resultRepository;
        private readonly INoIntroComponentClassifier _componentClassifier;

        public NoIntroCatalogController(
            INoIntroCatalogSourceRepository sourceRepository,
            INoIntroCatalogEntryRepository entryRepository,
            INoIntroVerificationResultRepository resultRepository,
            INoIntroComponentClassifier componentClassifier)
        {
            _sourceRepository = sourceRepository;
            _entryRepository = entryRepository;
            _resultRepository = resultRepository;
            _componentClassifier = componentClassifier;
        }

        [HttpGet("source")]
        [Produces("application/json")]
        public List<NoIntroCatalogSourceResource> GetSources()
        {
            return _sourceRepository.All().Select(x => x.ToResource()).ToList();
        }

        [HttpGet("entry")]
        [Produces("application/json")]
        public List<NoIntroCatalogEntryResource> GetEntries([FromQuery] int catalogSourceId)
        {
            var entries = catalogSourceId > 0 ? _entryRepository.GetBySourceId(catalogSourceId) : _entryRepository.All();
            return entries.Select(x => x.ToResource()).ToList();
        }

        [HttpGet("verification")]
        [Produces("application/json")]
        public List<NoIntroVerificationResultResource> GetVerificationResults()
        {
            return _resultRepository.All().Select(x => x.ToResource()).ToList();
        }

        [HttpGet("componentplan")]
        [Produces("application/json")]
        public NoIntroCatalogPlanResource GetComponentPlan([FromQuery] int catalogSourceId)
        {
            var entries = catalogSourceId > 0 ? _entryRepository.GetBySourceId(catalogSourceId) : _entryRepository.All();
            return _componentClassifier.BuildCatalogPlan(entries).ToResource();
        }
    }
}
