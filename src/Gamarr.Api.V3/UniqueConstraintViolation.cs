using System;
using Gamarr.Http.ErrorManagement;

namespace Gamarr.Api.V3
{
    public static class UniqueConstraintViolation
    {
        // Two concurrent submits (e.g. a double-clicked save) can both pass a
        // SharedValidator uniqueness rule before either row exists, so the DB
        // index is the last line of defence. Callers use this to surface that
        // as the same validation error instead of a 500.
        public static bool IsNameViolation(Exception ex)
        {
            return DatabaseConstraint.IsUniqueViolationOnColumn(ex, "Name");
        }
    }
}
