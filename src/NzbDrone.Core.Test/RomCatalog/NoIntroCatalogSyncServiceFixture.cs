using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.RomCatalog
{
    [TestFixture]
    public class NoIntroCatalogSyncServiceFixture : DbTest
    {
        private NoIntroCatalogSourceRepository _sourceRepository;
        private NoIntroCatalogEntryRepository _entryRepository;
        private NoIntroCatalogHashRepository _hashRepository;
        private NoIntroCatalogSyncService _subject;

        [SetUp]
        public void Setup()
        {
            _sourceRepository = Mocker.Resolve<NoIntroCatalogSourceRepository>();
            _entryRepository = Mocker.Resolve<NoIntroCatalogEntryRepository>();
            _hashRepository = Mocker.Resolve<NoIntroCatalogHashRepository>();
            _subject = Mocker.Resolve<NoIntroCatalogSyncService>();
        }

        [Test]
        public void sync_should_ingest_snapshot_and_update_metadata()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro",
                SourceUrl = "https://example.invalid/gba.dat",
                PinnedRevision = "pin-1"
            });

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.Fetch(source.SourceUrl))
                .Returns("<datafile><header><name>Nintendo - Game Boy Advance</name><version>2026.07</version></header><game name='F-Zero for Game Boy Advance (Japan)'><rom name='0001 - F-Zero for Game Boy Advance (Japan).gba' crc='ABCDEF12' sha1='0123456789' status='verified' /></game></datafile>");

            _subject.Sync(source.Id);

            var storedSource = _sourceRepository.Get(source.Id);
            storedSource.CatalogVersion.Should().Be("2026.07");
            storedSource.LastSuccessfulSync.Should().NotBeNull();
            storedSource.LastSyncError.Should().BeNull();

            var entry = _entryRepository.All().Should().ContainSingle().Subject;
            entry.CatalogSourceId.Should().Be(source.Id);
            entry.SystemKey.Should().Be("nintendo---game-boy-advance");

            _hashRepository.All().Should().Contain(x => x.CatalogEntryId == entry.Id && x.HashType == "crc32" && x.HashValue == "ABCDEF12");
        }

        [Test]
        public void sync_failure_should_preserve_existing_catalog_and_record_failure()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro",
                SourceUrl = "https://example.invalid/gba.dat",
                PinnedRevision = "pin-1",
                CatalogVersion = "old"
            });

            var existingEntry = _entryRepository.Insert(new NoIntroCatalogEntry
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                CanonicalName = "Existing Entry",
                CanonicalFileName = "Existing Entry.gba"
            });

            _hashRepository.Insert(new NoIntroCatalogHash
            {
                CatalogEntryId = existingEntry.Id,
                HashType = "sha1",
                HashValue = "oldhash",
                IsPrimary = true
            });

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.Fetch(source.SourceUrl))
                .Throws(new InvalidOperationException("boom"));

            Action act = () => _subject.Sync(source.Id);

            act.Should().Throw<InvalidOperationException>();

            _entryRepository.All().Should().ContainSingle(x => x.Id == existingEntry.Id);
            _hashRepository.All().Should().ContainSingle(x => x.HashValue == "oldhash");

            var storedSource = _sourceRepository.Get(source.Id);
            storedSource.LastAttemptedSync.Should().NotBeNull();
            storedSource.LastSyncError.Should().Be("boom");
            storedSource.CatalogVersion.Should().Be("old");
        }
    }
}
