using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.CustomFormats.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats
{
    [TestFixture]
    public class CustomFormatServiceFixture : CoreTest<CustomFormatService>
    {
        private CustomFormat _customFormat;

        [SetUp]
        public void Setup()
        {
            _customFormat = new CustomFormat
            {
                Id = 1,
                Name = "Test Format",
                IncludeCustomFormatWhenRenaming = true,
                Specifications = new List<ICustomFormatSpecification>()
            };

            Mocker.GetMock<ICustomFormatRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<CustomFormat> { _customFormat });

            Mocker.GetMock<ICustomFormatRepository>()
                  .Setup(s => s.Get(1))
                  .Returns(_customFormat);

            Mocker.GetMock<ICacheManager>()
                  .Setup(s => s.GetCache<Dictionary<int, CustomFormat>>(typeof(CustomFormat), "formats"))
                  .Returns(new Cached<Dictionary<int, CustomFormat>>());
        }

        [Test]
        public void should_return_all_custom_formats()
        {
            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_get_custom_format_by_id()
        {
            Subject.GetById(1).Should().Be(_customFormat);
        }

        [Test]
        public void should_insert_custom_format()
        {
            var newFormat = new CustomFormat { Name = "New Format" };

            Mocker.GetMock<ICustomFormatRepository>()
                  .Setup(s => s.Insert(newFormat))
                  .Returns(newFormat);

            Subject.Insert(newFormat);

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.Insert(newFormat), Times.Once());
        }

        [Test]
        public void should_publish_event_on_insert()
        {
            var newFormat = new CustomFormat { Name = "New Format" };

            Mocker.GetMock<ICustomFormatRepository>()
                  .Setup(s => s.Insert(newFormat))
                  .Returns(newFormat);

            Subject.Insert(newFormat);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent(It.IsAny<CustomFormatAddedEvent>()), Times.Once());
        }

        [Test]
        public void should_update_custom_format()
        {
            Subject.Update(_customFormat);

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.Update(_customFormat), Times.Once());
        }

        [Test]
        public void should_update_multiple_custom_formats()
        {
            var formats = new List<CustomFormat> { _customFormat };

            Subject.Update(formats);

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.UpdateMany(formats), Times.Once());
        }

        [Test]
        public void should_delete_custom_format()
        {
            Subject.Delete(1);

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.Delete(1), Times.Once());
        }

        [Test]
        public void should_publish_event_on_delete()
        {
            Subject.Delete(1);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent(It.IsAny<CustomFormatDeletedEvent>()), Times.Once());
        }

        [Test]
        public void should_delete_multiple_custom_formats()
        {
            var ids = new List<int> { 1 };

            Subject.Delete(ids);

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.Delete(It.IsAny<int>()), Times.Once());
        }
    }
}
