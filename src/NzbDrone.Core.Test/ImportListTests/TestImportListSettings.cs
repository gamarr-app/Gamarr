using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Test.ImportListTests
{
    public class TestImportListSettings : ImportListSettingsBase<TestImportListSettings>
    {
        public string BaseUrl { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult();
        }
    }
}
