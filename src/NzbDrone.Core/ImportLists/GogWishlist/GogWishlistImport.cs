using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.Gog;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.GogWishlist
{
    public class GogWishlistImport : HttpImportListBase<GogWishlistSettings>
    {
        private readonly IGogGameResolver _gogGameResolver;

        public override string Name => "GOG Wishlist";

        public override ImportListType ListType => ImportListType.GOG;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(6);
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        // GOG wishlist pages contain 100 products
        public override int PageSize => 100;

        public GogWishlistImport(IGogGameResolver gogGameResolver,
                                 IHttpClient httpClient,
                                 IImportListStatusService importListStatusService,
                                 IConfigService configService,
                                 IParsingService parsingService,
                                 Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _gogGameResolver = gogGameResolver;
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new GogWishlistRequestGenerator
            {
                Settings = Settings,
                Logger = _logger
            };
        }

        public override IParseImportListResponse GetParser()
        {
            return new GogWishlistParser();
        }

        protected override IList<ImportListGame> FetchPage(ImportListRequest request, IParseImportListResponse parser)
        {
            var response = FetchImportListResponse(request);
            var products = ((GogWishlistParser)parser).ParseProducts(response);

            return _gogGameResolver.ResolveGames(products).ToList();
        }

        protected override bool IsValidItem(ImportListGame listItem)
        {
            // Sync can only process games identified by IGDB or Steam ids;
            // GOG products the resolver couldn't map are dropped here.
            return listItem.IgdbId > 0 || listItem.SteamAppId > 0;
        }
    }
}
