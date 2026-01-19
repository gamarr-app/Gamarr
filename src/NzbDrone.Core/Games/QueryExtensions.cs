using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Games
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<Game> AllWithYear(this IEnumerable<Game> query, int? year)
        {
            return year.HasValue ? query.Where(game => game.Year == year || game.GameMetadata.Value.SecondaryYear == year) : query;
        }
    }
}
