using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists.GamarrList
{
    public class GamarrListImport : HttpImportListBase<GamarrListSettings>
    {
        public override string Name => "Custom Lists";

        public override ImportListType ListType => ImportListType.Advanced;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public GamarrListImport(IHttpClient httpClient,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                foreach (var def in base.DefaultDefinitions)
                {
                    yield return def;
                }
            }
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new GamarrListRequestGenerator()
            {
                Settings = Settings,
                Logger = _logger,
                HttpClient = _httpClient
            };
        }

        public override IParseImportListResponse GetParser()
        {
            return new GamarrListParser();
        }
    }
}
