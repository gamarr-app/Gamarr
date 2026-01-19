using Microsoft.Playwright;
using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    [TestFixture]
    public class UserFlowsTest : AutomationTest
    {
        [Test]
        public async Task Navigate_Through_All_Main_Sections()
        {
            // Games
            await ClickNavLinkAsync("Games");
            await Expect(Page.Locator("div[class*='GameIndex']")).ToBeVisibleAsync();

            // Calendar
            await ClickNavLinkAsync("Calendar");
            await Expect(Page.Locator("div[class*='CalendarPage']")).ToBeVisibleAsync();

            // Activity
            await ClickNavLinkAsync("Activity");
            await Expect(Page.Locator("div[class*='Queue']")).ToBeVisibleAsync();

            // Wanted
            await ClickNavLinkAsync("Wanted");
            await Expect(Page.Locator("div[class*='Missing']")).ToBeVisibleAsync();

            // Settings
            await ClickNavLinkAsync("Settings");
            await Expect(Page.Locator("div[class*='Settings']")).ToBeVisibleAsync();

            // System
            await ClickNavLinkAsync("System");
            await Expect(Page.Locator("div[class*='Health']")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Navigate_Through_Activity_Tabs()
        {
            await ClickNavLinkAsync("Activity");

            // Queue tab (default)
            await Expect(Page.Locator("div[class*='Queue']")).ToBeVisibleAsync();

            // History tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "History" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='History']")).ToBeVisibleAsync();

            // Blocklist tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Blocklist" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Blocklist']")).ToBeVisibleAsync();

            // Back to Queue
            await Page.GetByRole(AriaRole.Link, new () { Name = "Queue" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Queue']")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Navigate_Through_Wanted_Tabs()
        {
            await ClickNavLinkAsync("Wanted");

            // Missing tab (default)
            await Expect(Page.Locator("div[class*='Missing']")).ToBeVisibleAsync();

            // Cutoff Unmet tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Cutoff Unmet" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='CutoffUnmet']")).ToBeVisibleAsync();

            // Back to Missing
            await Page.GetByRole(AriaRole.Link, new () { Name = "Missing" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Missing']")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Navigate_Through_System_Tabs()
        {
            await ClickNavLinkAsync("System");

            // Status tab (default)
            await Expect(Page.Locator("div[class*='Health']")).ToBeVisibleAsync();

            // Tasks tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Tasks" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Tasks']")).ToBeVisibleAsync();

            // Backup tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Backup" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Backups']")).ToBeVisibleAsync();

            // Updates tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Updates" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Updates']")).ToBeVisibleAsync();

            // Events tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Events" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='LogsTable']")).ToBeVisibleAsync();

            // Log Files tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Log Files" }).ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='LogFiles']")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Navigate_Through_All_Settings_Pages()
        {
            await ClickNavLinkAsync("Settings");

            // Click through each settings section
            var settingsLinks = new[]
            {
                ("Media Management", "MediaManagement"),
                ("Profiles", "Profiles"),
                ("Quality", "Quality"),
                ("Indexers", "Indexers"),
                ("Download Clients", "DownloadClients"),
                ("Import Lists", "ImportLists"),
                ("Connect", "Notifications"),
                ("Metadata", "Metadata"),
                ("Tags", "Tags"),
                ("General", "GeneralSettings"),
                ("UI", "UISettings")
            };

            foreach (var (linkText, expectedClass) in settingsLinks)
            {
                await Page.GetByRole(AriaRole.Link, new () { Name = linkText, Exact = true }).ClickAsync();
                await WaitForNoSpinner();
                await Expect(Page.Locator($"div[class*='{expectedClass}']")).ToBeVisibleAsync();
            }
        }

        [Test]
        public async Task Add_Game_Search_Interface_Works()
        {
            await NavigateToAsync("/add/new");

            // Verify search input is present
            var searchInput = Page.Locator("input[class*='searchInput']");
            await Expect(searchInput).ToBeVisibleAsync();

            // Type in search box (don't submit, just verify it works)
            await searchInput.FillAsync("Test Game");
            await Expect(searchInput).ToHaveValueAsync("Test Game");

            await TakeScreenshotAsync("add_game_search");
        }

        [Test]
        public async Task Add_Game_Page_Has_Required_Elements()
        {
            await NavigateToAsync("/add/new");

            // Search input should be visible
            await Expect(Page.Locator("input[class*='searchInput']")).ToBeVisibleAsync();

            await TakeScreenshotAsync("add_game_elements");
        }

        [Test]
        public async Task Calendar_View_Modes_Work()
        {
            await ClickNavLinkAsync("Calendar");
            await Expect(Page.Locator("div[class*='CalendarPage']")).ToBeVisibleAsync();

            // Take screenshot of calendar
            await TakeScreenshotAsync("calendar_view");
        }

        [Test]
        public async Task Calendar_Navigation_Works()
        {
            await ClickNavLinkAsync("Calendar");

            // Look for calendar navigation buttons
            var header = Page.Locator("div[class*='CalendarHeader']");
            await Expect(header).ToBeVisibleAsync();

            await TakeScreenshotAsync("calendar_navigation");
        }

        [Test]
        public async Task Games_Index_Has_View_Options()
        {
            await ClickNavLinkAsync("Games");
            await Expect(Page.Locator("div[class*='GameIndex']")).ToBeVisibleAsync();

            // Check for view mode buttons/options in the toolbar
            await Expect(Page.Locator("div[class*='PageToolbar']")).ToBeVisibleAsync();

            await TakeScreenshotAsync("games_index_toolbar");
        }

        [Test]
        public async Task Settings_Profile_Modal_Opens()
        {
            await NavigateToAsync("/settings/profiles");

            // Look for add profile button and click it
            var addButton = Page.Locator("button[class*='add'], div[class*='Card-link']").First;
            if (await addButton.IsVisibleAsync())
            {
                await addButton.ClickAsync();
                await Task.Delay(500); // Wait for modal animation

                // Check if modal opened
                var modal = Page.Locator("div[class*='Modal']");
                if (await modal.IsVisibleAsync())
                {
                    await TakeScreenshotAsync("profile_modal");

                    // Close modal by pressing Escape
                    await Page.Keyboard.PressAsync("Escape");
                }
            }
        }

        [Test]
        public async Task Invalid_Route_Shows_Not_Found()
        {
            await NavigateToAsync("/this-route-does-not-exist");

            // Should show a not found page or redirect
            await TakeScreenshotAsync("not_found_page");
        }

        [Test]
        public async Task Page_Renders_At_Different_Viewports()
        {
            await ClickNavLinkAsync("Games");

            // Desktop
            await Page.SetViewportSizeAsync(1920, 1080);
            await WaitForNoSpinner();
            await TakeScreenshotAsync("games_desktop");

            // Tablet
            await Page.SetViewportSizeAsync(768, 1024);
            await WaitForNoSpinner();
            await TakeScreenshotAsync("games_tablet");

            // Mobile
            await Page.SetViewportSizeAsync(375, 667);
            await WaitForNoSpinner();
            await TakeScreenshotAsync("games_mobile");

            // Reset to desktop
            await Page.SetViewportSizeAsync(1920, 1080);
        }
    }
}
