using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.TMDb
{
    /// <summary>
    /// DEPRECATED: TMDb import lists are movie-specific and do not apply to games.
    /// This class is kept for backwards compatibility but should not be used.
    /// Use IGDB-based import lists instead.
    /// </summary>
    [Obsolete("TMDb import lists are movie-specific and do not apply to games. Use IGDB-based lists instead.")]
    public abstract class TMDbImportListBase<TSettings> : HttpImportListBase<TSettings>
        where TSettings : TMDbSettingsBase<TSettings>, new()
    {
        public override ImportListType ListType => ImportListType.IGDB;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override int PageSize => 20;
        protected override bool UsePreGeneratedPages => true;

        // Mark as disabled by default since TMDb doesn't apply to games
        public override bool Enabled => false;
        public override bool EnableAuto => false;

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
