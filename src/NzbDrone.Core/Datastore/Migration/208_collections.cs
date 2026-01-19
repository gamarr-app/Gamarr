using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(208)]
    public class add_collections : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("Collections")
                .WithColumn("IgdbId").AsInt32().Unique()
                .WithColumn("QualityProfileId").AsInt32()
                .WithColumn("RootFolderPath").AsString()
                .WithColumn("MinimumAvailability").AsInt32()
                .WithColumn("SearchOnAdd").AsBoolean()
                .WithColumn("Title").AsString()
                .WithColumn("SortTitle").AsString().Nullable()
                .WithColumn("CleanTitle").AsString()
                .WithColumn("Overview").AsString().Nullable()
                .WithColumn("Images").AsString().WithDefaultValue("[]")
                .WithColumn("Monitored").AsBoolean().WithDefaultValue(false)
                .WithColumn("LastInfoSync").AsDateTime().Nullable()
                .WithColumn("Added").AsDateTime().Nullable();

            Alter.Table("GameMetadata").AddColumn("CollectionIgdbId").AsInt32().Nullable()
                                        .AddColumn("CollectionTitle").AsString().Nullable();

            Alter.Table("ImportLists").AddColumn("Monitor").AsInt32().Nullable();

            Execute.WithConnection(MigrateCollections);
            Execute.WithConnection(MigrateCollectionMonitorStatus);
            Execute.WithConnection(MapCollections);
            Execute.WithConnection(MigrateListMonitor);

            Alter.Table("ImportLists").AlterColumn("Monitor").AsInt32().NotNullable();

            Delete.Column("ShouldMonitor").FromTable("ImportLists");
            Delete.FromTable("ImportLists").Row(new { Implementation = "TMDbCollectionImport" });
            Delete.Column("Collection").FromTable("GameMetadata");
        }

        private void MigrateCollections(IDbConnection conn, IDbTransaction tran)
        {
            var rootPaths = new List<string>();
            using (var getRootFolders = conn.CreateCommand())
            {
                getRootFolders.Transaction = tran;
                getRootFolders.CommandText = @"SELECT ""Path"" FROM ""RootFolders""";

                using (var definitionsReader = getRootFolders.ExecuteReader())
                {
                    while (definitionsReader.Read())
                    {
                        var path = definitionsReader.GetString(0);
                        rootPaths.Add(path);
                    }
                }
            }

            var newCollections = new List<GameCollection208>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Collection\", \"ProfileId\", \"MinimumAvailability\", \"Path\" FROM \"Games\" JOIN \"GameMetadata\" ON \"Games\".\"GameMetadataId\" = \"GameMetadata\".\"Id\" WHERE \"Collection\" IS NOT NULL";

                var addedCollections = new List<int>();
                var added = DateTime.UtcNow;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var collection = reader.GetString(0);
                        var qualityProfileId = reader.GetInt32(1);
                        var minimumAvailability = reader.GetInt32(2);
                        var gamePath = reader.GetString(3);
                        var data = STJson.Deserialize<GameCollection207>(collection);

                        if (data.IgdbId == 0 || newCollections.Any(d => d.IgdbId == data.IgdbId))
                        {
                            continue;
                        }

                        var rootFolderPath = rootPaths.Where(r => r.IsParentPath(gamePath))
                                          .OrderByDescending(r => r.Length)
                                          .FirstOrDefault();

                        if (rootFolderPath == null)
                        {
                            rootFolderPath = rootPaths.FirstOrDefault();
                        }

                        if (rootFolderPath == null)
                        {
                            rootFolderPath = gamePath.GetParentPath();
                        }

                        var collectionName = data.Name ?? $"Collection {data.IgdbId}";

                        newCollections.Add(new GameCollection208
                        {
                            IgdbId = data.IgdbId,
                            Title = collectionName,
                            CleanTitle = collectionName.CleanGameTitle(),
                            SortTitle = Parser.Parser.NormalizeTitle(collectionName),
                            Added = added,
                            QualityProfileId = qualityProfileId,
                            RootFolderPath = rootFolderPath.TrimEnd('/', '\\', ' '),
                            SearchOnAdd = true,
                            MinimumAvailability = minimumAvailability
                        });
                    }
                }
            }

            var updateSql = "INSERT INTO \"Collections\" (\"IgdbId\", \"Title\", \"CleanTitle\", \"SortTitle\", \"Added\", \"QualityProfileId\", \"RootFolderPath\", \"SearchOnAdd\", \"MinimumAvailability\") VALUES (@IgdbId, @Title, @CleanTitle, @SortTitle, @Added, @QualityProfileId, @RootFolderPath, @SearchOnAdd, @MinimumAvailability)";
            conn.Execute(updateSql, newCollections, transaction: tran);
        }

        private void MigrateCollectionMonitorStatus(IDbConnection conn, IDbTransaction tran)
        {
            var updatedCollections = new List<GameCollection208>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Enabled\", \"EnableAuto\", \"Settings\", \"ShouldMonitor\", \"Id\" FROM \"ImportLists\" WHERE \"Implementation\" = 'TMDbCollectionImport'";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var enabled = reader.GetBoolean(0);
                        var enabledAutoAdd = reader.GetBoolean(1);
                        var settings = reader.GetString(2);
                        var shouldMonitor = reader.GetBoolean(3);
                        var listId = reader.GetInt32(4);
                        var data = STJson.Deserialize<IgdbCollectionSettings206>(settings);

                        if (!enabled || !enabledAutoAdd || !int.TryParse(data.CollectionId, out var collectionId))
                        {
                            continue;
                        }

                        updatedCollections.Add(new GameCollection208
                        {
                            IgdbId = collectionId,
                            Monitored = true
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"Collections\" SET \"Monitored\" = @Monitored WHERE \"IgdbId\" = @IgdbId";
            conn.Execute(updateSql, updatedCollections, transaction: tran);
        }

        private void MigrateListMonitor(IDbConnection conn, IDbTransaction tran)
        {
            var updatedLists = new List<ImportList208>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"ShouldMonitor\", \"Id\" FROM \"ImportLists\"";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var shouldMonitor = reader.GetBoolean(0);
                        var listId = reader.GetInt32(1);

                        updatedLists.Add(new ImportList208
                        {
                            Monitor = shouldMonitor ? 0 : 2,
                            Id = listId
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"ImportLists\" SET \"Monitor\" = @Monitor WHERE \"Id\" = @Id";
            conn.Execute(updateSql, updatedLists, transaction: tran);
        }

        private void MapCollections(IDbConnection conn, IDbTransaction tran)
        {
            var updatedMeta = new List<GameMetadata208>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Collection\" FROM \"GameMetadata\" WHERE \"Collection\" IS NOT NULL";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var collection = reader.GetString(1);
                        var data = STJson.Deserialize<GameCollection207>(collection);

                        var collectionId = data.IgdbId;
                        var collectionTitle = data.Name;

                        updatedMeta.Add(new GameMetadata208
                        {
                            CollectionTitle = collectionTitle,
                            CollectionIgdbId = collectionId,
                            Id = id
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"GameMetadata\" SET \"CollectionIgdbId\" = @CollectionIgdbId, \"CollectionTitle\" = @CollectionTitle WHERE \"Id\" = @Id";
            conn.Execute(updateSql, updatedMeta, transaction: tran);
        }

        private class GameCollection207
        {
            public string Name { get; set; }
            public int IgdbId { get; set; }
        }

        private class GameCollection208
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string CleanTitle { get; set; }
            public string SortTitle { get; set; }
            public DateTime Added { get; set; }
            public int QualityProfileId { get; set; }
            public string RootFolderPath { get; set; }
            public bool SearchOnAdd { get; set; }
            public int MinimumAvailability { get; set; }
            public bool Monitored { get; set; }
            public int IgdbId { get; set; }
        }

        private class GameMetadata208
        {
            public int Id { get; set; }
            public int CollectionIgdbId { get; set; }
            public string CollectionTitle { get; set; }
        }

        private class ImportList208
        {
            public int Id { get; set; }
            public int Monitor { get; set; }
        }

        private class IgdbCollectionSettings206
        {
            public string CollectionId { get; set; }
        }
    }
}
