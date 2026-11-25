using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Npgsql;

namespace Gamarr.Http.ErrorManagement
{
    /// <summary>
    /// Single place that classifies database constraint failures from typed
    /// provider data (SQLite result codes, Postgres SQLSTATE). NOT NULL and
    /// foreign-key failures are deliberately not "unique": they are our bugs
    /// and must stay 500s that reach Sentry.
    /// </summary>
    public static class DatabaseConstraint
    {
        private const string SQLiteUniquePrefix = "UNIQUE constraint failed:";
        private const string SQLitePrimaryKeyPrefix = "PRIMARY KEY constraint failed:";

        public static bool IsUniqueViolation(Exception ex)
        {
            return TryGetUniqueViolation(ex, out _);
        }

        /// <summary>True when the unique violation involves the given column.</summary>
        public static bool IsUniqueViolationOnColumn(Exception ex, string column)
        {
            return TryGetUniqueViolation(ex, out var columns) &&
                   columns.Any(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetUniqueViolation(Exception ex, out IReadOnlyList<string> columns)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is SQLiteException sqlite && TryParseSQLite(sqlite, out columns))
                {
                    return true;
                }

                if (e is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    columns = PostgresColumns(pg);
                    return true;
                }
            }

            columns = Array.Empty<string>();
            return false;
        }

        private static bool TryParseSQLite(SQLiteException ex, out IReadOnlyList<string> columns)
        {
            columns = Array.Empty<string>();

            // Mask to the primary code: the extended code is only present when
            // extended result codes are enabled on the connection.
            if (((int)ex.ResultCode & 0xFF) != (int)SQLiteErrorCode.Constraint)
            {
                return false;
            }

            var extended = ex.ResultCode;
            var message = ex.Message ?? string.Empty;

            // SQLite reports the offending columns only in the message text
            // ("UNIQUE constraint failed: Table.Col[, Table.Col2]").
            string list = null;

            foreach (var prefix in new[] { SQLiteUniquePrefix, SQLitePrimaryKeyPrefix })
            {
                var idx = message.IndexOf(prefix, StringComparison.Ordinal);

                if (idx >= 0)
                {
                    list = message.Substring(idx + prefix.Length);
                    break;
                }
            }

            var isUnique = extended == SQLiteErrorCode.Constraint_Unique ||
                           extended == SQLiteErrorCode.Constraint_PrimaryKey ||
                           list != null;

            if (!isUnique)
            {
                return false;
            }

            columns = (list ?? string.Empty)
                .Split(',')
                .Select(c => c.Trim())
                .Select(c => c.Substring(c.LastIndexOf('.') + 1))
                .Where(c => c.Length > 0)
                .ToList();

            return true;
        }

        private static IReadOnlyList<string> PostgresColumns(PostgresException ex)
        {
            if (!string.IsNullOrEmpty(ex.ColumnName))
            {
                return new[] { ex.ColumnName };
            }

            // Unique violations rarely carry ColumnName; the constraint name
            // ("IX_Table_Col", "Table_Col_key") holds the column as a segment.
            return (ex.ConstraintName ?? string.Empty)
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
    }
}
