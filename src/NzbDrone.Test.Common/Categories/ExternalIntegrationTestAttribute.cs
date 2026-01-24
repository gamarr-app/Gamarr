using NUnit.Framework;

namespace NzbDrone.Test.Common.Categories
{
    public class ExternalIntegrationTestAttribute : CategoryAttribute
    {
        public ExternalIntegrationTestAttribute()
            : base("ExternalIntegrationTest")
        {
        }
    }
}
