using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Organizer
{
    [TestFixture]
    public class NamingConfigServiceFixture : CoreTest<NamingConfigService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<INamingConfigRepository>()
                  .Setup(s => s.SingleOrDefault())
                  .Returns(NamingConfig.Default);
        }

        [Test]
        public void should_return_config_when_exists()
        {
            var result = Subject.GetConfig();

            result.Should().NotBeNull();
        }

        [Test]
        public void should_create_default_config_when_none_exists()
        {
            Mocker.GetMock<INamingConfigRepository>()
                  .SetupSequence(s => s.SingleOrDefault())
                  .Returns((NamingConfig)null)
                  .Returns((NamingConfig)null)
                  .Returns(NamingConfig.Default);

            Mocker.GetMock<INamingConfigRepository>()
                  .Setup(s => s.Single())
                  .Returns(NamingConfig.Default);

            var result = Subject.GetConfig();

            result.Should().NotBeNull();

            Mocker.GetMock<INamingConfigRepository>()
                  .Verify(s => s.Insert(It.IsAny<NamingConfig>()), Times.Once());
        }

        [Test]
        public void should_save_config()
        {
            var config = NamingConfig.Default;

            Subject.Save(config);

            Mocker.GetMock<INamingConfigRepository>()
                  .Verify(s => s.Upsert(config), Times.Once());
        }
    }
}
