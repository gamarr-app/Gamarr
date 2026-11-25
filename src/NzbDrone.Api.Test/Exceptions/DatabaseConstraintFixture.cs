using System;
using System.Data.SQLite;
using FluentAssertions;
using Gamarr.Api.V3;
using Gamarr.Http.ErrorManagement;
using Npgsql;
using NUnit.Framework;

namespace NzbDrone.Api.Test.Exceptions
{
    [TestFixture]
    public class DatabaseConstraintFixture
    {
        private static SQLiteException Sqlite(SQLiteErrorCode code, string message)
        {
            return new SQLiteException(code, message);
        }

        private static PostgresException Postgres(string sqlState, string constraint = null, string column = null)
        {
            return new PostgresException(
                "failure",
                "ERROR",
                "ERROR",
                sqlState,
                constraintName: constraint,
                columnName: column);
        }

        [Test]
        public void should_detect_sqlite_unique_on_name_with_primary_code()
        {
            var ex = Sqlite(SQLiteErrorCode.Constraint, "constraint failed\r\nUNIQUE constraint failed: QualityProfiles.Name");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeTrue();
            UniqueConstraintViolation.IsNameViolation(ex).Should().BeTrue();
        }

        [Test]
        public void should_detect_sqlite_unique_via_extended_code()
        {
            var ex = Sqlite(SQLiteErrorCode.Constraint_Unique, "unrelated text");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeTrue();
        }

        [Test]
        public void should_not_match_other_column_containing_name()
        {
            var ex = Sqlite(SQLiteErrorCode.Constraint, "constraint failed\r\nUNIQUE constraint failed: Games.TypeName");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeTrue();
            UniqueConstraintViolation.IsNameViolation(ex).Should().BeFalse();
        }

        [Test]
        public void should_match_name_in_composite_unique()
        {
            var ex = Sqlite(SQLiteErrorCode.Constraint, "UNIQUE constraint failed: Tags.Kind, Tags.Name");

            UniqueConstraintViolation.IsNameViolation(ex).Should().BeTrue();
        }

        [Test]
        public void should_not_treat_not_null_as_unique()
        {
            var ex = Sqlite(SQLiteErrorCode.Constraint, "constraint failed\r\nNOT NULL constraint failed: Games.Name");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeFalse();
            UniqueConstraintViolation.IsNameViolation(ex).Should().BeFalse();
        }

        [Test]
        public void should_not_treat_foreign_key_as_unique()
        {
            var ex = Sqlite(SQLiteErrorCode.Constraint, "constraint failed\r\nFOREIGN KEY constraint failed");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeFalse();
        }

        [Test]
        public void should_not_treat_non_constraint_sqlite_error_as_unique()
        {
            var ex = Sqlite(SQLiteErrorCode.Busy, "UNIQUE constraint failed: X.Name");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeFalse();
        }

        [Test]
        public void should_detect_postgres_unique_violation()
        {
            var ex = Postgres(PostgresErrorCodes.UniqueViolation, "IX_QualityProfiles_Name");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeTrue();
            UniqueConstraintViolation.IsNameViolation(ex).Should().BeTrue();
        }

        [Test]
        public void should_use_postgres_column_name_when_present()
        {
            var ex = Postgres(PostgresErrorCodes.UniqueViolation, "pk_x", "Name");

            UniqueConstraintViolation.IsNameViolation(ex).Should().BeTrue();
        }

        [Test]
        public void should_not_match_postgres_unique_on_other_column()
        {
            var ex = Postgres(PostgresErrorCodes.UniqueViolation, "IX_Games_TypeName");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeTrue();
            UniqueConstraintViolation.IsNameViolation(ex).Should().BeFalse();
        }

        [Test]
        public void should_not_treat_postgres_not_null_or_fk_as_unique()
        {
            DatabaseConstraint.IsUniqueViolation(Postgres(PostgresErrorCodes.NotNullViolation, column: "Name")).Should().BeFalse();
            DatabaseConstraint.IsUniqueViolation(Postgres(PostgresErrorCodes.ForeignKeyViolation)).Should().BeFalse();
        }

        [Test]
        public void should_find_violation_in_inner_exception()
        {
            var inner = Sqlite(SQLiteErrorCode.Constraint, "UNIQUE constraint failed: QualityProfiles.Name");
            var ex = new InvalidOperationException("wrapped", inner);

            UniqueConstraintViolation.IsNameViolation(ex).Should().BeTrue();
        }

        [Test]
        public void should_ignore_message_text_on_untyped_exceptions()
        {
            var ex = new Exception("UNIQUE constraint failed: QualityProfiles.Name duplicate key");

            DatabaseConstraint.IsUniqueViolation(ex).Should().BeFalse();
        }
    }
}
