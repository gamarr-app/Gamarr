using System;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.RomCatalog
{
    public static class NoIntroRenameProfileEvaluator
    {
        public static string GetExpectedFileName(NoIntroCatalogEntry catalogEntry, string actualFileName, RenameProfile renameProfile)
        {
            if (renameProfile == RenameProfile.NoIntroPreserveById && IsByIdFileName(actualFileName, catalogEntry.CanonicalFileName))
            {
                return actualFileName;
            }

            return catalogEntry.CanonicalFileName;
        }

        public static bool MatchesProfile(NoIntroCatalogEntry catalogEntry, string actualFileName, RenameProfile renameProfile)
        {
            if (actualFileName.Equals(catalogEntry.CanonicalFileName, StringComparison.Ordinal))
            {
                return true;
            }

            return renameProfile == RenameProfile.NoIntroPreserveById &&
                   IsByIdFileName(actualFileName, catalogEntry.CanonicalFileName);
        }

        private static bool IsByIdFileName(string actualFileName, string canonicalFileName)
        {
            if (string.IsNullOrWhiteSpace(actualFileName) || string.IsNullOrWhiteSpace(canonicalFileName))
            {
                return false;
            }

            var prefix = $" - {canonicalFileName}";
            return actualFileName.EndsWith(prefix, StringComparison.Ordinal);
        }
    }
}
