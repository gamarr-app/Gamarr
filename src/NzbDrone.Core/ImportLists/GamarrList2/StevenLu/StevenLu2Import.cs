using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.GamarrList2.StevenLu
{
    public class StevenLu2Import : HttpImportListBase<StevenLu2Settings>
    {
        private readonly IHttpRequestBuilderFactory _gamarrMetadata;

        public override string Name => "StevenLu List";

        public override ImportListType ListType => ImportListType.Other;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public StevenLu2Import(IGamarrCloudRequestBuilder requestBuilder,
                              IHttpClient httpClient,
                              IImportListStatusService importListStatusService,
                              IConfigService configService,
                              IParsingService parsingService,
                              Logger logger)
        : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _gamarrMetadata = requestBuilder.GamarrMetadata;
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new StevenLu2RequestGenerator()
            {
                Settings = Settings,
                Logger = _logger,
                HttpClient = _httpClient,
                RequestBuilder = _gamarrMetadata
            };
        }

        public override IParseImportListResponse GetParser()
        {
            return new GamarrList2Parser();
        }
    }
}
