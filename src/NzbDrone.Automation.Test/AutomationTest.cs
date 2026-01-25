using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Test.Common;

namespace NzbDrone.Automation.Test
{
    [TestFixture]
    [AutomationTest]
    public abstract class AutomationTest : PageTest
    {
        private NzbDroneRunner _runner;
        protected string ApiKey => _runner?.ApiKey;
        protected List<string> ConsoleErrors { get; private set; } = new ();
        protected const string BaseUrl = "http://localhost:6767";

        /// <summary>
        /// Mock metadata is enabled by default so tests work without network access.
        /// Override to false for tests that specifically need real API responses.
        /// </summary>
        protected virtual bool UseMockMetadata => true;

        public AutomationTest()
        {
            new StartupContext();

            LogManager.Configuration = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget { Layout = "${level}: ${message} ${exception}" };
            LogManager.Configuration.AddTarget(consoleTarget.GetType().Name, consoleTarget);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Trace, consoleTarget));
        }

        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                IgnoreHTTPSErrors = true
            };
        }

        [OneTimeSetUp]
        public async Task SmokeTestSetup()
        {
            _runner = new NzbDroneRunner(LogManager.GetCurrentClassLogger(), null);
            _runner.KillAll();

            // Configure mock metadata mode so tests work without network access
            _runner.UseMockMetadata = UseMockMetadata;
            _runner.MockDataPath = FindMockDataPath();

            _runner.Start(true);

            // Wait for server to be ready
            await Task.Delay(2000);
        }

        /// <summary>
        /// Attempts to find the mock data path relative to the test directory.
        /// </summary>
        private static string FindMockDataPath()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            var possiblePaths = new[]
            {
                Path.Combine(testDir, "Files", "MockData"),
                Path.Combine(testDir, "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "..", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "src", "NzbDrone.Core.Test", "Files", "MockData"),
                Path.Combine(testDir, "..", "..", "..", "src", "NzbDrone.Core.Test", "Files", "MockData"),
            };

            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        [SetUp]
        public async Task TestSetup()
        {
            ConsoleErrors.Clear();

            // Listen for console errors (but ignore certain expected errors)
            Page.Console += (_, msg) =>
            {
                if (msg.Type == "error")
                {
                    // Ignore React development warnings that come through as console errors
                    if (msg.Text.StartsWith("Warning:") || msg.Text.Contains("reactjs.org/link"))
                    {
                        return;
                    }

                    // Ignore 404 errors for external resources (media/covers may not always exist)
                    if (msg.Text.Contains("404") || msg.Text.Contains("Failed to load resource"))
                    {
                        return;
                    }

                    // Ignore React key warnings (common in dynamic lists)
                    if (msg.Text.Contains("same key") || msg.Text.Contains("key prop"))
                    {
                        return;
                    }

                    ConsoleErrors.Add(msg.Text);
                }
            };

            // Set longer timeout for assertions (CI is slower)
            SetDefaultExpectTimeout(30000);

            // Listen for page errors (uncaught exceptions)
            Page.PageError += (_, error) =>
            {
                ConsoleErrors.Add(error);
            };

            await Page.GotoAsync(BaseUrl);
            await WaitForNoSpinner();

            // Enable debug mode
            await Page.EvaluateAsync("window.Gamarr.NameViews = true");
        }

        [TearDown]
        public async Task TestTearDown()
        {
            if (ConsoleErrors.Count > 0)
            {
                var screenshotPath = $"./{TestContext.CurrentContext.Test.Name}_error_screenshot.png";
                await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
                Assert.Fail($"JavaScript errors detected:\n{string.Join("\n", ConsoleErrors)}");
            }
        }

        [OneTimeTearDown]
        public void SmokeTestTearDown()
        {
            _runner?.KillAll();
        }

        protected async Task WaitForNoSpinner(int timeoutMs = 30000)
        {
            // Give the spinner time to appear
            await Task.Delay(200);

            try
            {
                // Wait for all spinners to be hidden (use .Nth(0) to avoid strict mode issues with multiple spinners)
                var spinners = Page.Locator(".followingBalls");
                var count = await spinners.CountAsync();

                for (var i = 0; i < count; i++)
                {
                    await spinners.Nth(i).WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Hidden,
                        Timeout = timeoutMs
                    });
                }
            }
            catch (TimeoutException)
            {
                // Spinner might not exist, which is fine
            }
        }

        protected async Task NavigateToAsync(string path)
        {
            await Page.GotoAsync($"{BaseUrl}{path}");
            await WaitForNoSpinner();
        }

        protected async Task TakeScreenshotAsync(string name)
        {
            try
            {
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = $"./{name}_test_screenshot.png"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save screenshot {name}: {ex.Message}");
            }
        }

        protected async Task ClickNavLinkAsync(string text)
        {
            // Use First() because sidebar may have parent + child links with same name
            await Page.GetByRole(AriaRole.Link, new () { Name = text }).First.ClickAsync();
            await WaitForNoSpinner();
        }

        protected Task AssertNoJavaScriptErrors()
        {
            Assert.That(ConsoleErrors, Is.Empty, $"JavaScript errors found: {string.Join(", ", ConsoleErrors)}");
            return Task.CompletedTask;
        }
    }
}
