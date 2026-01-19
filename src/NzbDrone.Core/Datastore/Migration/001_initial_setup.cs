using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1)]
    public class InitialSetup : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // ===========================================
            // CONFIGURATION & SETTINGS
            // ===========================================

            Create.TableForModel("Config")
                .WithColumn("Key").AsString().Unique()
                .WithColumn("Value").AsString();

            Create.TableForModel("RootFolders")
                .WithColumn("Path").AsString().Unique();

            Create.TableForModel("Tags")
                .WithColumn("Label").AsString().Unique();

            Create.TableForModel("NamingConfig")
                .WithColumn("RenameGames").AsBoolean().Nullable()
                .WithColumn("ReplaceIllegalCharacters").AsBoolean().WithDefaultValue(true)
                .WithColumn("ColonReplacementFormat").AsInt32().WithDefaultValue(0)
                .WithColumn("StandardGameFormat").AsString().Nullable()
                .WithColumn("GameFolderFormat").AsString().Nullable();

            // ===========================================
            // QUALITY PROFILES & DEFINITIONS
            // ===========================================

            Create.TableForModel("QualityDefinitions")
                .WithColumn("Quality").AsInt32().Unique()
                .WithColumn("Title").AsString().Unique()
                .WithColumn("MinSize").AsDouble().Nullable()
                .WithColumn("MaxSize").AsDouble().Nullable()
                .WithColumn("PreferredSize").AsDouble().Nullable();

            Create.TableForModel("QualityProfiles")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Cutoff").AsInt32()
                .WithColumn("Items").AsString().NotNullable()
                .WithColumn("Language").AsInt32().Nullable()
                .WithColumn("FormatItems").AsString().NotNullable()
                .WithColumn("UpgradeAllowed").AsBoolean().Nullable()
                .WithColumn("MinFormatScore").AsInt32().NotNullable()
                .WithColumn("CutoffFormatScore").AsInt32().NotNullable()
                .WithColumn("MinUpgradeFormatScore").AsInt32().WithDefaultValue(1);

            Create.TableForModel("DelayProfiles")
                .WithColumn("EnableUsenet").AsBoolean().NotNullable()
                .WithColumn("EnableTorrent").AsBoolean().NotNullable()
                .WithColumn("PreferredProtocol").AsInt32().NotNullable()
                .WithColumn("UsenetDelay").AsInt32().NotNullable()
                .WithColumn("TorrentDelay").AsInt32().NotNullable()
                .WithColumn("Order").AsInt32().NotNullable()
                .WithColumn("Tags").AsString().NotNullable()
                .WithColumn("BypassIfHighestQuality").AsBoolean().WithDefaultValue(false)
                .WithColumn("BypassIfAboveCustomFormatScore").AsBoolean().WithDefaultValue(false)
                .WithColumn("MinimumCustomFormatScore").AsInt32().Nullable();

            Insert.IntoTable("DelayProfiles").Row(new
            {
                EnableUsenet = true,
                EnableTorrent = true,
                PreferredProtocol = 1,
                UsenetDelay = 0,
                TorrentDelay = 0,
                Order = int.MaxValue,
                Tags = "[]",
                BypassIfHighestQuality = false,
                BypassIfAboveCustomFormatScore = false
            });

            Create.TableForModel("ReleaseProfiles")
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Enabled").AsBoolean().WithDefaultValue(true)
                .WithColumn("Required").AsString().Nullable()
                .WithColumn("Ignored").AsString().Nullable()
                .WithColumn("Tags").AsString().NotNullable()
                .WithColumn("IndexerId").AsInt32().WithDefaultValue(0);

            Create.TableForModel("CustomFormats")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Specifications").AsString().WithDefaultValue("[]")
                .WithColumn("IncludeCustomFormatWhenRenaming").AsBoolean().WithDefaultValue(false);

            // ===========================================
            // GAME METADATA & CORE ENTITIES
            // ===========================================

            Create.TableForModel("GameMetadata")
                .WithColumn("SteamAppId").AsInt32().WithDefaultValue(0).Indexed()
                .WithColumn("IgdbId").AsInt32().Indexed()
                .WithColumn("RawgId").AsInt32().WithDefaultValue(0).Indexed()
                .WithColumn("Title").AsString()
                .WithColumn("CleanTitle").AsString().Nullable().Indexed()
                .WithColumn("SortTitle").AsString().Nullable()
                .WithColumn("OriginalTitle").AsString().Nullable()
                .WithColumn("CleanOriginalTitle").AsString().Nullable().Indexed()
                .WithColumn("OriginalLanguage").AsInt32()
                .WithColumn("Status").AsInt32()
                .WithColumn("Overview").AsString().Nullable()
                .WithColumn("Images").AsString()
                .WithColumn("Genres").AsString().Nullable()
                .WithColumn("Keywords").AsString().Nullable()
                .WithColumn("Ratings").AsString().Nullable()
                .WithColumn("Runtime").AsInt32()
                .WithColumn("EarlyAccess").AsDateTime().Nullable()
                .WithColumn("PhysicalRelease").AsDateTime().Nullable()
                .WithColumn("DigitalRelease").AsDateTime().Nullable()
                .WithColumn("Year").AsInt32().Nullable()
                .WithColumn("SecondaryYear").AsInt32().Nullable()
                .WithColumn("Certification").AsString().Nullable()
                .WithColumn("YouTubeTrailerId").AsString().Nullable()
                .WithColumn("Studio").AsString().Nullable()
                .WithColumn("Website").AsString().Nullable()
                .WithColumn("Popularity").AsFloat().Nullable()
                .WithColumn("LastInfoSync").AsDateTime().Nullable()
                .WithColumn("Recommendations").AsString().WithDefaultValue("[]")
                .WithColumn("CollectionIgdbId").AsInt32().WithDefaultValue(0).Indexed()
                .WithColumn("CollectionTitle").AsString().Nullable()
                .WithColumn("GameType").AsInt32().WithDefaultValue(0)
                .WithColumn("ParentGameId").AsInt32().Nullable()
                .WithColumn("DlcIds").AsString().Nullable()
                .WithColumn("Platforms").AsString().Nullable()
                .WithColumn("GameModes").AsString().Nullable()
                .WithColumn("Themes").AsString().Nullable()
                .WithColumn("Developer").AsString().Nullable()
                .WithColumn("Publisher").AsString().Nullable()
                .WithColumn("GameEngine").AsString().Nullable()
                .WithColumn("AggregatedRating").AsDouble().Nullable()
                .WithColumn("AggregatedRatingCount").AsInt32().Nullable();

            Create.TableForModel("Games")
                .WithColumn("GameMetadataId").AsInt32().Unique()
                .WithColumn("Path").AsString()
                .WithColumn("Monitored").AsBoolean()
                .WithColumn("MinimumAvailability").AsInt32()
                .WithColumn("QualityProfileId").AsInt32()
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("Added").AsDateTime().Nullable()
                .WithColumn("AddOptions").AsString().Nullable()
                .WithColumn("GameFileId").AsInt32().WithDefaultValue(0)
                .WithColumn("LastSearchTime").AsDateTime().Nullable();

            Create.TableForModel("GameFiles")
                .WithColumn("GameId").AsInt32().Indexed()
                .WithColumn("Quality").AsString().NotNullable()
                .WithColumn("Size").AsInt64().NotNullable()
                .WithColumn("DateAdded").AsDateTime().NotNullable()
                .WithColumn("SceneName").AsString().Nullable()
                .WithColumn("MediaInfo").AsString().Nullable()
                .WithColumn("ReleaseGroup").AsString().Nullable()
                .WithColumn("RelativePath").AsString().Nullable()
                .WithColumn("Edition").AsString().Nullable()
                .WithColumn("Languages").AsString().NotNullable()
                .WithColumn("IndexerFlags").AsInt32().NotNullable()
                .WithColumn("OriginalFilePath").AsString().Nullable();

            Create.TableForModel("AlternativeTitles")
                .WithColumn("Title").AsString().NotNullable()
                .WithColumn("CleanTitle").AsString().NotNullable().Unique()
                .WithColumn("SourceType").AsInt32().NotNullable()
                .WithColumn("GameMetadataId").AsInt32().NotNullable().Indexed();

            Create.Index().OnTable("AlternativeTitles").OnColumn("CleanTitle").Ascending();

            Create.TableForModel("GameTranslations")
                .WithColumn("Title").AsString().Nullable()
                .WithColumn("CleanTitle").AsString().Nullable().Indexed()
                .WithColumn("Overview").AsString().Nullable()
                .WithColumn("Language").AsInt32().NotNullable()
                .WithColumn("GameMetadataId").AsInt32().NotNullable().Indexed();

            Create.Index().OnTable("GameTranslations").OnColumn("Language").Ascending();

            Create.TableForModel("Credits")
                .WithColumn("CreditIgdbId").AsString().NotNullable().Unique()
                .WithColumn("PersonIgdbId").AsInt32().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Images").AsString().NotNullable()
                .WithColumn("Character").AsString().Nullable()
                .WithColumn("Order").AsInt32().NotNullable()
                .WithColumn("Job").AsString().Nullable()
                .WithColumn("Department").AsString().Nullable()
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("GameMetadataId").AsInt32().NotNullable().Indexed();

            Create.TableForModel("Collections")
                .WithColumn("IgdbId").AsInt32().NotNullable().Unique()
                .WithColumn("QualityProfileId").AsInt32().NotNullable()
                .WithColumn("RootFolderPath").AsString().NotNullable()
                .WithColumn("MinimumAvailability").AsInt32().NotNullable()
                .WithColumn("SearchOnAdd").AsBoolean().NotNullable()
                .WithColumn("Title").AsString().NotNullable()
                .WithColumn("SortTitle").AsString().Nullable()
                .WithColumn("CleanTitle").AsString().NotNullable()
                .WithColumn("Overview").AsString().Nullable()
                .WithColumn("Images").AsString().NotNullable()
                .WithColumn("Monitored").AsBoolean().NotNullable()
                .WithColumn("LastInfoSync").AsDateTime().Nullable()
                .WithColumn("Added").AsDateTime().Nullable()
                .WithColumn("Tags").AsString().Nullable();

            // ===========================================
            // HISTORY & TRACKING
            // ===========================================

            Create.TableForModel("History")
                .WithColumn("GameId").AsInt32().Indexed()
                .WithColumn("SourceTitle").AsString()
                .WithColumn("Date").AsDateTime().Indexed()
                .WithColumn("Quality").AsString()
                .WithColumn("Data").AsString()
                .WithColumn("EventType").AsInt32().Nullable()
                .WithColumn("DownloadId").AsString().Nullable().Indexed()
                .WithColumn("Languages").AsString().WithDefaultValue("[]");

            Create.TableForModel("Blocklist")
                .WithColumn("GameId").AsInt32().Indexed()
                .WithColumn("SourceTitle").AsString()
                .WithColumn("Quality").AsString()
                .WithColumn("Date").AsDateTime()
                .WithColumn("PublishedDate").AsDateTime().Nullable()
                .WithColumn("Size").AsInt64().Nullable()
                .WithColumn("Protocol").AsInt32().Nullable()
                .WithColumn("Indexer").AsString().Nullable()
                .WithColumn("Message").AsString().Nullable()
                .WithColumn("TorrentInfoHash").AsString().Nullable()
                .WithColumn("Languages").AsString().WithDefaultValue("[]")
                .WithColumn("IndexerFlags").AsInt32().WithDefaultValue(0);

            Create.TableForModel("DownloadHistory")
                .WithColumn("EventType").AsInt32().NotNullable()
                .WithColumn("GameId").AsInt32().NotNullable().Indexed()
                .WithColumn("DownloadId").AsString().NotNullable().Indexed()
                .WithColumn("SourceTitle").AsString().NotNullable()
                .WithColumn("Date").AsDateTime().NotNullable().Indexed()
                .WithColumn("Protocol").AsInt32().Nullable()
                .WithColumn("IndexerId").AsInt32().Nullable()
                .WithColumn("DownloadClientId").AsInt32().Nullable()
                .WithColumn("Release").AsString().Nullable()
                .WithColumn("Data").AsString().Nullable();

            // ===========================================
            // IMPORT LISTS & EXCLUSIONS
            // ===========================================

            Create.TableForModel("ImportLists")
                .WithColumn("Enabled").AsBoolean()
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Implementation").AsString()
                .WithColumn("ConfigContract").AsString().Nullable()
                .WithColumn("Settings").AsString().Nullable()
                .WithColumn("EnableAuto").AsBoolean()
                .WithColumn("RootFolderPath").AsString()
                .WithColumn("QualityProfileId").AsInt32()
                .WithColumn("MinimumAvailability").AsInt32()
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("SearchOnAdd").AsBoolean().WithDefaultValue(true)
                .WithColumn("Monitor").AsInt32().WithDefaultValue(0);

            Create.TableForModel("ImportListStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable()
                .WithColumn("LastInfoSync").AsDateTime().Nullable();

            Create.TableForModel("ImportListGames")
                .WithColumn("ListId").AsInt32()
                .WithColumn("GameMetadataId").AsInt32().Indexed();

            Create.TableForModel("ImportExclusions")
                .WithColumn("IgdbId").AsInt32().NotNullable().Unique()
                .WithColumn("GameTitle").AsString().Nullable()
                .WithColumn("GameYear").AsInt32().WithDefaultValue(0)
                .WithColumn("SteamAppId").AsInt32().WithDefaultValue(0);

            // ===========================================
            // PROVIDERS (INDEXERS, DOWNLOAD CLIENTS, ETC)
            // ===========================================

            Create.TableForModel("Indexers")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Implementation").AsString()
                .WithColumn("Settings").AsString().Nullable()
                .WithColumn("ConfigContract").AsString().Nullable()
                .WithColumn("EnableRss").AsBoolean().Nullable()
                .WithColumn("EnableAutomaticSearch").AsBoolean().Nullable()
                .WithColumn("EnableInteractiveSearch").AsBoolean().WithDefaultValue(true)
                .WithColumn("Priority").AsInt32().WithDefaultValue(25)
                .WithColumn("Tags").AsString().WithDefaultValue("[]")
                .WithColumn("DownloadClientId").AsInt32().WithDefaultValue(0);

            Create.TableForModel("IndexerStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable()
                .WithColumn("LastRssSyncReleaseInfo").AsString().Nullable()
                .WithColumn("Cookies").AsString().Nullable()
                .WithColumn("CookiesExpirationDate").AsDateTime().Nullable();

            Create.TableForModel("DownloadClients")
                .WithColumn("Enable").AsBoolean().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Implementation").AsString().NotNullable()
                .WithColumn("Settings").AsString().NotNullable()
                .WithColumn("ConfigContract").AsString().NotNullable()
                .WithColumn("Priority").AsInt32().WithDefaultValue(1)
                .WithColumn("RemoveCompletedDownloads").AsBoolean().WithDefaultValue(true)
                .WithColumn("RemoveFailedDownloads").AsBoolean().WithDefaultValue(true)
                .WithColumn("Tags").AsString().WithDefaultValue("[]");

            Create.TableForModel("DownloadClientStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable();

            Create.TableForModel("Notifications")
                .WithColumn("Name").AsString()
                .WithColumn("OnGrab").AsBoolean()
                .WithColumn("OnDownload").AsBoolean()
                .WithColumn("Settings").AsString()
                .WithColumn("Implementation").AsString()
                .WithColumn("ConfigContract").AsString().Nullable()
                .WithColumn("OnUpgrade").AsBoolean().Nullable()
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("OnRename").AsBoolean().NotNullable()
                .WithColumn("OnGameAdded").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnGameDelete").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnGameFileDelete").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnGameFileDeleteForUpgrade").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnHealthIssue").AsBoolean().WithDefaultValue(false)
                .WithColumn("IncludeHealthWarnings").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnHealthRestored").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnApplicationUpdate").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnManualInteractionRequired").AsBoolean().WithDefaultValue(false);

            Create.TableForModel("NotificationStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable();

            Create.TableForModel("Metadata")
                .WithColumn("Enable").AsBoolean().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Implementation").AsString().NotNullable()
                .WithColumn("Settings").AsString().NotNullable()
                .WithColumn("ConfigContract").AsString().NotNullable();

            // ===========================================
            // PENDING & QUEUE
            // ===========================================

            Create.TableForModel("PendingReleases")
                .WithColumn("GameId").AsInt32().WithDefaultValue(0)
                .WithColumn("Title").AsString()
                .WithColumn("Added").AsDateTime()
                .WithColumn("ParsedGameInfo").AsString().Nullable()
                .WithColumn("Release").AsString()
                .WithColumn("Reason").AsInt32().WithDefaultValue(0)
                .WithColumn("AdditionalInfo").AsString().Nullable();

            // ===========================================
            // EXTRA FILES & METADATA FILES
            // ===========================================

            Create.TableForModel("MetadataFiles")
                .WithColumn("GameId").AsInt32().NotNullable().Indexed()
                .WithColumn("Consumer").AsString().NotNullable()
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("LastUpdated").AsDateTime().NotNullable()
                .WithColumn("GameFileId").AsInt32().Nullable()
                .WithColumn("Added").AsDateTime().Nullable()
                .WithColumn("Extension").AsString().NotNullable()
                .WithColumn("Hash").AsString().Nullable();

            Create.TableForModel("SubtitleFiles")
                .WithColumn("GameId").AsInt32().NotNullable()
                .WithColumn("GameFileId").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("Extension").AsString().NotNullable()
                .WithColumn("Added").AsDateTime().NotNullable()
                .WithColumn("LastUpdated").AsDateTime().NotNullable()
                .WithColumn("Language").AsInt32().NotNullable()
                .WithColumn("LanguageTags").AsString().Nullable()
                .WithColumn("Title").AsString().Nullable()
                .WithColumn("Copy").AsInt32().WithDefaultValue(0);

            Create.TableForModel("ExtraFiles")
                .WithColumn("GameId").AsInt32().NotNullable()
                .WithColumn("GameFileId").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("Extension").AsString().NotNullable()
                .WithColumn("Added").AsDateTime().NotNullable()
                .WithColumn("LastUpdated").AsDateTime().NotNullable();

            // ===========================================
            // SYSTEM & SCHEDULING
            // ===========================================

            Create.TableForModel("ScheduledTasks")
                .WithColumn("TypeName").AsString().Unique()
                .WithColumn("Interval").AsInt32()
                .WithColumn("LastExecution").AsDateTime()
                .WithColumn("LastStartTime").AsDateTime().Nullable();

            Create.TableForModel("Commands")
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Body").AsString().NotNullable()
                .WithColumn("Priority").AsInt32().NotNullable()
                .WithColumn("Status").AsInt32().NotNullable()
                .WithColumn("QueuedAt").AsDateTime().NotNullable()
                .WithColumn("StartedAt").AsDateTime().Nullable()
                .WithColumn("EndedAt").AsDateTime().Nullable()
                .WithColumn("Duration").AsString().Nullable()
                .WithColumn("Exception").AsString().Nullable()
                .WithColumn("Trigger").AsInt32().NotNullable()
                .WithColumn("Result").AsInt32().WithDefaultValue(1);

            Create.TableForModel("Users")
                .WithColumn("Identifier").AsString().NotNullable().Unique()
                .WithColumn("Username").AsString().NotNullable().Unique()
                .WithColumn("Password").AsString().NotNullable()
                .WithColumn("Salt").AsString().Nullable()
                .WithColumn("Iterations").AsInt32().Nullable();

            Create.TableForModel("RemotePathMappings")
                .WithColumn("Host").AsString()
                .WithColumn("RemotePath").AsString()
                .WithColumn("LocalPath").AsString();

            // ===========================================
            // CUSTOM FILTERS & AUTO-TAGGING
            // ===========================================

            Create.TableForModel("CustomFilters")
                .WithColumn("Type").AsString().NotNullable()
                .WithColumn("Label").AsString().NotNullable()
                .WithColumn("Filters").AsString().NotNullable();

            Create.TableForModel("AutoTagging")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Specifications").AsString().WithDefaultValue("[]")
                .WithColumn("RemoveTagsAutomatically").AsBoolean().WithDefaultValue(false)
                .WithColumn("Tags").AsString().WithDefaultValue("[]");
        }

        protected override void LogDbUpgrade()
        {
            Create.TableForModel("Logs")
                .WithColumn("Message").AsString()
                .WithColumn("Time").AsDateTime().Indexed()
                .WithColumn("Logger").AsString()
                .WithColumn("Exception").AsString().Nullable()
                .WithColumn("ExceptionType").AsString().Nullable()
                .WithColumn("Level").AsString();

            Create.TableForModel("UpdateHistory")
                .WithColumn("Date").AsDateTime().NotNullable()
                .WithColumn("Version").AsString().NotNullable()
                .WithColumn("EventType").AsInt32().NotNullable();
        }
    }
}
