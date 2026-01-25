using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    public class ExternalAutomationTestAttribute : CategoryAttribute
    {
        public ExternalAutomationTestAttribute()
            : base("ExternalAutomationTest")
        {
        }
    }
}
