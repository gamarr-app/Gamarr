using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;
using Gamarr.Http.Ping;

namespace NzbDrone.Api.Test.Ping
{
    [TestFixture]
    public class PingControllerFixture : TestBase<PingController>
    {
        [SetUp]
        public void Setup()
        {
            var cacheManager = Mocker.Resolve<ICacheManager>();
            var cache = cacheManager.GetCache<IEnumerable<Config>>(typeof(PingController));
            cache.Clear();
        }

        [Test]
        public void should_return_ok_when_database_is_healthy()
        {
            Mocker.GetMock<IConfigRepository>()
                .Setup(r => r.All())
                .Returns(new List<Config>());

            var result = Subject.GetStatus();

            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var resource = objectResult.Value as PingResource;
            resource.Status.Should().Be("OK");
        }

        [Test]
        public void should_return_error_when_database_throws()
        {
            Mocker.GetMock<IConfigRepository>()
                .Setup(r => r.All())
                .Throws(new Exception("database unavailable"));

            var result = Subject.GetStatus();

            var objectResult = result.Result as ObjectResult;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var resource = objectResult.Value as PingResource;
            resource.Status.Should().Be("Error");

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
