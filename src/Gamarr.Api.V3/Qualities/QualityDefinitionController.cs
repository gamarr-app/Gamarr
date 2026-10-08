using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Validation;
using NzbDrone.SignalR;
using Gamarr.Http;
using Gamarr.Http.REST;
using Gamarr.Http.REST.Attributes;

namespace Gamarr.Api.V3.Qualities
{
    [V3ApiController]
    public class QualityDefinitionController :
        RestControllerWithSignalR<QualityDefinitionResource, QualityDefinition>,
        IHandle<CommandExecutedEvent>
    {
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly QualityDefinitionTitleInUseValidator _titleInUseValidator;

        public QualityDefinitionController(
            IQualityDefinitionService qualityDefinitionService,
            IBroadcastSignalRMessage signalRBroadcaster,
            QualityDefinitionTitleInUseValidator titleInUseValidator)
            : base(signalRBroadcaster)
        {
            _qualityDefinitionService = qualityDefinitionService;
            _titleInUseValidator = titleInUseValidator;

            SharedValidator.RuleFor(c => c)
                .SetValidator(new QualityDefinitionResourceValidator());

            SharedValidator.RuleFor(c => c.Title)
                .Must((v, c) => titleInUseValidator.Validate(v.Id, c))
                .WithMessage("Should be unique");
        }

        [RestPutById]
        public ActionResult<QualityDefinitionResource> Update([FromBody] QualityDefinitionResource resource)
        {
            var model = resource.ToModel();
            _qualityDefinitionService.Update(model);
            return Accepted(model.Id);
        }

        protected override QualityDefinitionResource GetResourceById(int id)
        {
            return _qualityDefinitionService.GetById(id).ToResource();
        }

        [HttpGet]
        public List<QualityDefinitionResource> GetAll()
        {
            return _qualityDefinitionService.All().ToResource();
        }

        [HttpPut("update")]
        public object UpdateMany([FromBody] List<QualityDefinitionResource> resource)
        {
            // Read from request
            var qualityDefinitions = resource.ToModel().ToList();

            // This route, not the single-item PUT, is what the UI saves through,
            // and it bypasses the validator pipeline entirely.
            if (!_titleInUseValidator.ValidateBatch(qualityDefinitions))
            {
                throw new ValidationException(new List<ValidationFailure> { new ("Title", "Should be unique") });
            }

            _qualityDefinitionService.UpdateMany(qualityDefinitions);

            return Accepted(_qualityDefinitionService.All()
                .ToResource());
        }

        [HttpGet("limits")]
        public ActionResult<QualityDefinitionLimitsResource> GetLimits()
        {
            return Ok(new QualityDefinitionLimitsResource(
                QualityDefinitionLimits.Min,
                QualityDefinitionLimits.Max));
        }

        [NonAction]
        public void Handle(CommandExecutedEvent message)
        {
            if (message.Command.Name == "ResetQualityDefinitions")
            {
                BroadcastResourceChange(ModelAction.Sync);
            }
        }
    }
}
