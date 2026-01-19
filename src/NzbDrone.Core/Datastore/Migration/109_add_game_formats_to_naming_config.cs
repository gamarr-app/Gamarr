using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(109)]
    public class add_game_formats_to_naming_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NamingConfig").AddColumn("StandardGameFormat").AsString().Nullable();
            Alter.Table("NamingConfig").AddColumn("GameFolderFormat").AsString().Nullable();

            Execute.WithConnection(ConvertConfig);
        }

        private void ConvertConfig(IDbConnection conn, IDbTransaction tran)
        {
            using (var namingConfigCmd = conn.CreateCommand())
            {
                namingConfigCmd.Transaction = tran;
                namingConfigCmd.CommandText = @"SELECT * FROM ""NamingConfig"" LIMIT 1";
                using (var namingConfigReader = namingConfigCmd.ExecuteReader())
                {
                    while (namingConfigReader.Read())
                    {
                        // Output Settings
                        var gameTitlePattern = "";
                        var gameYearPattern = "({Release Year})";
                        var qualityFormat = "[{Quality Title}]";

                        gameTitlePattern = "{Game Title}";

                        var standardGameFormat = string.Format("{0} {1} {2}", gameTitlePattern, gameYearPattern, qualityFormat);

                        var gameFolderFormat = string.Format("{0} {1}", gameTitlePattern, gameYearPattern);

                        using (var updateCmd = conn.CreateCommand())
                        {
                            var text = string.Format("UPDATE \"NamingConfig\" " +
                                                     "SET \"StandardGameFormat\" = '{0}', " +
                                                     "\"GameFolderFormat\" = '{1}'",
                                                     standardGameFormat,
                                                     gameFolderFormat);

                            updateCmd.Transaction = tran;
                            updateCmd.CommandText = text;
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
