using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Test.ImportListTests
{
    public class TestImportList : HttpImportListBase<TestImportListSettings>
    {
        public override string Name => "Test Import List";

        public override ImportListType ListType => ImportListType.Program;

        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        public TestImportList(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public IImportListRequestGenerator _requestGenerator;
        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return _requestGenerator;
        }

        public IParseImportListResponse _parser;
        public override IParseImportListResponse GetParser()
        {
            return _parser;
        }
    }
}
