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
    public class NoIntroVerificationRenameProfileFixture : DbTest
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
            _subject = Mocker.Resolve<NoIntroVerificationService>();
            _tempRoot = Path.Combine(TempFolder, "rename-profile-roms");
            Directory.CreateDirectory(_tempRoot);
        }

        [Test]
        public void RenameProfile_should_keep_by_id_file_verified_when_preserve_profile_is_selected()
        {
            SetRenameProfile(RenameProfile.NoIntroPreserveById);
            var verificationSet = CreateVerificationSet();
            var zipPath = CreateVerifiedZip("0001 - F-Zero for Game Boy Advance (Japan).zip");

            _subject.Verify(verificationSet.Id, new[] { zipPath });

            var result = _resultRepository.All().Single(x => !x.IsMissing);
            result.VerificationStatus.Should().Be(NoIntroVerificationStatus.Verified);
            result.ExpectedFileName.Should().Be("0001 - F-Zero for Game Boy Advance (Japan).zip");
        }

        [Test]
        public void RenameProfile_should_mark_same_by_id_file_as_mismatch_when_canonical_profile_is_selected()
        {
            SetRenameProfile(RenameProfile.NoIntroCanonical);
            var verificationSet = CreateVerificationSet();
            var zipPath = CreateVerifiedZip("0001 - F-Zero for Game Boy Advance (Japan).zip");

            _subject.Verify(verificationSet.Id, new[] { zipPath });

            var result = _resultRepository.All().Single(x => !x.IsMissing);
            result.VerificationStatus.Should().Be(NoIntroVerificationStatus.NameMismatch);
            result.ExpectedFileName.Should().Be("F-Zero for Game Boy Advance (Japan).zip");
        }

        [Test]
        public void RenameProfile_should_keep_existing_default_behavior_under_normal_gamarr_profile()
        {
            SetRenameProfile(RenameProfile.Gamarr);
            var verificationSet = CreateVerificationSet();
            var zipPath = CreateVerifiedZip("0001 - F-Zero for Game Boy Advance (Japan).zip");

            _subject.Verify(verificationSet.Id, new[] { zipPath });

            var result = _resultRepository.All().Single(x => !x.IsMissing);
            result.VerificationStatus.Should().Be(NoIntroVerificationStatus.NameMismatch);
            result.ExpectedFileName.Should().Be("F-Zero for Game Boy Advance (Japan).zip");
        }

        [Test]
        public void RenameProfileFailure_should_not_mark_by_id_file_as_name_mismatch_when_preserve_profile_is_selected()
        {
            SetRenameProfile(RenameProfile.NoIntroPreserveById);
            var verificationSet = CreateVerificationSet();
            var zipPath = CreateVerifiedZip("0001 - F-Zero for Game Boy Advance (Japan).zip");

            _subject.Verify(verificationSet.Id, new[] { zipPath });

            var result = _resultRepository.All().Single(x => !x.IsMissing);
            result.VerificationStatus.Should().NotBe(NoIntroVerificationStatus.NameMismatch);
            result.ExpectedFileName.Should().Be("0001 - F-Zero for Game Boy Advance (Japan).zip");
        }

        private void SetRenameProfile(RenameProfile renameProfile)
        {
            Mocker.GetMock<INamingConfigService>()
                  .Setup(x => x.GetConfig())
                  .Returns(new NamingConfig
                  {
                      RenameProfile = renameProfile
                  });
        }

        private NoIntroVerificationSet CreateVerificationSet()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro",
                SourceUrl = "https://example.invalid/catalog.dat"
            });

            var entry = _entryRepository.Insert(new NoIntroCatalogEntry
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                CanonicalName = "F-Zero for Game Boy Advance (Japan)",
                CanonicalFileName = "F-Zero for Game Boy Advance (Japan).zip",
                PlatformFamily = PlatformFamily.Nintendo
            });

            InsertSha1Hash(entry.Id, new byte[] { 1, 2, 3, 4 });

            return _verificationSetRepository.Insert(new NoIntroVerificationSet
            {
                CatalogSourceId = source.Id,
                SystemKey = "nintendo-gba",
                RootPath = _tempRoot,
                Enabled = true
            });
        }

        private void InsertSha1Hash(int entryId, byte[] content)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = System.BitConverter.ToString(sha1.ComputeHash(content)).Replace("-", string.Empty);

            _hashRepository.Insert(new NoIntroCatalogHash
            {
                CatalogEntryId = entryId,
                HashType = "sha1",
                HashValue = hash,
                IsPrimary = true,
                IsBadDump = false
            });
        }

        private string CreateVerifiedZip(string archiveName)
        {
            var path = Path.Combine(_tempRoot, archiveName);
            var content = new byte[] { 1, 2, 3, 4 };

            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            var entry = archive.CreateEntry("F-Zero.gba");
            using var stream = entry.Open();
            stream.Write(content, 0, content.Length);

            return path;
        }
    }
}
