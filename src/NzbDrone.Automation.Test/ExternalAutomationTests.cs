using Microsoft.Playwright;
using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    /// <summary>
    /// Tests that require external network access (real API calls).
    /// These tests are slower and depend on external services.
    /// </summary>
    [TestFixture]
    public class ExternalAutomationTests : AutomationTest
    {
        // Disable mock metadata so tests use real external APIs
        protected override bool UseMockMetadata => false;

        [Test]
        [ExternalAutomationTest]
        public async Task Add_Game_Full_Flow_Works()
        {
            // First, set up required configuration via API
            var apiKey = ApiKey;

            // Create root folder
            await CreateRootFolderAsync(apiKey, "/tmp/gamarr-test-games");

            // Navigate to add game page
            await NavigateToAsync("/add/new");

            // Search for a known Steam game with retry logic for flaky external API
            var searchInput = Page.Locator("input[class*='searchInput']");
            await Expect(searchInput).ToBeVisibleAsync();

            var maxRetries = 3;
            var searchSucceeded = false;
            Exception lastException = null;

            for (var attempt = 1; attempt <= maxRetries && !searchSucceeded; attempt++)
            {
                try
                {
                    Console.WriteLine($"[DEBUG] Search attempt {attempt}/{maxRetries}");

                    // Clear and fill search input
                    await searchInput.ClearAsync();
                    await searchInput.FillAsync("The Witness");

                    // Take screenshot before pressing Enter
                    await TakeScreenshotAsync($"search_attempt_{attempt}_before_enter");

                    await searchInput.PressAsync("Enter");

                    Console.WriteLine($"[DEBUG] Pressed Enter, waiting for results...");

                    // Wait for search results to appear
                    await Page.Locator("div[class*='searchResult']").First.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 30000
                    });

                    Console.WriteLine($"[DEBUG] Search results appeared on attempt {attempt}");
                    searchSucceeded = true;
                }
                catch (TimeoutException ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    Console.WriteLine($"[DEBUG] Attempt {attempt} timed out, taking debug screenshot...");

                    // Take screenshot to see what's on screen
                    await TakeScreenshotAsync($"search_attempt_{attempt}_timeout");

                    // Log page HTML for debugging
                    var bodyHtml = await Page.Locator("body").InnerHTMLAsync();
                    Console.WriteLine($"[DEBUG] Page body length: {bodyHtml.Length} chars");

                    // Check if loading indicator is visible (search still in progress)
                    var loadingLocator = Page.Locator("div[class*='LoadingIndicator'], div[class*='loadingIndicator']").First;
                    var isLoading = await loadingLocator.IsVisibleAsync();
                    Console.WriteLine($"[DEBUG] Loading indicator visible: {isLoading}");

                    // Check if "no results" message appears
                    var noResultsLocator = Page.Locator("div[class*='noResults']").First;
                    var hasNoResults = await noResultsLocator.IsVisibleAsync();
                    Console.WriteLine($"[DEBUG] No results message visible: {hasNoResults}");

                    // Check if there's an error/alert message visible
                    var alertLocator = Page.Locator("div[class*='alert'], div[class*='Alert']").First;
                    if (await alertLocator.IsVisibleAsync())
                    {
                        var alertText = await alertLocator.InnerTextAsync();
                        Console.WriteLine($"[DEBUG] Alert message visible: {alertText}");
                    }

                    // Check for "Failed loading search results" message
                    var failedLocator = Page.Locator("text=Failed");
                    if (await failedLocator.IsVisibleAsync())
                    {
                        Console.WriteLine($"[DEBUG] 'Failed' text found on page");
                    }

                    // Log the search input value
                    var inputValue = await searchInput.InputValueAsync();
                    Console.WriteLine($"[DEBUG] Search input value: '{inputValue}'");

                    // Count how many search results exist (even if hidden)
                    var resultCount = await Page.Locator("div[class*='searchResult']").CountAsync();
                    Console.WriteLine($"[DEBUG] Search result elements found: {resultCount}");

                    // Log first 500 chars of body for inspection
                    Console.WriteLine($"[DEBUG] Body preview: {bodyHtml.Substring(0, Math.Min(500, bodyHtml.Length))}...");

                    // Wait before retrying
                    await Task.Delay(2000);
                }
                catch (TimeoutException ex)
                {
                    lastException = ex;
                    Console.WriteLine($"[DEBUG] Final attempt {attempt} timed out");
                    await TakeScreenshotAsync($"search_attempt_{attempt}_final_timeout");
                }
            }

            if (!searchSucceeded)
            {
                // Take final debug screenshot
                await TakeScreenshotAsync("search_all_attempts_failed");

                var message = $"Search results did not appear after {maxRetries} attempts. " +
                              $"Last error: {lastException?.Message}";
                throw new TimeoutException(message, lastException);
            }

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
    }
}
