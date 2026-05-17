using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    [TestFixture]
    public class IsolatedSettingsTest : AutomationTest
    {
        [Test]
        [AutomationTest]
        public async Task Settings_General_In_Isolation()
        {
            await NavigateToAsync("/settings/general");
            await Expect(Page.Locator("div[class*='GeneralSettings']").First).ToBeVisibleAsync();
            await TakeScreenshotAsync("isolated_settings_general");
        }
    }
}
