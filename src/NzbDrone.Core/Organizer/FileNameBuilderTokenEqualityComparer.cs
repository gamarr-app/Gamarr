using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Organizer
{
    public class FileNameBuilderTokenEqualityComparer : IEqualityComparer<string>
    {
        public static readonly FileNameBuilderTokenEqualityComparer Instance = new FileNameBuilderTokenEqualityComparer();

        private static readonly Regex SimpleTokenRegex = new Regex(@"\s|_|\W", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private FileNameBuilderTokenEqualityComparer()
        {
        }

        public bool Equals(string x, string y)
        {
            return SimplifyToken(x).Equals(SimplifyToken(y));
        }

        public int GetHashCode(string obj)
        {
            return SimplifyToken(obj).GetHashCode();
        }

        private static string SimplifyToken(string token)
        {
            return SimpleTokenRegex.Replace(token, string.Empty).ToLower();
        }
    }
}
