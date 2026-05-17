using Microsoft.Playwright;
using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    // Temporary single-test fixture for fast CI iteration while we debug
    // the rest of the Automation suite. Once green, remove this file and
    // delete the [Explicit] attributes on MainPagesTest / UserFlowsTest.
    [TestFixture]
    public class SmokeTest : AutomationTest
    {
        [Test]
        public async Task Root_Page_Loads_Without_Errors()
        {
            // Page is already navigated to BaseUrl by TestSetup. Just verify
            // the SPA bootstrapped and rendered the sidebar — that's enough
            // to prove auth + bundle + bootstrap + render all work.
            await Expect(Page.GetByRole(AriaRole.Link, new () { Name = "Games" }).First).ToBeVisibleAsync();
            await TakeScreenshotAsync("smoke_root");
        }
    }
}
