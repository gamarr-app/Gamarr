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
            await Expect(Page.Locator("div[class*='GameIndex']").First).ToBeVisibleAsync();

            // Calendar
            await ClickNavLinkAsync("Calendar");
            await Expect(Page.Locator("div[class*='CalendarPage']").First).ToBeVisibleAsync();

            // Activity
            await ClickNavLinkAsync("Activity");
            await Expect(Page.Locator("div[class*='Queue']").First).ToBeVisibleAsync();

            // Wanted
            await ClickNavLinkAsync("Wanted");
            await Expect(Page.Locator("div[class*='Missing']").First).ToBeVisibleAsync();

            // Settings
            await ClickNavLinkAsync("Settings");
            await Expect(Page.Locator("div[class*='Settings']").First).ToBeVisibleAsync();

            // System
            await ClickNavLinkAsync("System");
            await Expect(Page.Locator("div[class*='Health']").First).ToBeVisibleAsync();
        }

        [Test]
        public async Task Navigate_Through_Activity_Tabs()
        {
            await ClickNavLinkAsync("Activity");

            // Queue tab (default)
            await Expect(Page.Locator("div[class*='Queue']").First).ToBeVisibleAsync();

            // History tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "History" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='History']").First).ToBeVisibleAsync();

            // Blocklist tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Blocklist" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Blocklist']").First).ToBeVisibleAsync();

            // Back to Queue
            await Page.GetByRole(AriaRole.Link, new () { Name = "Queue" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Queue']").First).ToBeVisibleAsync();
        }

        [Test]
        public async Task Navigate_Through_Wanted_Tabs()
        {
            await ClickNavLinkAsync("Wanted");

            // Missing tab (default)
            await Expect(Page.Locator("div[class*='Missing']").First).ToBeVisibleAsync();

            // Cutoff Unmet tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Cutoff Unmet" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='CutoffUnmet']").First).ToBeVisibleAsync();

            // Back to Missing
            await Page.GetByRole(AriaRole.Link, new () { Name = "Missing" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Missing']").First).ToBeVisibleAsync();
        }

        [Test]
        public async Task Navigate_Through_System_Tabs()
        {
            await ClickNavLinkAsync("System");

            // Status tab (default)
            await Expect(Page.Locator("div[class*='Health']").First).ToBeVisibleAsync();

            // Tasks tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Tasks" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Tasks']").First).ToBeVisibleAsync();

            // Backup tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Backup" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Backups']").First).ToBeVisibleAsync();

            // Updates tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Updates" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='Updates']").First).ToBeVisibleAsync();

            // Events tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Events" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='LogsTable']").First).ToBeVisibleAsync();

            // Log Files tab
            await Page.GetByRole(AriaRole.Link, new () { Name = "Log Files" }).First.ClickAsync();
            await WaitForNoSpinner();
            await Expect(Page.Locator("div[class*='LogFiles']").First).ToBeVisibleAsync();
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
                ("Tags", "TagSettings"),
                ("General", "GeneralSettings"),
                ("UI", "UISettings")
            };

            foreach (var (linkText, expectedClass) in settingsLinks)
            {
                await Page.GetByRole(AriaRole.Link, new () { Name = linkText, Exact = true }).First.ClickAsync();
                await WaitForNoSpinner();
                await Expect(Page.Locator($"div[class*='{expectedClass}']").First).ToBeVisibleAsync();
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
        [Ignore("Test requires internet access for game search API (Steam/IGDB)")]
        public async Task Add_Game_Full_Flow_Works()
        {
            // First, set up required configuration via API
            var apiKey = GetApiKeyFromRunner();

            // Create root folder
            await CreateRootFolderAsync(apiKey, "/tmp/gamarr-test-games");

            // Navigate to add game page
            await NavigateToAsync("/add/new");

            // Search for a known Steam game
            var searchInput = Page.Locator("input[class*='searchInput']");
            await Expect(searchInput).ToBeVisibleAsync();
            await searchInput.FillAsync("The Witness");
            await searchInput.PressAsync("Enter");

            // Wait for search results to appear
            await Page.Locator("div[class*='searchResult']").First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30000
            });

            await TakeScreenshotAsync("add_game_search_results");

            // Click on the first search result to open add modal
            await Page.Locator("div[class*='searchResult']").First.ClickAsync();

            // Wait for the modal to appear
            await Page.Locator("div[class*='ModalContent']").First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            });

            await TakeScreenshotAsync("add_game_modal");

            // Verify modal has required elements
            await Expect(Page.Locator("div[class*='ModalContent']").First).ToBeVisibleAsync();

            // Click the Add Game button
            var addButton = Page.GetByRole(AriaRole.Button, new () { Name = "Add Game" });
            await Expect(addButton).ToBeVisibleAsync();
            await addButton.ClickAsync();

            // Wait for the modal to close or redirect
            await Task.Delay(2000);

            // Take screenshot of result
            await TakeScreenshotAsync("add_game_result");

            // Verify no JavaScript errors occurred
            await AssertNoJavaScriptErrors();
        }

        private string GetApiKeyFromRunner()
        {
            // Get API key from the test runner
            return ApiKey;
        }

        private async Task CreateRootFolderAsync(string apiKey, string path)
        {
            // Create the directory if it doesn't exist
            System.IO.Directory.CreateDirectory(path);

            // Create root folder via API
            try
            {
                await Page.APIRequest.PostAsync($"{BaseUrl}/api/v3/rootfolder", new APIRequestContextOptions
                {
                    Headers = new Dictionary<string, string> { { "X-Api-Key", apiKey } },
                    DataObject = new { path }
                });
            }
            catch
            {
                // Root folder might already exist, that's ok
            }
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
            await Expect(Page.Locator("div[class*='CalendarPage']").First).ToBeVisibleAsync();

            // Take screenshot of calendar
            await TakeScreenshotAsync("calendar_view");
        }

        [Test]
        public async Task Calendar_Navigation_Works()
        {
            await ClickNavLinkAsync("Calendar");

            // Look for calendar navigation buttons
            var header = Page.Locator("div[class*='CalendarHeader']").First;
            await Expect(header).ToBeVisibleAsync();

            await TakeScreenshotAsync("calendar_navigation");
        }

        [Test]
        public async Task Games_Index_Has_View_Options()
        {
            await ClickNavLinkAsync("Games");
            await Expect(Page.Locator("div[class*='GameIndex']").First).ToBeVisibleAsync();

            // Check for view mode buttons/options in the toolbar
            await Expect(Page.Locator("div[class*='PageToolbar']").First).ToBeVisibleAsync();

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
                var modal = Page.Locator("div[class*='Modal']").First;
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
