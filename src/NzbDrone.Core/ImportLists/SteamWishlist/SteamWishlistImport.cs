using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.SteamWishlist
{
    public class SteamWishlistImport : HttpImportListBase<SteamWishlistSettings>
    {
        public override string Name => "Steam Wishlist";

        public override ImportListType ListType => ImportListType.Steam;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(6);
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public SteamWishlistImport(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new SteamWishlistRequestGenerator
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger
            };
        }

        public override IParseImportListResponse GetParser()
        {
            return new SteamWishlistParser();
        }

        protected override bool IsValidItem(ImportListGame listItem)
        {
            return listItem.SteamAppId > 0;
        }
    }
}
