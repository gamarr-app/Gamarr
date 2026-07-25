using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    // On-demand harness: feed a file of real release names (one per line,
    // e.g. pulled from srrdb or an indexer) through the actual parser and
    // print a coverage report. Run with:
    //   dotnet test --filter Name~RealCorpusExploration -e CORPUS_FILE=/path/to/corpus.txt
    [TestFixture]
    [Explicit("Requires CORPUS_FILE env var pointing at a release-name list")]
    public class RealCorpusExplorationFixture : CoreTest
    {
        [Test]
        public void report_parser_coverage_for_corpus()
        {
            var path = Environment.GetEnvironmentVariable("CORPUS_FILE");

            if (path == null || !File.Exists(path))
            {
                Assert.Ignore("CORPUS_FILE not set or missing");
            }

            var titles = File.ReadAllLines(path).Where(l => l.Trim().Length > 0).Distinct().ToList();

            var unparsed = new List<string>();
            var byContentType = new Dictionary<ReleaseContentType, int>();
            var updateNoVersion = new List<string>();
            var emptyTitle = new List<string>();

            foreach (var title in titles)
            {
                ParsedGameInfo info = null;

                try
                {
                    info = Core.Parser.Parser.ParseGameTitle(title);
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"THROWS [{title}]: {ex.GetType().Name} {ex.Message}");
                }

                if (info == null)
                {
                    unparsed.Add(title);
                    continue;
                }

                byContentType.TryGetValue(info.ContentType, out var n);
                byContentType[info.ContentType] = n + 1;

                if (string.IsNullOrWhiteSpace(info.GameTitle))
                {
                    emptyTitle.Add(title);
                }

                if (info.ContentType == ReleaseContentType.UpdateOnly && info.GameVersion?.HasValue != true)
                {
                    updateNoVersion.Add(title);
                }
            }

            TestContext.Out.WriteLine($"total: {titles.Count}");
            TestContext.Out.WriteLine($"unparsed (null): {unparsed.Count}");

            foreach (var kv in byContentType.OrderByDescending(k => k.Value))
            {
                TestContext.Out.WriteLine($"  contentType {kv.Key}: {kv.Value}");
            }

            TestContext.Out.WriteLine($"empty primary title: {emptyTitle.Count}");
            TestContext.Out.WriteLine($"UpdateOnly without version: {updateNoVersion.Count}");

            TestContext.Out.WriteLine("--- unparsed sample:");
            unparsed.Take(40).ToList().ForEach(t => TestContext.Out.WriteLine($"  {t}"));

            TestContext.Out.WriteLine("--- update-with-no-version sample:");
            updateNoVersion.Take(40).ToList().ForEach(t => TestContext.Out.WriteLine($"  {t}"));

            TestContext.Out.WriteLine("--- empty-title sample:");
            emptyTitle.Take(20).ToList().ForEach(t => TestContext.Out.WriteLine($"  {t}"));
        }
    }
}
