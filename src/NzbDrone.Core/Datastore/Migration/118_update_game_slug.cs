using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(118)]
    public class update_game_slug : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(SetTitleSlug);
        }

        private void SetTitleSlug(IDbConnection conn, IDbTransaction tran)
        {
            using (var getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"SELECT ""Id"", ""Title"", ""Year"", ""IgdbId"" FROM ""Games""";
                using (var seriesReader = getSeriesCmd.ExecuteReader())
                {
                    while (seriesReader.Read())
                    {
                        var id = seriesReader.GetInt32(0);
                        var title = seriesReader.GetString(1);
                        var year = seriesReader.GetInt32(2);
                        var igdbId = seriesReader.GetInt32(3);

                        var titleSlug = Parser.Parser.ToUrlSlug(title + "-" + igdbId);

                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = tran;
                            updateCmd.CommandText = "UPDATE \"Games\" SET \"TitleSlug\" = ? WHERE \"Id\" = ?";
                            updateCmd.AddParameter(titleSlug);
                            updateCmd.AddParameter(id);

                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
