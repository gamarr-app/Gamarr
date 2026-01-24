using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Converters
{
    [TestFixture]
    public class ProviderSettingConverterFixture : CoreTest<ProviderSettingConverter>
    {
        [Test]
        public void should_return_null_if_config_is_null()
        {
            Subject.Parse(null).Should().BeNull();
        }

        [TestCase(null)]
        [TestCase("")]
        public void should_return_null_if_config_is_empty(object dbValue)
        {
            Subject.Parse(dbValue).Should().BeNull();
        }
    }
}
