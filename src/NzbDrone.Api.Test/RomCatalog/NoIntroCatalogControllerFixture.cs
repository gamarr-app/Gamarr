using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Gamarr.Api.V3.RomCatalog;
using NzbDrone.Core.Games;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Test.Common;

namespace NzbDrone.Api.Test.RomCatalog
{
    [TestFixture]
    public class NoIntroCatalogControllerFixture : TestBase<NoIntroCatalogController>
    {
        [Test]
        public void NoIntroApi_should_surface_catalog_source_metadata_and_verification_rows()
        {
            Mocker.GetMock<INoIntroCatalogSourceRepository>()
                .Setup(x => x.All())
                .Returns(new List<NoIntroCatalogSource>
                {
                    new NoIntroCatalogSource
                    {
                        Id = 7,
                        Name = "No-Intro",
                        SourceUrl = "https://example.invalid/gba.dat",
                        CatalogVersion = "2026-07-18",
                        LastSuccessfulSync = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc)
                    }
                });

            Mocker.GetMock<INoIntroVerificationResultRepository>()
                .Setup(x => x.All())
                .Returns(new List<NoIntroVerificationResult>
                {
                    new NoIntroVerificationResult
                    {
                        Id = 11,
                        SnapshotId = 2,
                        VerificationSetId = 3,
                        ActualFileName = "0001 - F-Zero.zip",
                        ExpectedFileName = "F-Zero.zip",
                        VerificationStatus = NoIntroVerificationStatus.NameMismatch,
                        IsDuplicate = true,
                        VerifiedAt = new DateTime(2026, 7, 18, 1, 0, 0, DateTimeKind.Utc)
                    }
                });

            Subject.GetSources().Should().ContainSingle(x => x.Id == 7 && x.CatalogVersion == "2026-07-18" && x.LastSuccessfulSync.HasValue);
            Subject.GetVerificationResults().Should().ContainSingle(x => x.Id == 11 && x.VerificationStatus == NoIntroVerificationStatus.NameMismatch && x.IsDuplicate);
        }

        [Test]
        public void NoIntroApi_should_surface_region_download_play_and_standalone_component_plan()
        {
            var entries = new List<NoIntroCatalogEntry>
            {
                Entry(1, "nintendo-ds", "Mario Kart DS (USA)"),
                ChildEntry(1, "nintendo-ds", "Mario Kart DS (Download Play)", "Mario Kart DS"),
                Entry(1, "nintendo-ds", "Mario Party DS (Download Play)"),
                Entry(1, "nintendo-gba", "Pokemon Emerald Version (Germany)")
            };

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                .Setup(x => x.GetBySourceId(1))
                .Returns(entries);

            Mocker.GetMock<INoIntroComponentClassifier>()
                .Setup(x => x.BuildCatalogPlan(entries))
                .Returns(new NoIntroComponentClassifier().BuildCatalogPlan(entries));

            var plan = Subject.GetComponentPlan(1);

            plan.Games.Should().ContainSingle(x => x.GameTitle == "Mario Kart DS")
                .Subject.DownloadPlayComponents.Should().ContainSingle(x => x.SlotLabel == "Download Play");
            plan.Games.Should().ContainSingle(x => x.GameTitle == "Pokemon Emerald Version")
                .Subject.RegionLanguageComponents.Should().ContainSingle(x => x.SlotLabel == "Germany");
            plan.StandaloneGames.Should().ContainSingle(x => x.Title == "Mario Party DS (Download Play)" && x.ComponentType == NoIntroRomComponentType.Multiboot);
        }

        private static NoIntroCatalogEntry Entry(int sourceId, string systemKey, string canonicalName)
        {
            return new NoIntroCatalogEntry
            {
                CatalogSourceId = sourceId,
                SystemKey = systemKey,
                CanonicalName = canonicalName,
                CanonicalFileName = $"{canonicalName}.zip",
                PlatformFamily = PlatformFamily.Nintendo
            };
        }

        private static NoIntroCatalogEntry ChildEntry(int sourceId, string systemKey, string canonicalName, string parentCanonicalName)
        {
            var entry = Entry(sourceId, systemKey, canonicalName);
            entry.ParentCanonicalName = parentCanonicalName;
            return entry;
        }
    }
}
