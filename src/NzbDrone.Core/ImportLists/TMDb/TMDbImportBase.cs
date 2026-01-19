using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.TMDb
{
    public abstract class TMDbImportListBase<TSettings> : HttpImportListBase<TSettings>
        where TSettings : TMDbSettingsBase<TSettings>, new()
    {
        public override ImportListType ListType => ImportListType.IGDB;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override int PageSize => 20;
        protected override bool UsePreGeneratedPages => true;

        public readonly ISearchForNewGame _skyhookProxy;
        public readonly IHttpRequestBuilderFactory _requestBuilder;

        protected TMDbImportListBase(IGamarrCloudRequestBuilder requestBuilder,
                                    IHttpClient httpClient,
                                    IImportListStatusService importListStatusService,
                                    IConfigService configService,
                                    IParsingService parsingService,
                                    ISearchForNewGame skyhookProxy,
                                    Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _skyhookProxy = skyhookProxy;
            _requestBuilder = requestBuilder.IGDB;
        }
    }
}
