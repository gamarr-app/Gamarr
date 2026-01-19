#pragma warning disable CS0618
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.TMDb.Popular
{
    public class TMDbPopularImport : TMDbImportListBase<TMDbPopularSettings>
    {
        public TMDbPopularImport(IGamarrCloudRequestBuilder requestBuilder,
                                 IHttpClient httpClient,
                                 IImportListStatusService importListStatusService,
                                 IConfigService configService,
                                 IParsingService parsingService,
                                 ISearchForNewGame searchForNewGame,
                                 Logger logger)
        : base(requestBuilder, httpClient, importListStatusService, configService, parsingService, searchForNewGame, logger)
        {
        }

        public override string Name => "TMDb Popular";
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public override IParseImportListResponse GetParser()
        {
            return new TMDbParser();
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new TMDbPopularRequestGenerator()
            {
                RequestBuilder = _requestBuilder,
                Settings = Settings,
                Logger = _logger,
                HttpClient = _httpClient
            };
        }
    }
}
