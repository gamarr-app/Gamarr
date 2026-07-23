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
    public class NoIntroEndToEndFixture : DbTest
    {
        private NoIntroCatalogSourceRepository _sourceRepository;
        private NoIntroCatalogEntryRepository _entryRepository;
        private NoIntroCatalogHashRepository _hashRepository;
        private NoIntroVerificationSetRepository _verificationSetRepository;
        private NoIntroVerificationResultRepository _resultRepository;
        private NoIntroVerificationService _verificationService;
        private NoIntroComponentClassifier _componentClassifier;
        private string _tempRoot;

        [SetUp]
        public void Setup()
        {
            _sourceRepository = Mocker.Resolve<NoIntroCatalogSourceRepository>();
            _entryRepository = Mocker.Resolve<NoIntroCatalogEntryRepository>();
            _hashRepository = Mocker.Resolve<NoIntroCatalogHashRepository>();
            _verificationSetRepository = Mocker.Resolve<NoIntroVerificationSetRepository>();
            _resultRepository = Mocker.Resolve<NoIntroVerificationResultRepository>();
            _componentClassifier = new NoIntroComponentClassifier();

            Mocker.GetMock<INamingConfigService>()
                  .Setup(x => x.GetConfig())
                  .Returns(new NamingConfig { RenameProfile = RenameProfile.NoIntroPreserveById });

            _verificationService = Mocker.Resolve<NoIntroVerificationService>();
            _tempRoot = Path.Combine(TempFolder, "nointro-e2e");
            Directory.CreateDirectory(_tempRoot);
        }

        [Test]
        public void EndToEndNoIntro_should_validate_gba_nds_components_and_standalone_products()
        {
            var source = _sourceRepository.Insert(new NoIntroCatalogSource
            {
                Name = "No-Intro",
                SourceUrl = "https://example.invalid/nointro.dat",
                CatalogVersion = "fixture"
            });

            var fzeroBytes = new byte[] { 1, 2, 3, 4 };
            var marioKartBytes = new byte[] { 5, 6, 7, 8 };
            var fzero = InsertEntry(source.Id, "nintendo-gba", "F-Zero for Game Boy Advance (Japan)", "F-Zero for Game Boy Advance (Japan).zip");
            var marioKart = InsertEntry(source.Id, "nintendo-ds", "Mario Kart DS (USA)", "Mario Kart DS (USA).nds");

            InsertHash(fzero.Id, fzeroBytes);
            InsertHash(marioKart.Id, marioKartBytes);
            InsertEntry(source.Id, "nintendo-ds", "Mario Kart DS (Download Play)", "Mario Kart DS (Download Play).nds", "Mario Kart DS");
            InsertEntry(source.Id, "nintendo-gba", "Pokemon Emerald Version (Germany)", "Pokemon Emerald Version (Germany).zip");
            InsertEntry(source.Id, "nintendo-gba", "Game Boy Advance Video - Cartoon Network Collection - Volume 1 (USA)", "Game Boy Advance Video - Cartoon Network Collection - Volume 1 (USA).zip");

            var verificationSet = _verificationSetRepository.Insert(new NoIntroVerificationSet
            {
                CatalogSourceId = source.Id,
                SystemKey = "mixed-fixture",
                RootPath = _tempRoot,
                Enabled = true
            });

            var fzeroZip = WriteZip("Nintendo/Game Boy Advance/GBA (by-id)/0001 - F-Zero for Game Boy Advance (Japan).zip", "F-Zero for Game Boy Advance (Japan).gba", fzeroBytes);
            var marioKartRaw = WriteBytes("Nintendo/Nintendo DS/nds/Mario Kart DS (USA).nds", marioKartBytes);

            _verificationService.Verify(verificationSet.Id, new[] { fzeroZip, marioKartRaw });
            var results = _resultRepository.All().ToList();
            var plan = _componentClassifier.BuildCatalogPlan(_entryRepository.GetBySourceId(source.Id));

            results.Should().Contain(x => x.CatalogEntryId == fzero.Id && x.VerificationStatus == NoIntroVerificationStatus.Verified);
            results.Should().Contain(x => x.CatalogEntryId == marioKart.Id && x.VerificationStatus == NoIntroVerificationStatus.Verified);
            plan.Games.Should().ContainSingle(x => x.GameTitle == "Mario Kart DS")
                .Subject.DownloadPlayComponents.Should().ContainSingle(x => x.SlotLabel == "Download Play");
            plan.Games.Should().ContainSingle(x => x.GameTitle == "Pokemon Emerald Version")
                .Subject.RegionLanguageComponents.Should().ContainSingle(x => x.SlotLabel == "Germany");
            plan.StandaloneGames.Should().ContainSingle(x => x.Title.StartsWith("Game Boy Advance Video", StringComparison.Ordinal));
        }

        private NoIntroCatalogEntry InsertEntry(int sourceId, string systemKey, string canonicalName, string fileName)
        {
            return _entryRepository.Insert(new NoIntroCatalogEntry
            {
                CatalogSourceId = sourceId,
                SystemKey = systemKey,
                CanonicalName = canonicalName,
                CanonicalFileName = fileName,
                PlatformFamily = PlatformFamily.Nintendo
            });
        }

        private NoIntroCatalogEntry InsertEntry(int sourceId, string systemKey, string canonicalName, string fileName, string parentCanonicalName)
        {
            var entry = InsertEntry(sourceId, systemKey, canonicalName, fileName);
            entry.ParentCanonicalName = parentCanonicalName;
            return _entryRepository.Update(entry);
        }

        private void InsertHash(int entryId, byte[] content)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();

            _hashRepository.Insert(new NoIntroCatalogHash
            {
                CatalogEntryId = entryId,
                HashType = "sha1",
                HashValue = BitConverter.ToString(sha1.ComputeHash(content)).Replace("-", string.Empty),
                IsPrimary = true,
                IsBadDump = false
            });
        }

        private string WriteBytes(string relativePath, byte[] content)
        {
            var path = Path.Combine(_tempRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, content);
            return path;
        }

        private string WriteZip(string relativePath, string memberName, byte[] content)
        {
            var path = Path.Combine(_tempRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            var entry = archive.CreateEntry(memberName);
            using var stream = entry.Open();
            stream.Write(content, 0, content.Length);

            return path;
        }
    }
}
