using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(207)]
    public class game_metadata : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("GameMetadata")
                .WithColumn("IgdbId").AsInt32().Unique()
                .WithColumn("ImdbId").AsString().Nullable()
                .WithColumn("Images").AsString()
                .WithColumn("Genres").AsString().Nullable()
                .WithColumn("Title").AsString()
                .WithColumn("SortTitle").AsString().Nullable()
                .WithColumn("CleanTitle").AsString().Nullable().Indexed()
                .WithColumn("OriginalTitle").AsString().Nullable()
                .WithColumn("CleanOriginalTitle").AsString().Nullable().Indexed()
                .WithColumn("OriginalLanguage").AsInt32()
                .WithColumn("Status").AsInt32()
                .WithColumn("LastInfoSync").AsDateTime().Nullable()
                .WithColumn("Runtime").AsInt32()
                .WithColumn("InDevelopment").AsDateTime().Nullable()
                .WithColumn("PhysicalRelease").AsDateTime().Nullable()
                .WithColumn("DigitalRelease").AsDateTime().Nullable()
                .WithColumn("Year").AsInt32().Nullable()
                .WithColumn("SecondaryYear").AsInt32().Nullable()
                .WithColumn("Ratings").AsString().Nullable()
                .WithColumn("Recommendations").AsString()
                .WithColumn("Certification").AsString().Nullable()
                .WithColumn("YouTubeTrailerId").AsString().Nullable()
                .WithColumn("Collection").AsString().Nullable()
                .WithColumn("Studio").AsString().Nullable()
                .WithColumn("Overview").AsString().Nullable()
                .WithColumn("Website").AsString().Nullable()
                .WithColumn("Popularity").AsFloat().Nullable();

            // Transfer metadata from Games to GameMetadata
            Execute.Sql(@"INSERT INTO ""GameMetadata"" (""IgdbId"", ""ImdbId"", ""Title"", ""SortTitle"", ""CleanTitle"", ""OriginalTitle"", ""CleanOriginalTitle"", ""OriginalLanguage"", ""Overview"", ""Status"", ""LastInfoSync"", ""Images"", ""Genres"", ""Ratings"", ""Runtime"", ""InDevelopment"", ""PhysicalRelease"", ""DigitalRelease"", ""Year"", ""SecondaryYear"", ""Recommendations"", ""Certification"", ""YouTubeTrailerId"", ""Studio"", ""Collection"", ""Website"")
                          SELECT ""IgdbId"", ""ImdbId"", ""Title"", ""SortTitle"", ""CleanTitle"", ""OriginalTitle"", ""CleanTitle"", ""OriginalLanguage"", ""Overview"", ""Status"", ""LastInfoSync"", ""Images"", ""Genres"", ""Ratings"", ""Runtime"", ""InDevelopment"", ""PhysicalRelease"", ""DigitalRelease"", ""Year"", ""SecondaryYear"", ""Recommendations"", ""Certification"", ""YouTubeTrailerId"", ""Studio"", ""Collection"", ""Website""
                          FROM ""Games""");

            // Transfer metadata from ImportListGames to GameMetadata if not already in
            Execute.Sql(@"INSERT INTO ""GameMetadata"" (""IgdbId"", ""ImdbId"", ""Title"", ""SortTitle"", ""CleanTitle"", ""OriginalTitle"", ""CleanOriginalTitle"", ""OriginalLanguage"", ""Overview"", ""Status"", ""LastInfoSync"", ""Images"", ""Genres"", ""Ratings"", ""Runtime"", ""InDevelopment"", ""PhysicalRelease"", ""DigitalRelease"", ""Year"", ""Recommendations"", ""Certification"", ""YouTubeTrailerId"", ""Studio"", ""Collection"", ""Website"")
                          SELECT ""IgdbId"", ""ImdbId"", ""Title"", ""SortTitle"", ""Title"", ""OriginalTitle"", ""OriginalTitle"", 1, ""Overview"", ""Status"", ""LastInfoSync"", ""Images"", ""Genres"", ""Ratings"", ""Runtime"", ""InDevelopment"", ""PhysicalRelease"", ""DigitalRelease"", ""Year"", '[]', ""Certification"", ""YouTubeTrailerId"", ""Studio"", ""Collection"", ""Website""
                          FROM ""ImportListGames""
                          WHERE ""ImportListGames"".""IgdbId"" NOT IN ( SELECT ""GameMetadata"".""IgdbId"" FROM ""GameMetadata"" )
                          AND ""ImportListGames"".""Id"" IN ( SELECT MIN(""Id"") FROM ""ImportListGames"" GROUP BY ""IgdbId"" )");

            // Add an GameMetadataId column to Games
            Alter.Table("Games").AddColumn("GameMetadataId").AsInt32().WithDefaultValue(0);
            Alter.Table("AlternativeTitles").AddColumn("GameMetadataId").AsInt32().WithDefaultValue(0);
            Alter.Table("Credits").AddColumn("GameMetadataId").AsInt32().WithDefaultValue(0);
            Alter.Table("GameTranslations").AddColumn("GameMetadataId").AsInt32().WithDefaultValue(0);
            Alter.Table("ImportListGames").AddColumn("GameMetadataId").AsInt32().WithDefaultValue(0).Indexed();

            // Update GameMetadataId
            Execute.Sql(@"UPDATE ""Games""
                          SET ""GameMetadataId"" = (SELECT ""GameMetadata"".""Id"" 
                                                  FROM ""GameMetadata""
                                                  WHERE ""GameMetadata"".""IgdbId"" = ""Games"".""IgdbId"")");

            Execute.Sql(@"UPDATE ""AlternativeTitles""
                          SET ""GameMetadataId"" = (SELECT ""Games"".""GameMetadataId"" 
                                                  FROM ""Games"" 
                                                  WHERE ""Games"".""Id"" = ""AlternativeTitles"".""GameId"")");

            Execute.Sql(@"UPDATE ""Credits""
                          SET ""GameMetadataId"" = (SELECT ""Games"".""GameMetadataId"" 
                                                  FROM ""Games"" 
                                                  WHERE ""Games"".""Id"" = ""Credits"".""GameId"")");

            Execute.Sql(@"UPDATE ""GameTranslations""
                          SET ""GameMetadataId"" = (SELECT ""Games"".""GameMetadataId"" 
                                                  FROM ""Games"" 
                                                  WHERE ""Games"".""Id"" = ""GameTranslations"".""GameId"")");

            Execute.Sql(@"UPDATE ""ImportListGames""
                          SET ""GameMetadataId"" = (SELECT ""GameMetadata"".""Id"" 
                                                  FROM ""GameMetadata"" 
                                                  WHERE ""GameMetadata"".""IgdbId"" = ""ImportListGames"".""IgdbId"")");

            // Alter GameMetadataId column to be unique on Games
            Alter.Table("Games").AlterColumn("GameMetadataId").AsInt32().Unique();

            // Remove Game Link from Metadata Tables
            Delete.Column("GameId").FromTable("AlternativeTitles");
            Delete.Column("GameId").FromTable("Credits");
            Delete.Column("GameId").FromTable("GameTranslations");

            // Remove the columns in Games now in GameMetadata
            Delete.Column("IgdbId")
                .Column("ImdbId")
                .Column("Title")
                .Column("SortTitle")
                .Column("CleanTitle")
                .Column("OriginalTitle")
                .Column("OriginalLanguage")
                .Column("Overview")
                .Column("Status")
                .Column("LastInfoSync")
                .Column("Images")
                .Column("Genres")
                .Column("Ratings")
                .Column("Runtime")
                .Column("InDevelopment")
                .Column("PhysicalRelease")
                .Column("DigitalRelease")
                .Column("Year")
                .Column("SecondaryYear")
                .Column("Recommendations")
                .Column("Certification")
                .Column("YouTubeTrailerId")
                .Column("Studio")
                .Column("Collection")
                .Column("Website")

                // as well as the ones no longer used
                .Column("LastDiskSync")
                .Column("TitleSlug")
                .FromTable("Games");

            // Remove the columns in ImportListGames now in GameMetadata
            Delete.Column("IgdbId")
                .Column("ImdbId")
                .Column("Title")
                .Column("SortTitle")
                .Column("Overview")
                .Column("Status")
                .Column("LastInfoSync")
                .Column("OriginalTitle")
                .Column("Translations")
                .Column("Images")
                .Column("Genres")
                .Column("Ratings")
                .Column("Runtime")
                .Column("InDevelopment")
                .Column("PhysicalRelease")
                .Column("DigitalRelease")
                .Column("Year")
                .Column("Certification")
                .Column("YouTubeTrailerId")
                .Column("Studio")
                .Column("Collection")
                .Column("Website")
                .FromTable("ImportListGames");
        }
    }
}
