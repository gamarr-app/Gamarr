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
        private List<CustomFormat> _formats;

        [SetUp]
        public void Setup()
        {
            _formats = new List<CustomFormat>
            {
                new CustomFormat { Id = 1, Name = "Format1" },
                new CustomFormat { Id = 2, Name = "Format2" }
            };

            Mocker.GetMock<ICustomFormatRepository>()
                  .Setup(s => s.All())
                  .Returns(_formats);

            Mocker.GetMock<ICacheManager>()
                  .Setup(s => s.GetCache<Dictionary<int, CustomFormat>>(It.IsAny<System.Type>(), It.IsAny<string>()))
                  .Returns(new Cached<Dictionary<int, CustomFormat>>());
        }

        [Test]
        public void should_return_all_formats()
        {
            var result = Subject.All();

            result.Should().HaveCount(2);
        }

        [Test]
        public void should_get_by_id()
        {
            var result = Subject.GetById(1);

            result.Name.Should().Be("Format1");
        }

        [Test]
        public void should_insert_new_format()
        {
            var newFormat = new CustomFormat { Name = "NewFormat" };

            Mocker.GetMock<ICustomFormatRepository>()
                  .Setup(s => s.Insert(newFormat))
                  .Returns(new CustomFormat { Id = 3, Name = "NewFormat" });

            Subject.Insert(newFormat);

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.Insert(newFormat), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent(It.IsAny<CustomFormatAddedEvent>()), Times.Once());
        }

        [Test]
        public void should_update_format()
        {
            var format = _formats.First();

            Subject.Update(format);

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.Update(format), Times.Once());
        }

        [Test]
        public void should_delete_format_and_publish_event()
        {
            Mocker.GetMock<ICustomFormatRepository>()
                  .Setup(s => s.Get(1))
                  .Returns(_formats.First());

            Subject.Delete(1);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent(It.IsAny<CustomFormatDeletedEvent>()), Times.Once());

            Mocker.GetMock<ICustomFormatRepository>()
                  .Verify(s => s.Delete(1), Times.Once());
        }
    }
}
