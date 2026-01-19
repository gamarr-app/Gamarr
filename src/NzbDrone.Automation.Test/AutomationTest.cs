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
        protected List<string> ConsoleErrors { get; private set; } = new();
        protected const string BaseUrl = "http://localhost:6767";

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
            _runner.Start(true);

            // Wait for server to be ready
            await Task.Delay(2000);
        }

        [SetUp]
        public async Task TestSetup()
        {
            ConsoleErrors.Clear();

            // Listen for console errors
            Page.Console += (_, msg) =>
            {
                if (msg.Type == "error")
                {
                    ConsoleErrors.Add(msg.Text);
                }
            };

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
                await Page.Locator(".followingBalls").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = timeoutMs
                });
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
            await Page.GetByRole(AriaRole.Link, new() { Name = text }).ClickAsync();
            await WaitForNoSpinner();
        }

        protected async Task AssertNoJavaScriptErrors()
        {
            Assert.That(ConsoleErrors, Is.Empty, $"JavaScript errors found: {string.Join(", ", ConsoleErrors)}");
        }
    }
}
