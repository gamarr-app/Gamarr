using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists.GamarrList2.IMDbList
{
    /// <summary>
    /// DEPRECATED: IMDb lists are movie-specific and do not apply to games.
    /// This class is kept for backwards compatibility but should not be used.
    /// Use IGDB-based import lists instead.
    /// </summary>
    [Obsolete("IMDb lists are movie-specific and do not apply to games. Use IGDB-based lists instead.")]
    public class IMDbListImport : HttpImportListBase<IMDbListSettings>
    {
        private readonly IHttpRequestBuilderFactory _gamarrMetadata;

        public override string Name => "IMDb Lists (Deprecated)";

        public override ImportListType ListType => ImportListType.Other;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        // Disabled by default since IMDb doesn't apply to games
        public override bool Enabled => false;
        public override bool EnableAuto => false;

        public IMDbListImport(IGamarrCloudRequestBuilder requestBuilder,
                              IHttpClient httpClient,
                              IImportListStatusService importListStatusService,
                              IConfigService configService,
                              IParsingService parsingService,
                              Logger logger)
        : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _gamarrMetadata = requestBuilder.GamarrMetadata;
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                foreach (var def in base.DefaultDefinitions)
                {
                    yield return def;
                }

                yield return new ImportListDefinition
                {
                    Name = "IMDb Top 250",
                    Enabled = Enabled,
                    EnableAuto = true,
                    QualityProfileId = 1,
                    Implementation = GetType().Name,
                    MinRefreshInterval = MinRefreshInterval,
                    Settings = new IMDbListSettings { ListId = "top250" },
                };
                yield return new ImportListDefinition
                {
                    Name = "IMDb Popular Games",
                    Enabled = Enabled,
                    EnableAuto = true,
                    QualityProfileId = 1,
                    Implementation = GetType().Name,
                    MinRefreshInterval = MinRefreshInterval,
                    Settings = new IMDbListSettings { ListId = "popular" },
                };
            }
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new IMDbListRequestGenerator()
            {
                Settings = Settings,
                Logger = _logger,
                HttpClient = _httpClient,
                RequestBuilder = _gamarrMetadata
            };
        }

        public override IParseImportListResponse GetParser()
        {
            return new IMDbListParser(Settings, _logger);
        }
    }
}
