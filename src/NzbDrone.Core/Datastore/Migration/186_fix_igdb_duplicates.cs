using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(186)]
    public class fix_igdb_duplicates : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(FixGames);
            Delete.Index("IX_Games_IgdbId").OnTable("Games");
            Alter.Table("Games").AlterColumn("IgdbId").AsInt32().Unique();
        }

        private void FixGames(IDbConnection conn, IDbTransaction tran)
        {
            var gameRows = conn.Query<GameEntity185>($"SELECT \"Id\", \"IgdbId\", \"Added\", \"LastInfoSync\", \"GameFileId\" FROM \"Games\"");

            // Only process if there are games existing in the DB
            if (gameRows.Any())
            {
                var gameGroups = gameRows.GroupBy(m => m.IgdbId);
                var problemGames = gameGroups.Where(g => g.Count() > 1);
                var purgeGames = new List<GameEntity185>();

                // Don't do anything if there are no duplicate games
                if (!problemGames.Any())
                {
                    return;
                }

                // Process duplicates to pick which to purge
                foreach (var problemGroup in problemGames)
                {
                    var gamesWithFiles = problemGroup.Where(m => m.GameFileId > 0);
                    var gamesWithInfo = problemGroup.Where(m => m.LastInfoSync != null);

                    // If we only have one with file keep it
                    if (gamesWithFiles.Count() == 1)
                    {
                        purgeGames.AddRange(problemGroup.Where(m => m.GameFileId == 0).Select(m => m));
                        continue;
                    }

                    // If we only have one with info keep it
                    if (gamesWithInfo.Count() == 1)
                    {
                        purgeGames.AddRange(problemGroup.Where(m => m.LastInfoSync == null).Select(m => m));
                        continue;
                    }

                    // Else Prioritize by having file then Added
                    purgeGames.AddRange(problemGroup.OrderByDescending(m => m.GameFileId > 0 ? 1 : 0).ThenBy(m => m.Added).Skip(1).Select(m => m));
                }

                if (purgeGames.Count > 0)
                {
                    var deleteSql = "DELETE FROM \"Games\" WHERE \"Id\" = @Id";
                    conn.Execute(deleteSql, purgeGames, transaction: tran);
                }

                // Delete duplicates, files, metadata, history, etc...
                // (Or just the game and let housekeeper take the rest)
            }
        }

        private class GameEntity185
        {
            public int Id { get; set; }
            public int IgdbId { get; set; }
            public DateTime Added { get; set; }
            public DateTime? LastInfoSync { get; set; }
            public int GameFileId { get; set; }
        }
    }
}
