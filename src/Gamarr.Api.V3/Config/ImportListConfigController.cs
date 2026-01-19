using NzbDrone.Core.Configuration;
using Gamarr.Http;

namespace Gamarr.Api.V3.Config
{
    [V3ApiController("config/importlist")]

    public class ImportListConfigController : ConfigController<ImportListConfigResource>
    {
        public ImportListConfigController(IConfigService configService)
            : base(configService)
        {
        }

        protected override ImportListConfigResource ToResource(IConfigService model)
        {
            return ImportListConfigResourceMapper.ToResource(model);
        }
    }
}
