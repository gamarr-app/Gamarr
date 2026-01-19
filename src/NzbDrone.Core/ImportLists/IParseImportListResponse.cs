using System.Collections.Generic;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists
{
    public interface IParseImportListResponse
    {
        IList<ImportListGame> ParseResponse(ImportListResponse importListResponse);
    }
}
