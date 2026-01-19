using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ModelEvent<NamingConfig>))]
    public class NamingConfigCheck : HealthCheckBase, IProvideHealthCheck
    {
        private readonly INamingConfigService _namingConfigService;

        public NamingConfigCheck(INamingConfigService namingConfigService, ILocalizationService localizationService)
            : base(localizationService)
        {
            _namingConfigService = namingConfigService;
        }

        public override HealthCheck Check()
        {
            var namingConfig = _namingConfigService.GetConfig();

            if (namingConfig.GameFolderFormat.IsNotNullOrWhiteSpace())
            {
                var match = FileNameValidation.DeprecatedGameFolderTokensRegex.Matches(namingConfig.GameFolderFormat);

                if (match.Any())
                {
                    return new HealthCheck(
                        GetType(),
                        HealthCheckResult.Error,
                        _localizationService.GetLocalizedString(
                            "NamingConfigGameFolderFormatDeprecatedHealthCheckMessage", new Dictionary<string, object>
                            {
                                { "tokens", string.Join(", ", match.Select(c => c.Value).ToArray()) },
                            }));
                }
            }

            return new HealthCheck(GetType());
        }
    }
}
