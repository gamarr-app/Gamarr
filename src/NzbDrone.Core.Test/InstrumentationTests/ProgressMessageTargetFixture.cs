using NLog;
using NLog.Config;
using NUnit.Framework;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.ProgressMessaging;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.InstrumentationTests
{
    [TestFixture]
    [NonParallelizable]
    public class ProgressMessageTargetFixture : CoreTest<ProgressMessageTarget>
    {
        private LoggingConfiguration _originalConfiguration;

        [SetUp]
        public void Setup()
        {
            _originalConfiguration = LogManager.Configuration;
        }

        [TearDown]
        public void Teardown()
        {
            LogManager.Configuration = _originalConfiguration;
        }

        [Test]
        public void should_not_throw_when_log_manager_configuration_is_null()
        {
            // AppLifetime nulls LogManager.Configuration on a (non-restart)
            // shutdown. A shutdown requested while startup is still propagating
            // can race this handler on another thread and null the config out
            // from under it.
            LogManager.Configuration = null;

            Assert.DoesNotThrow(() => Subject.Handle(new ApplicationStartedEvent()));
        }

        [Test]
        public void should_register_target_when_log_manager_configuration_is_set()
        {
            var configuration = new LoggingConfiguration();
            LogManager.Configuration = configuration;

            Subject.Handle(new ApplicationStartedEvent());

            Assert.That(configuration.FindTargetByName("ProgressMessagingLogger"), Is.Not.Null);
        }
    }
}
