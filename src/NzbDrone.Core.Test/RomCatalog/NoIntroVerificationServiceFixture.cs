using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.RomCatalog
{
    [TestFixture]
    public class NoIntroVerificationServiceFixture : DbTest
    {
        private NoIntroCatalogSourceRepository _sourceRepository;
        private NoIntroCatalogEntryRepository _entryRepository;
        private NoIntroCatalogHashRepository _hashRepository;
        private NoIntroVerificationSetRepository _verificationSetRepository;
        private NoIntroVerificationResultRepository _resultRepository;
        private NoIntroVerificationService _subject;
        private string _tempRoot;

        [SetUp]
        public void Setup()
        {
            _sourceRepository = Mocker.Resolve<NoIntroCatalogSourceRepository>();
            _entryRepository = Mocker.Resolve<NoIntroCatalogEntryRepository>();
            _hashRepository = Mocker.Resolve<NoIntroCatalogHashRepository>();
            _verificationSetRepository = Mocker.Resolve<NoIntroVerificationSetRepository>();
            _resultRepository = Mocker.Resolve<NoIntroVerificationResultRepository>();
            Mocker.GetMock<INamingConfigService>()
                  .Setup(x => x.GetConfig())
                  .Returns(NamingConfig.Default);

            _subject = Mocker.Resolve<NoIntroVerificationService>();
            _tempRoot = Path.Combine(TempFolder, "nointro-roms");
            Directory.CreateDirectory(_tempRoot);
        }

        [Test]
        public void should_verify_raw_and_zip_roms_and_mark_duplicates_missing_and_bad_dumps()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro",
                SourceUrl = "https://example.invalid/catalog.dat"
            });

            var goodBytes = new byte[] { 1, 2, 3, 4 };
            var badBytes = new byte[] { 9, 9, 9, 9 };

            var verifiedEntry = _entryRepository.Insert(new NoIntroCatalogEntry
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                CanonicalName = "Verified Game",
                CanonicalFileName = "Verified Game.gba",
                PlatformFamily = PlatformFamily.Nintendo
            });

            InsertSha1Hash(verifiedEntry.Id, goodBytes, false);

            var badDumpEntry = _entryRepository.Insert(new NoIntroCatalogEntry
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                CanonicalName = "Bad Dump Game",
                CanonicalFileName = "Bad Dump Game.gba",
                PlatformFamily = PlatformFamily.Nintendo
            });

            InsertSha1Hash(badDumpEntry.Id, badBytes, true);

            var missingEntry = _entryRepository.Insert(new NoIntroCatalogEntry
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                CanonicalName = "Missing Game",
                CanonicalFileName = "Missing Game.gba",
                PlatformFamily = PlatformFamily.Nintendo
            });

            InsertSha1Hash(missingEntry.Id, new byte[] { 7, 7, 7, 7 }, false);

            var verificationSet = _verificationSetRepository.Insert(new NoIntroVerificationSet
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                RootPath = _tempRoot,
                Enabled = true
            });

            var rawPath = WriteBytes("Verified Game.gba", goodBytes);
            var mismatchZip = WriteZip("0001 - Verified Game.zip", "Verified Game.gba", goodBytes);
            var badDumpPath = WriteBytes("Bad Dump Game.gba", badBytes);
            var unknownPath = WriteBytes("Unknown Game.gba", new byte[] { 5, 6, 7, 8 });
            var ambiguousZip = WriteMultiZip("Ambiguous.zip", ("one.gba", goodBytes), ("two.gba", badBytes));

            _subject.Verify(verificationSet.Id, new[] { rawPath, mismatchZip, badDumpPath, unknownPath, ambiguousZip });

            var results = _resultRepository.All().ToList();

            results.Should().Contain(x => x.ActualFileName == "Verified Game.gba" && x.VerificationStatus == NoIntroVerificationStatus.Verified && x.IsDuplicate);
            results.Should().Contain(x => x.ActualFileName == "0001 - Verified Game.zip" && x.VerificationStatus == NoIntroVerificationStatus.NameMismatch && x.IsDuplicate);
            results.Should().Contain(x => x.ActualFileName == "Bad Dump Game.gba" && x.VerificationStatus == NoIntroVerificationStatus.BadDump);
            results.Should().Contain(x => x.ActualFileName == "Unknown Game.gba" && x.VerificationStatus == NoIntroVerificationStatus.Unknown && !x.IsMissing);
            results.Should().Contain(x => x.ActualFileName == "Ambiguous.zip" && x.VerificationStatus == NoIntroVerificationStatus.Unknown && x.MemberPath == null);
            results.Should().Contain(x => x.CatalogEntryId == missingEntry.Id && x.IsMissing);
        }

        private void InsertSha1Hash(int entryId, byte[] content, bool isBadDump)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = BitConverter.ToString(sha1.ComputeHash(content)).Replace("-", string.Empty);

            _hashRepository.Insert(new NoIntroCatalogHash
            {
                CatalogEntryId = entryId,
                HashType = "sha1",
                HashValue = hash,
                IsPrimary = true,
                IsBadDump = isBadDump
            });
        }

        private string WriteBytes(string fileName, byte[] content)
        {
            var path = Path.Combine(_tempRoot, fileName);
            File.WriteAllBytes(path, content);
            return path;
        }

        private string WriteZip(string archiveName, string memberName, byte[] content)
        {
            var path = Path.Combine(_tempRoot, archiveName);

            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            var entry = archive.CreateEntry(memberName);
            using var stream = entry.Open();
            stream.Write(content, 0, content.Length);

            return path;
        }

        private string WriteMultiZip(string archiveName, params (string Name, byte[] Content)[] members)
        {
            var path = Path.Combine(_tempRoot, archiveName);

            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

            foreach (var member in members)
            {
                var entry = archive.CreateEntry(member.Name);
                using var stream = entry.Open();
                stream.Write(member.Content, 0, member.Content.Length);
            }

            return path;
        }
    }
}
