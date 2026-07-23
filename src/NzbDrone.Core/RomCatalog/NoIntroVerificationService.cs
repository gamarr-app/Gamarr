using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.RomCatalog
{
    public interface INoIntroVerificationService
    {
        NoIntroVerificationSnapshot Verify(int verificationSetId, IEnumerable<string> filePaths);
    }

    public class NoIntroVerificationService : INoIntroVerificationService
    {
        private readonly NoIntroVerificationSetRepository _verificationSetRepository;
        private readonly NoIntroVerificationSnapshotRepository _snapshotRepository;
        private readonly NoIntroVerificationResultRepository _resultRepository;
        private readonly NoIntroCatalogEntryRepository _entryRepository;
        private readonly NoIntroCatalogHashRepository _hashRepository;
        private readonly INamingConfigService _namingConfigService;

        public NoIntroVerificationService(
            NoIntroVerificationSetRepository verificationSetRepository,
            NoIntroVerificationSnapshotRepository snapshotRepository,
            NoIntroVerificationResultRepository resultRepository,
            NoIntroCatalogEntryRepository entryRepository,
            NoIntroCatalogHashRepository hashRepository,
            INamingConfigService namingConfigService)
        {
            _verificationSetRepository = verificationSetRepository;
            _snapshotRepository = snapshotRepository;
            _resultRepository = resultRepository;
            _entryRepository = entryRepository;
            _hashRepository = hashRepository;
            _namingConfigService = namingConfigService;
        }

        public NoIntroVerificationSnapshot Verify(int verificationSetId, IEnumerable<string> filePaths)
        {
            var verificationSet = _verificationSetRepository.Get(verificationSetId);
            var snapshot = _snapshotRepository.Insert(new NoIntroVerificationSnapshot
            {
                VerificationSetId = verificationSet.Id,
                CatalogSourceId = verificationSet.CatalogSourceId,
                CatalogRevision = string.Empty,
                StartedAt = DateTime.UtcNow
            });

            var entries = _entryRepository.GetBySourceId(verificationSet.CatalogSourceId);
            var entryIds = entries.Select(x => x.Id).ToList();
            var hashes = _hashRepository.GetByEntryIds(entryIds);
            var renameProfile = _namingConfigService.GetConfig().RenameProfile;
            var hashMap = hashes.GroupBy(x => $"{x.HashType}:{x.HashValue}".ToLowerInvariant())
                .ToDictionary(x => x.Key, x => x.First());
            var entryMap = entries.ToDictionary(x => x.Id);

            var results = filePaths.Select(path => VerifyPath(snapshot, verificationSet, path, hashMap, entryMap, renameProfile)).ToList();
            MarkDuplicates(results);
            results.AddRange(BuildMissingResults(snapshot, verificationSet, entries, results));

            _resultRepository.InsertMany(results);

            snapshot.CompletedAt = DateTime.UtcNow;
            return _snapshotRepository.Update(snapshot);
        }

        private static NoIntroVerificationResult VerifyPath(
            NoIntroVerificationSnapshot snapshot,
            NoIntroVerificationSet verificationSet,
            string path,
            Dictionary<string, NoIntroCatalogHash> hashMap,
            Dictionary<int, NoIntroCatalogEntry> entryMap,
            RenameProfile renameProfile)
        {
            return Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                ? VerifyArchive(snapshot, verificationSet, path, hashMap, entryMap, renameProfile)
                : VerifyFile(snapshot, verificationSet, path, hashMap, entryMap, renameProfile);
        }

        private static NoIntroVerificationResult VerifyFile(
            NoIntroVerificationSnapshot snapshot,
            NoIntroVerificationSet verificationSet,
            string path,
            Dictionary<string, NoIntroCatalogHash> hashMap,
            Dictionary<int, NoIntroCatalogEntry> entryMap,
            RenameProfile renameProfile)
        {
            using var stream = File.OpenRead(path);
            var hashes = ComputeHashes(stream);
            return BuildMatchedResult(snapshot, verificationSet, path, null, null, Path.GetFileName(path), hashes, hashMap, entryMap, renameProfile);
        }

        private static NoIntroVerificationResult VerifyArchive(
            NoIntroVerificationSnapshot snapshot,
            NoIntroVerificationSet verificationSet,
            string path,
            Dictionary<string, NoIntroCatalogHash> hashMap,
            Dictionary<int, NoIntroCatalogEntry> entryMap,
            RenameProfile renameProfile)
        {
            using var fileStream = File.OpenRead(path);
            using var zipFile = new ZipFile(fileStream);
            var fileEntries = zipFile.Cast<ZipEntry>().Where(x => x.IsFile).ToList();

            if (fileEntries.Count != 1)
            {
                return new NoIntroVerificationResult
                {
                    SnapshotId = snapshot.Id,
                    VerificationSetId = verificationSet.Id,
                    CatalogEntryId = null,
                    RelativePath = GetRelativePath(verificationSet.RootPath, path),
                    ArchivePath = GetRelativePath(verificationSet.RootPath, path),
                    MemberPath = null,
                    ActualFileName = Path.GetFileName(path),
                    ExpectedFileName = null,
                    HashType = null,
                    HashValue = null,
                    VerificationStatus = NoIntroVerificationStatus.Unknown,
                    IsDuplicate = false,
                    IsMissing = false,
                    VerifiedAt = DateTime.UtcNow
                };
            }

            var entry = fileEntries[0];
            using var memberStream = zipFile.GetInputStream(entry);
            var hashes = ComputeHashes(memberStream);

            return BuildMatchedResult(snapshot, verificationSet, path, GetRelativePath(verificationSet.RootPath, path), entry.Name, Path.GetFileName(path), hashes, hashMap, entryMap, renameProfile);
        }

        private static NoIntroVerificationResult BuildMatchedResult(
            NoIntroVerificationSnapshot snapshot,
            NoIntroVerificationSet verificationSet,
            string fullPath,
            string archivePath,
            string memberPath,
            string actualFileName,
            NoIntroHashTriplet hashes,
            Dictionary<string, NoIntroCatalogHash> hashMap,
            Dictionary<int, NoIntroCatalogEntry> entryMap,
            RenameProfile renameProfile)
        {
            var matchedHash = FindMatch(hashes, hashMap);

            if (matchedHash == null)
            {
                return new NoIntroVerificationResult
                {
                    SnapshotId = snapshot.Id,
                    VerificationSetId = verificationSet.Id,
                    RelativePath = GetRelativePath(verificationSet.RootPath, fullPath),
                    ArchivePath = archivePath,
                    MemberPath = memberPath,
                    ActualFileName = actualFileName,
                    ExpectedFileName = null,
                    HashType = hashes.PreferredHashType,
                    HashValue = hashes.PreferredHashValue,
                    VerificationStatus = NoIntroVerificationStatus.Unknown,
                    IsDuplicate = false,
                    IsMissing = false,
                    VerifiedAt = DateTime.UtcNow
                };
            }

            var catalogEntry = entryMap[matchedHash.CatalogEntryId];
            var expectedFileName = NoIntroRenameProfileEvaluator.GetExpectedFileName(catalogEntry, actualFileName, renameProfile);
            var verificationStatus = matchedHash.IsBadDump
                ? NoIntroVerificationStatus.BadDump
                : NoIntroRenameProfileEvaluator.MatchesProfile(catalogEntry, actualFileName, renameProfile)
                    ? NoIntroVerificationStatus.Verified
                    : NoIntroVerificationStatus.NameMismatch;

            return new NoIntroVerificationResult
            {
                SnapshotId = snapshot.Id,
                VerificationSetId = verificationSet.Id,
                CatalogEntryId = catalogEntry.Id,
                RelativePath = GetRelativePath(verificationSet.RootPath, fullPath),
                ArchivePath = archivePath,
                MemberPath = memberPath,
                ActualFileName = actualFileName,
                ExpectedFileName = expectedFileName,
                HashType = matchedHash.HashType,
                HashValue = matchedHash.HashValue,
                VerificationStatus = verificationStatus,
                IsDuplicate = false,
                IsMissing = false,
                VerifiedAt = DateTime.UtcNow
            };
        }

        private static NoIntroCatalogHash FindMatch(NoIntroHashTriplet hashes, Dictionary<string, NoIntroCatalogHash> hashMap)
        {
            return TryGetHash("sha1", hashes.Sha1, hashMap) ??
                   TryGetHash("md5", hashes.Md5, hashMap) ??
                   TryGetHash("crc32", hashes.Crc32, hashMap);
        }

        private static NoIntroCatalogHash TryGetHash(string hashType, string hashValue, Dictionary<string, NoIntroCatalogHash> hashMap)
        {
            if (string.IsNullOrWhiteSpace(hashValue))
            {
                return null;
            }

            hashMap.TryGetValue($"{hashType}:{hashValue}".ToLowerInvariant(), out var matchedHash);
            return matchedHash;
        }

        private static void MarkDuplicates(List<NoIntroVerificationResult> results)
        {
            var duplicates = results.Where(x => x.CatalogEntryId.HasValue).GroupBy(x => x.CatalogEntryId.Value).Where(x => x.Count() > 1);

            foreach (var duplicateGroup in duplicates)
            {
                foreach (var result in duplicateGroup)
                {
                    result.IsDuplicate = true;
                }
            }
        }

        private static List<NoIntroVerificationResult> BuildMissingResults(
            NoIntroVerificationSnapshot snapshot,
            NoIntroVerificationSet verificationSet,
            List<NoIntroCatalogEntry> entries,
            List<NoIntroVerificationResult> results)
        {
            var matchedEntryIds = results.Where(x => x.CatalogEntryId.HasValue).Select(x => x.CatalogEntryId.Value).ToHashSet();

            return entries.Where(entry => !matchedEntryIds.Contains(entry.Id)).Select(entry => new NoIntroVerificationResult
            {
                SnapshotId = snapshot.Id,
                VerificationSetId = verificationSet.Id,
                CatalogEntryId = entry.Id,
                RelativePath = string.Empty,
                ArchivePath = null,
                MemberPath = null,
                ActualFileName = string.Empty,
                ExpectedFileName = entry.CanonicalFileName,
                HashType = null,
                HashValue = null,
                VerificationStatus = NoIntroVerificationStatus.Unknown,
                IsDuplicate = false,
                IsMissing = true,
                VerifiedAt = DateTime.UtcNow
            }).ToList();
        }

        private static string GetRelativePath(string rootPath, string fullPath)
        {
            return fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : fullPath;
        }

        private static NoIntroHashTriplet ComputeHashes(Stream stream)
        {
            return NoIntroRomHasher.Compute(stream);
        }
    }
}
