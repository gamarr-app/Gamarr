using System;
using System.Collections.Generic;
using System.Linq;
using Gamarr.Http.REST;
using NzbDrone.Core.Games;
using NzbDrone.Core.RomCatalog;

namespace Gamarr.Api.V3.RomCatalog
{
    public class NoIntroCatalogSourceResource : RestResource
    {
        public string Name { get; set; }
        public string SourceUrl { get; set; }
        public string PinnedRevision { get; set; }
        public string CatalogVersion { get; set; }
        public DateTime? LastSuccessfulSync { get; set; }
        public DateTime? LastAttemptedSync { get; set; }
        public string LastSyncError { get; set; }
    }

    public class NoIntroCatalogEntryResource : RestResource
    {
        public int CatalogSourceId { get; set; }
        public string SystemKey { get; set; }
        public string CanonicalName { get; set; }
        public string ParentCanonicalName { get; set; }
        public string CanonicalFileName { get; set; }
        public PlatformFamily PlatformFamily { get; set; }
    }

    public class NoIntroCatalogPlanResource
    {
        public List<NoIntroCatalogGamePlanResource> Games { get; set; } = new List<NoIntroCatalogGamePlanResource>();
        public List<NoIntroCatalogStandalonePlanResource> StandaloneGames { get; set; } = new List<NoIntroCatalogStandalonePlanResource>();
    }

    public class NoIntroCatalogGamePlanResource
    {
        public string SystemKey { get; set; }
        public string GameTitle { get; set; }
        public List<NoIntroCatalogComponentSlotResource> RegionLanguageComponents { get; set; } = new List<NoIntroCatalogComponentSlotResource>();
        public List<NoIntroCatalogComponentSlotResource> DownloadPlayComponents { get; set; } = new List<NoIntroCatalogComponentSlotResource>();
    }

    public class NoIntroCatalogStandalonePlanResource
    {
        public string Title { get; set; }
        public NoIntroRomComponentType ComponentType { get; set; }
    }

    public class NoIntroCatalogComponentSlotResource
    {
        public string SlotLabel { get; set; }
        public string CanonicalName { get; set; }
        public NoIntroRomComponentType ComponentType { get; set; }
    }

    public class NoIntroVerificationResultResource : RestResource
    {
        public int SnapshotId { get; set; }
        public int VerificationSetId { get; set; }
        public int? CatalogEntryId { get; set; }
        public string RelativePath { get; set; }
        public string ArchivePath { get; set; }
        public string MemberPath { get; set; }
        public string ActualFileName { get; set; }
        public string ExpectedFileName { get; set; }
        public string HashType { get; set; }
        public string HashValue { get; set; }
        public NoIntroVerificationStatus VerificationStatus { get; set; }
        public bool IsDuplicate { get; set; }
        public bool IsMissing { get; set; }
        public DateTime VerifiedAt { get; set; }
    }

    public static class NoIntroCatalogResourceMapper
    {
        public static NoIntroCatalogSourceResource ToResource(this NoIntroCatalogSource model)
        {
            if (model == null)
            {
                return null;
            }

            return new NoIntroCatalogSourceResource
            {
                Id = model.Id,
                Name = model.Name,
                SourceUrl = model.SourceUrl,
                PinnedRevision = model.PinnedRevision,
                CatalogVersion = model.CatalogVersion,
                LastSuccessfulSync = model.LastSuccessfulSync,
                LastAttemptedSync = model.LastAttemptedSync,
                LastSyncError = model.LastSyncError
            };
        }

        public static NoIntroCatalogEntryResource ToResource(this NoIntroCatalogEntry model)
        {
            if (model == null)
            {
                return null;
            }

            return new NoIntroCatalogEntryResource
            {
                Id = model.Id,
                CatalogSourceId = model.CatalogSourceId,
                SystemKey = model.SystemKey,
                CanonicalName = model.CanonicalName,
                ParentCanonicalName = model.ParentCanonicalName,
                CanonicalFileName = model.CanonicalFileName,
                PlatformFamily = model.PlatformFamily
            };
        }

        public static NoIntroVerificationResultResource ToResource(this NoIntroVerificationResult model)
        {
            if (model == null)
            {
                return null;
            }

            return new NoIntroVerificationResultResource
            {
                Id = model.Id,
                SnapshotId = model.SnapshotId,
                VerificationSetId = model.VerificationSetId,
                CatalogEntryId = model.CatalogEntryId,
                RelativePath = model.RelativePath,
                ArchivePath = model.ArchivePath,
                MemberPath = model.MemberPath,
                ActualFileName = model.ActualFileName,
                ExpectedFileName = model.ExpectedFileName,
                HashType = model.HashType,
                HashValue = model.HashValue,
                VerificationStatus = model.VerificationStatus,
                IsDuplicate = model.IsDuplicate,
                IsMissing = model.IsMissing,
                VerifiedAt = model.VerifiedAt
            };
        }

        public static NoIntroCatalogPlanResource ToResource(this NoIntroCatalogPlan model)
        {
            if (model == null)
            {
                return null;
            }

            return new NoIntroCatalogPlanResource
            {
                Games = model.Games.Select(ToResource).ToList(),
                StandaloneGames = model.StandaloneGames.Select(ToResource).ToList()
            };
        }

        private static NoIntroCatalogGamePlanResource ToResource(this NoIntroCatalogGamePlan model)
        {
            return new NoIntroCatalogGamePlanResource
            {
                SystemKey = model.SystemKey,
                GameTitle = model.GameTitle,
                RegionLanguageComponents = model.RegionLanguageComponents.Select(ToResource).ToList(),
                DownloadPlayComponents = model.DownloadPlayComponents.Select(ToResource).ToList()
            };
        }

        private static NoIntroCatalogStandalonePlanResource ToResource(this NoIntroCatalogStandalonePlan model)
        {
            return new NoIntroCatalogStandalonePlanResource
            {
                Title = model.Title,
                ComponentType = model.ComponentType
            };
        }

        private static NoIntroCatalogComponentSlotResource ToResource(this NoIntroCatalogComponentSlot model)
        {
            return new NoIntroCatalogComponentSlotResource
            {
                SlotLabel = model.SlotLabel,
                CanonicalName = model.CanonicalName,
                ComponentType = model.ComponentType
            };
        }
    }
}
