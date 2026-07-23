using System;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.RomCatalog
{
    public static class NoIntroRenameProfileEvaluator
    {
        public static string GetExpectedFileName(NoIntroCatalogEntry catalogEntry, string actualFileName, RenameProfile renameProfile)
        {
            if (renameProfile == RenameProfile.NoIntroPreserveById && !string.IsNullOrWhiteSpace(catalogEntry.NumberedCanonicalFileName))
            {
                return catalogEntry.NumberedCanonicalFileName;
            }

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

            if (renameProfile != RenameProfile.NoIntroPreserveById)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(catalogEntry.NumberedCanonicalFileName)
                ? actualFileName.Equals(catalogEntry.NumberedCanonicalFileName, StringComparison.Ordinal)
                : IsByIdFileName(actualFileName, catalogEntry.CanonicalFileName);
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
