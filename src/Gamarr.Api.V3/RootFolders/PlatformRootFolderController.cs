using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Gamarr.Http;
using Gamarr.Http.REST;
using Gamarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation.Paths;

namespace Gamarr.Api.V3.RootFolders
{
    [V3ApiController]
    public class PlatformRootFolderController : RestController<PlatformRootFolderResource>
    {
        private readonly IPlatformRootFolderService _platformRootFolderService;

        public PlatformRootFolderController(IPlatformRootFolderService platformRootFolderService,
                                            PathExistsValidator pathExistsValidator,
                                            MappedNetworkDriveValidator mappedNetworkDriveValidator)
        {
            _platformRootFolderService = platformRootFolderService;

            SharedValidator.RuleFor(c => c.Path)
                .Cascade(CascadeMode.Stop)
                .IsValidPath()
                .SetPathValidator(mappedNetworkDriveValidator)
                .SetPathValidator(pathExistsValidator);

            SharedValidator.RuleFor(c => c.Platform)
                .Must((resource, platform) => !_platformRootFolderService.All()
                                                                         .Any(p => p.Platform == platform && p.Id != resource.Id))
                .WithMessage("A default root folder for this platform already exists");
        }

        protected override PlatformRootFolderResource GetResourceById(int id)
        {
            return _platformRootFolderService.Get(id).ToResource();
        }

        [HttpGet]
        public List<PlatformRootFolderResource> GetPlatformRootFolders()
        {
            return _platformRootFolderService.All().ToResource();
        }

        [RestPostById]
        [Consumes("application/json")]
        public ActionResult<PlatformRootFolderResource> CreatePlatformRootFolder([FromBody] PlatformRootFolderResource resource)
        {
            return Created(_platformRootFolderService.Add(resource.ToModel()).Id);
        }

        [RestPutById]
        [Consumes("application/json")]
        public ActionResult<PlatformRootFolderResource> UpdatePlatformRootFolder([FromBody] PlatformRootFolderResource resource)
        {
            return Accepted(_platformRootFolderService.Update(resource.ToModel()).Id);
        }

        [RestDeleteById]
        public void DeletePlatformRootFolder(int id)
        {
            _platformRootFolderService.Remove(id);
        }
    }
}
