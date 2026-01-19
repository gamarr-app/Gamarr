using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(117)]
    public class update_game_file : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Column("Edition").OnTable("GameFiles").AsString().Nullable();

            // Execute.WithConnection(SetSortTitles);
        }

        private void SetSortTitles(IDbConnection conn, IDbTransaction tran)
        {
            using (var getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"SELECT ""Id"", ""RelativePath"" FROM ""GameFiles""";
                using (var seriesReader = getSeriesCmd.ExecuteReader())
                {
                    while (seriesReader.Read())
                    {
                        var id = seriesReader.GetInt32(0);
                        var relativePath = seriesReader.GetString(1);

                        var result = Parser.Parser.ParseGameTitle(relativePath);

                        var edition = "";

                        if (result != null)
                        {
                            edition = result.Edition ?? Parser.Parser.ParseEdition(result.SimpleReleaseTitle);
                        }

                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = tran;
                            updateCmd.CommandText = "UPDATE \"GameFiles\" SET \"Edition\" = ? WHERE \"Id\" = ?";
                            updateCmd.AddParameter(edition);
                            updateCmd.AddParameter(id);

                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
