using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.TPL;
using NzbDrone.Test.Common;
using HttpClient = NzbDrone.Common.Http.HttpClient;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class HttpClientDownloadFileFixture : TestBase<HttpClient>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
            Mocker.SetConstant<IRateLimitService>(Mocker.Resolve<RateLimitService>());
            Mocker.SetConstant<IEnumerable<IHttpRequestInterceptor>>(Array.Empty<IHttpRequestInterceptor>());
        }

        [Test]
        public async Task should_not_clobber_a_concurrent_download_of_the_same_file()
        {
            var file = GetTempFilePath();

            var firstResponseWritten = new TaskCompletionSource();
            var secondDownloadFinished = new TaskCompletionSource();
            var calls = 0;

            Mocker.GetMock<IHttpDispatcher>()
                  .Setup(v => v.GetResponseAsync(It.IsAny<HttpRequest>(), It.IsAny<CookieContainer>()))
                  .Returns<HttpRequest, CookieContainer>(async (request, cookies) =>
                  {
                      var isFirstCall = Interlocked.Increment(ref calls) == 1;

                      await request.ResponseStream.WriteAsync(new byte[1024].AsMemory());

                      // Hold the first download open until the second one has
                      // finished with the same destination file.
                      if (isFirstCall)
                      {
                          firstResponseWritten.SetResult();
                          await secondDownloadFinished.Task;
                      }

                      return new HttpResponse(request, new HttpHeader(), Array.Empty<byte>());
                  });

            var firstDownload = Subject.DownloadFileAsync("http://localhost/poster.jpg", file);

            await firstResponseWritten.Task;

            await Subject.DownloadFileAsync("http://localhost/poster.jpg", file);

            secondDownloadFinished.SetResult();

            await firstDownload;

            new FileInfo(file).Length.Should().Be(1024);
            Directory.GetFiles(TempFolder, "*.part").Should().BeEmpty();
        }
    }
}
