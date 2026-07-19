using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
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
        public void sync_should_preserve_quoted_clrmamepro_rom_filenames()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro",
                SourceUrl = "https://example.invalid/gba.dat"
            });

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.Fetch(source.SourceUrl))
                .Returns("clrmamepro (\n" +
                         "\tname \"Nintendo - Game Boy Advance\"\n" +
                         "\tversion \"2026.07\"\n" +
                         ")\n" +
                         "game (\n" +
                         "\tname \"007 - Everything or Nothing (USA, Europe) (En,Fr,De)\"\n" +
                         "\trom ( name \"007 - Everything or Nothing (USA, Europe) (En,Fr,De).gba\" size 16777216 crc 1234ABCD md5 0123456789ABCDEF0123456789ABCDEF sha1 0123456789ABCDEF0123456789ABCDEF01234567 )\n" +
                         ")\n");

            _subject.Sync(source.Id);

            var entry = _entryRepository.All().Should().ContainSingle().Subject;
            entry.CanonicalName.Should().Be("007 - Everything or Nothing (USA, Europe) (En,Fr,De)");
            entry.CanonicalFileName.Should().Be("007 - Everything or Nothing (USA, Europe) (En,Fr,De).gba");
            _hashRepository.All().Should().Contain(x => x.CatalogEntryId == entry.Id && x.HashType == "crc32" && x.HashValue == "1234ABCD");
        }

        [Test]
        public void sync_should_enrich_entries_with_datomatic_numbered_filename_by_hash()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro Nintendo DS",
                SourceUrl = "https://example.invalid/nds.dat"
            });

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.Fetch(source.SourceUrl))
                .Returns("<datafile><header><name>Nintendo - Nintendo DS</name><version>2026.05.02</version></header><game name='Mario Kart DS (Europe) (En,Fr,De,Es,It)'><rom name='Mario Kart DS (Europe) (En,Fr,De,Es,It).nds' crc='94E8127E' sha1='CE97D9B43F0D3CA0D48B781983E8A16F6393378F' status='verified' /></game></datafile>");

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.FetchDatOMaticNumbered(28))
                .Returns("<datafile><header><name>Nintendo - Nintendo DS</name></header><game name='0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It)' id='0201'><rom name='0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It).nds' crc='94E8127E' sha1='CE97D9B43F0D3CA0D48B781983E8A16F6393378F' status='verified' /></game></datafile>");

            _subject.Sync(source.Id);

            var entry = _entryRepository.All().Should().ContainSingle().Subject;
            entry.CanonicalName.Should().Be("Mario Kart DS (Europe) (En,Fr,De,Es,It)");
            entry.CanonicalFileName.Should().Be("Mario Kart DS (Europe) (En,Fr,De,Es,It).nds");
            entry.ReleaseNumber.Should().Be("0201");
            entry.NumberedCanonicalFileName.Should().Be("0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It).nds");
        }

        [Test]
        public void sync_should_fallback_to_advanscene_release_number_when_datomatic_fails()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro Nintendo DS",
                SourceUrl = "https://example.invalid/nds.dat"
            });

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.Fetch(source.SourceUrl))
                .Returns("<datafile><header><name>Nintendo - Nintendo DS</name><version>2026.05.02</version></header><game name='Mario Kart DS (Europe) (En,Fr,De,Es,It)'><rom name='Mario Kart DS (Europe) (En,Fr,De,Es,It).nds' crc='94E8127E' sha1='CE97D9B43F0D3CA0D48B781983E8A16F6393378F' status='verified' /></game></datafile>");

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.FetchDatOMaticNumbered(28))
                .Throws(new InvalidOperationException("DAT-o-MATIC did not return a numbered DAT download token"));

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.FetchAdvanscene("https://advanscene.com/offline/datas/ADVANsCEne_NDS_S.zip"))
                .Returns("<releases><game><releaseNumber>201</releaseNumber><files><romCRC extension='.nds'>94E8127E</romCRC></files></game></releases>");

            _subject.Sync(source.Id);

            var entry = _entryRepository.All().Should().ContainSingle().Subject;
            entry.ReleaseNumber.Should().Be("0201");
            entry.NumberedCanonicalFileName.Should().Be("0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It).nds");
        }

        [Test]
        public void sync_should_seed_missing_default_game_boy_sources()
        {
            _sourceRepository.Purge();

            var existingSource = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro Nintendo DS",
                SourceUrl = "https://raw.githubusercontent.com/libretro/libretro-database/master/metadat/no-intro/Nintendo%20-%20Nintendo%20DS.dat"
            });

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.Fetch(It.IsAny<string>()))
                .Returns("<datafile><header><name>Nintendo - Nintendo DS</name></header></datafile>");

            _subject.Sync();

            var sources = _sourceRepository.All().ToList();
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo Game Boy");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo Game Boy Color");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo Game Boy Advance");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo DS Download Play");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo 3DS");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo 3DS Digital");
            sources.Should().Contain(x => x.Name == "No-Intro New Nintendo 3DS");
            sources.Should().Contain(x => x.Name == "No-Intro New Nintendo 3DS Digital");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo DS DSvision SD Cards" && x.SourceUrl == "datomatic://system/319");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo Game Boy Advance Multiboot" && x.SourceUrl == "datomatic://system/137");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo Game Boy Advance e-Reader" && x.SourceUrl == "datomatic://system/41");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo Game Boy Advance Play-Yan" && x.SourceUrl == "datomatic://system/148");
            sources.Should().Contain(x => x.Name == "No-Intro Nintendo Game Boy Advance Video" && x.SourceUrl == "datomatic://system/297");
            sources.Should().ContainSingle(x => x.SourceUrl == existingSource.SourceUrl);
        }

        [Test]
        public void sync_should_map_3ds_sources_to_nintendo_3ds_platform()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro Nintendo 3DS",
                SourceUrl = "https://example.invalid/3ds.dat"
            });

            Mocker.GetMock<INoIntroCatalogDocumentClient>()
                .Setup(x => x.Fetch(source.SourceUrl))
                .Returns("<datafile><header><name>Nintendo - Nintendo 3DS</name><version>2026.07</version></header><game name='Mario Kart 7 (USA) (En,Fr,Es)'><rom name='Mario Kart 7 (USA) (En,Fr,Es).3ds' crc='ED9D4190' sha1='858F85EF67DD996A9F8F56B84D1C2CC4EE369DDE' status='verified' /></game></datafile>");

            _subject.Sync(source.Id);

            var entry = _entryRepository.All().Should().ContainSingle().Subject;
            entry.SystemKey.Should().Be("nintendo---nintendo-3ds");
            entry.PlatformFamily.Should().Be(PlatformFamily.Nintendo3DS);
            entry.CanonicalFileName.Should().Be("Mario Kart 7 (USA) (En,Fr,Es).3ds");
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
