using System;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Integration.Test.Client;
using Gamarr.Api.V3.Queue;
using Gamarr.Http;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class QueueFixture : IntegrationTest
    {
        private PagingResource<QueueResource> GetFirstPage()
        {
            var request = Queue.BuildRequest();
            request.AddParameter("includeUnknownGameItems", true);

            return Queue.Get<PagingResource<QueueResource>>(request);
        }

        private void RefreshQueue()
        {
            var command = Commands.Post(new SimpleCommandResource { Name = "RefreshMonitoredDownloads" });

            for (var i = 0; i < 30; i++)
            {
                var updatedCommand = Commands.Get(command.Id);

                if (updatedCommand.Status == CommandStatus.Completed)
                {
                    return;
                }

                Thread.Sleep(1000);
            }
        }

        [Test]
        [Order(0)]
        public void ensure_queue_is_empty_when_download_client_is_configured()
        {
            EnsureNoDownloadClient();
            EnsureDownloadClient();

            var queue = GetFirstPage();

            queue.TotalRecords.Should().Be(0);
            queue.Records.Should().BeEmpty();
        }

        [Test]
        [Order(1)]
        public void ensure_queue_is_not_empty()
        {
            EnsureNoDownloadClient();

            var client = EnsureDownloadClient();
            var directory = client.Fields.First(v => v.Name == "watchFolder").Value as string;

            var filePath = Path.Combine(directory, "Game.Title.2024.zip");
            File.WriteAllText(filePath, "Test Download");

            // Backdate the file so it's older than the UsenetBlackhole ScanGracePeriod (30s)
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow.AddMinutes(-5));

            RefreshQueue();

            var queue = GetFirstPage();

            queue.TotalRecords.Should().Be(1);
            queue.Records.Should().NotBeEmpty();
        }
    }
}
