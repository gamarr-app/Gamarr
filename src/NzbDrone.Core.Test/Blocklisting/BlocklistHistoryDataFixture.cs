using System;
using System.Collections.Generic;
using System.Data.SQLite;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Download;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Blocklisting
{
    /// <summary>
    /// DownloadFailedEvent.Data is a history row reloaded from the database, not the
    /// dictionary HistoryService built: FailedDownloadService hands over
    /// historyItem.Data verbatim. HistoryService writes capitalised keys ("Size",
    /// "Indexer", "PublishedDate") while GameHistory's constructor only makes the
    /// *in-memory* dictionary case-insensitive, and EmbeddedDocumentConverter sets
    /// PropertyNameCaseInsensitive, which does not apply to dictionary KEYS -- so the
    /// rehydrated dictionary is case-sensitive. BlocklistService.Handle reads lowercase
    /// keys, which would give Size=0, Indexer=null and a throwing publishedDate parse.
    ///
    /// It does not, and this fixture is the proof: the converter also sets
    /// DictionaryKeyPolicy = CamelCase, so the keys are camelCased on the way *in* and
    /// come back already matching the lowercase reads. The defect is real in the types
    /// but unreachable in practice. These tests exist so that removing or changing that
    /// key policy fails loudly here instead of silently writing Size=0 blocklist rows.
    /// </summary>
    [TestFixture]
    public class BlocklistHistoryDataFixture : CoreTest<BlocklistService>
    {
        private EmbeddedDocumentConverter<Dictionary<string, string>> _converter;

        [SetUp]
        public void Setup()
        {
            _converter = new EmbeddedDocumentConverter<Dictionary<string, string>>();
        }

        private Dictionary<string, string> RoundTrip(Dictionary<string, string> data)
        {
            var param = new SQLiteParameter();
            _converter.SetValue(param, data);

            return _converter.Parse((string)param.Value);
        }

        private static Dictionary<string, string> GrabbedHistoryData()
        {
            // Exactly the keys HistoryService.Handle(GameGrabbedEvent) writes, with its
            // capitalisation.
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Indexer", "1337x (Prowlarr)" },
                { "PublishedDate", "2026-10-01T12:00:00Z" },
                { "Size", "8031189401" },
                { "Protocol", "1" },
                { "IndexerFlags", "G_Freeleech" }
            };
        }

        [Test]
        public void rehydrated_history_data_is_key_case_sensitive()
        {
            var rehydrated = RoundTrip(GrabbedHistoryData());

            // The capitalised keys HistoryService wrote do not come back: the dictionary
            // GameHistory made case-insensitive is gone, replaced by a plain one.
            rehydrated.ContainsKey("Size").Should().BeFalse();
            rehydrated.ContainsKey("Indexer").Should().BeFalse();
        }

        [Test]
        public void rehydrated_history_data_still_resolves_the_keys_the_blocklist_reads()
        {
            var rehydrated = RoundTrip(GrabbedHistoryData());

            rehydrated.Should().ContainKey("size");
            rehydrated.Should().ContainKey("indexer");
            rehydrated.Should().ContainKey("publishedDate");
            rehydrated.Should().ContainKey("protocol");
            rehydrated.Should().ContainKey("indexerFlags");
        }

        [Test]
        public void should_blocklist_a_failed_download_with_the_real_size_and_indexer()
        {
            var message = new DownloadFailedEvent
            {
                GameId = 69,
                Quality = new QualityModel(),
                SourceTitle = "Princess Peach Showtime NSP",
                Data = RoundTrip(GrabbedHistoryData())
            };

            Subject.Handle(message);

            Mocker.GetMock<IBlocklistRepository>()
                  .Verify(v => v.Insert(It.Is<Blocklist>(b =>
                      b.Size == 8031189401L &&
                      b.Indexer == "1337x (Prowlarr)")), Times.Once());
        }
    }
}
