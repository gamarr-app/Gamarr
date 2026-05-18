using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test
{
    [TestFixture]
    public class HttpLogFixture : IntegrationTest
    {
        [Test]
        public void should_log_on_error()
        {
            var config = HostConfig.Get(1);
            config.LogLevel = "Trace";
            HostConfig.Put(config);

            var logFile = "gamarr.trace.txt";
            var beforeCount = Logs.GetLogFileLines(logFile).Length;

            Games.InvalidPost(new Gamarr.Api.V3.Games.GameResource());

            // Only assert on lines written after the POST; the previous slice-based
            // approach was sensitive to interleaving with the log-fetch endpoint's
            // own request/response logging and went flaky once timing changed.
            var newLines = Logs.GetLogFileLines(logFile).Skip(beforeCount).ToArray();

            newLines.Should().Contain(v => v.Contains("|Trace|Http|Req") && v.Contains("/api/v3/game/"));
            newLines.Should().Contain(v => v.Contains("|Trace|Http|Res") && v.Contains("/api/v3/game/: 400.BadRequest"));
            newLines.Should().Contain(v => v.Contains("|Debug|Api|") && v.Contains("/api/v3/game/: 400.BadRequest"));
        }
    }
}
