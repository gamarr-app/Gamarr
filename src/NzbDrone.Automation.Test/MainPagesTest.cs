using Microsoft.Playwright;
using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    [TestFixture]
    public class MainPagesTest : AutomationTest
    {
        [Test]
        public async Task Games_Index_Page_Loads_Without_Errors()
        {
            await ClickNavLinkAsync("Games");

            await Expect(Page.Locator("div[class*='GameIndex']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("games_index");
        }

        [Test]
        public async Task Calendar_Page_Loads_Without_Errors()
        {
            await ClickNavLinkAsync("Calendar");

            await Expect(Page.Locator("div[class*='CalendarPage']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("calendar");
        }

        [Test]
        public async Task Activity_Queue_Page_Loads_Without_Errors()
        {
            await ClickNavLinkAsync("Activity");

            await Expect(Page.GetByRole(AriaRole.Link, new () { Name = "Queue" }).First).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Link, new () { Name = "History" }).First).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Link, new () { Name = "Blocklist" }).First).ToBeVisibleAsync();
            await TakeScreenshotAsync("activity_queue");
        }

        [Test]
        public async Task Activity_History_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/activity/history");

            await Expect(Page.Locator("div[class*='History']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("activity_history");
        }

        [Test]
        public async Task Activity_Blocklist_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/activity/blocklist");

            await Expect(Page.Locator("div[class*='Blocklist']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("activity_blocklist");
        }

        [Test]
        public async Task Wanted_Missing_Page_Loads_Without_Errors()
        {
            await ClickNavLinkAsync("Wanted");

            await Expect(Page.GetByRole(AriaRole.Link, new () { Name = "Missing" }).First).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Link, new () { Name = "Cutoff Unmet" }).First).ToBeVisibleAsync();
            await TakeScreenshotAsync("wanted_missing");
        }

        [Test]
        public async Task Wanted_CutoffUnmet_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/wanted/cutoffunmet");

            await Expect(Page.Locator("div[class*='CutoffUnmet']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("wanted_cutoffunmet");
        }

        [Test]
        public async Task System_Status_Page_Loads_Without_Errors()
        {
            await ClickNavLinkAsync("System");

            await Expect(Page.Locator("div[class*='Health']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("system_status");
        }

        [Test]
        public async Task Add_New_Game_Page_Loads_Without_Errors()
        {
            await ClickNavLinkAsync("Games");
            await Page.GetByRole(AriaRole.Link, new () { Name = "Add New" }).First.ClickAsync();
            await WaitForNoSpinner();

            await Expect(Page.Locator("input[class*='searchInput']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("add_new_game");
        }

        [Test]
        public async Task Import_Games_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/add/import");

            await Expect(Page.Locator("div[class*='ImportGame']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("import_games");
        }

        [Test]
        public async Task Discover_Games_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/add/discover");

            await Expect(Page.Locator("div[class*='DiscoverGame']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("discover_games");
        }

        [Test]
        public async Task Collections_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/collections");

            // Collections page should load - may show empty state or collections
            await WaitForNoSpinner();
            await TakeScreenshotAsync("collections");
        }

        [Test]
        public async Task Settings_Main_Page_Loads_Without_Errors()
        {
            await ClickNavLinkAsync("Settings");

            await Expect(Page.Locator("div[class*='Settings']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_main");
        }

        [Test]
        public async Task Settings_MediaManagement_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/mediamanagement");

            await Expect(Page.Locator("div[class*='MediaManagement']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_mediamanagement");
        }

        [Test]
        public async Task Settings_Profiles_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/profiles");

            await Expect(Page.Locator("div[class*='Profiles']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_profiles");
        }

        [Test]
        public async Task Settings_Quality_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/quality");

            await Expect(Page.Locator("div[class*='Quality']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_quality");
        }

        [Test]
        public async Task Settings_CustomFormats_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/customformats");

            await Expect(Page.Locator("div[class*='CustomFormats']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_customformats");
        }

        [Test]
        public async Task Settings_Indexers_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/indexers");

            await Expect(Page.Locator("div[class*='Indexers']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_indexers");
        }

        [Test]
        public async Task Settings_DownloadClients_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/downloadclients");

            await Expect(Page.Locator("div[class*='DownloadClients']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_downloadclients");
        }

        [Test]
        public async Task Settings_ImportLists_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/importlists");

            await Expect(Page.Locator("div[class*='ImportLists']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_importlists");
        }

        [Test]
        public async Task Settings_Connect_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/connect");

            await Expect(Page.Locator("div[class*='Notifications']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_connect");
        }

        [Test]
        public async Task Settings_Metadata_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/metadata");

            await Expect(Page.Locator("div[class*='Metadata']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_metadata");
        }

        [Test]
        public async Task Settings_Tags_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/tags");

            await Expect(Page.Locator("div[class*='TagSettings']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_tags");
        }

        [Test]
        public async Task Settings_General_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/general");

            await Expect(Page.Locator("div[class*='GeneralSettings']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_general");
        }

        [Test]
        public async Task Settings_UI_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/settings/ui");

            await Expect(Page.Locator("div[class*='UISettings']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("settings_ui");
        }

        [Test]
        public async Task System_Tasks_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/system/tasks");

            await Expect(Page.Locator("div[class*='Tasks']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("system_tasks");
        }

        [Test]
        public async Task System_Backup_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/system/backup");

            await Expect(Page.Locator("div[class*='Backups']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("system_backup");
        }

        [Test]
        public async Task System_Updates_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/system/updates");

            await Expect(Page.Locator("div[class*='Updates']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("system_updates");
        }

        [Test]
        public async Task System_Events_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/system/events");

            await Expect(Page.Locator("div[class*='LogsTable']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("system_events");
        }

        [Test]
        public async Task System_LogFiles_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/system/logs/files");

            await Expect(Page.Locator("div[class*='LogFiles']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("system_logfiles");
        }

        [Test]
        public async Task System_UpdateLogFiles_Page_Loads_Without_Errors()
        {
            await NavigateToAsync("/system/logs/files/update");

            await Expect(Page.Locator("div[class*='LogFiles']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("system_updatelogfiles");
        }
    }
}
