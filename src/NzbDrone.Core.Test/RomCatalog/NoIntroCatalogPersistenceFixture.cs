using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.RomCatalog
{
    [TestFixture]
    public class NoIntroCatalogPersistenceFixture : DbTest
    {
        [Test]
        public void should_persist_generic_nointro_catalog_models_without_gamefile_semantics()
        {
            var sourceRepository = Mocker.Resolve<NoIntroCatalogSourceRepository>();
            var entryRepository = Mocker.Resolve<NoIntroCatalogEntryRepository>();
            var resultRepository = Mocker.Resolve<NoIntroVerificationResultRepository>();

            var source = sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro",
                SourceUrl = "https://example.invalid/nointro.dat",
                PinnedRevision = "rev-1",
                CatalogVersion = "2026-07-01"
            });

            var entry = entryRepository.Insert(new NoIntroCatalogEntry
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                CanonicalName = "F-Zero for Game Boy Advance (Japan)",
                CanonicalFileName = "0001 - F-Zero for Game Boy Advance (Japan).zip"
            });

            var result = resultRepository.Insert(new NoIntroVerificationResult
            {
                SnapshotId = 1,
                VerificationSetId = 1,
                CatalogEntryId = entry.Id,
                RelativePath = "GBA (by-id)/0001 - F-Zero for Game Boy Advance (Japan).zip",
                ActualFileName = "0001 - F-Zero for Game Boy Advance (Japan).zip",
                ExpectedFileName = "0001 - F-Zero for Game Boy Advance (Japan).zip",
                HashType = "sha1",
                HashValue = "abc123",
                VerificationStatus = NoIntroVerificationStatus.Verified,
                IsDuplicate = false,
                IsMissing = false,
                VerifiedAt = DateTime.UtcNow
            });

            source.Id.Should().BeGreaterThan(0);
            entry.Id.Should().BeGreaterThan(0);
            result.Id.Should().BeGreaterThan(0);
            result.CatalogEntryId.Should().Be(entry.Id);
        }
    }
}
