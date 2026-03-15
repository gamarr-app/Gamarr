using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Backup;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Backup
{
    [TestFixture]
    public class BackupServiceFixture : CoreTest<BackupService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IAppFolderInfo>()
                .Setup(c => c.TempFolder)
                .Returns(Path.Combine(TempFolder, "temp"));

            Mocker.GetMock<IAppFolderInfo>()
                .SetupGet(c => c.AppDataFolder)
                .Returns(TempFolder);

            Mocker.GetMock<IConfigService>()
                .Setup(c => c.BackupFolder)
                .Returns("Backups");

            Mocker.GetMock<IConfigService>()
                .Setup(c => c.BackupRetention)
                .Returns(28);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderWritable(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Array.Empty<string>());

            Mocker.GetMock<IMainDatabase>()
                .Setup(c => c.DatabaseType)
                .Returns(DatabaseType.SQLite);
        }

        [Test]
        public void should_create_backup_in_correct_folder()
        {
            Subject.Backup(BackupType.Manual);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.EnsureFolder(It.Is<string>(s => s.Contains("manual"))), Times.Once());
        }

        [Test]
        public void should_create_backup_in_scheduled_folder()
        {
            Subject.Backup(BackupType.Scheduled);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.EnsureFolder(It.Is<string>(s => s.Contains("scheduled"))), Times.Once());
        }

        [Test]
        public void should_create_backup_in_update_folder()
        {
            Subject.Backup(BackupType.Update);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.EnsureFolder(It.Is<string>(s => s.Contains("update"))), Times.Once());
        }

        [Test]
        public void should_throw_if_backup_folder_not_writable()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderWritable(It.IsAny<string>()))
                .Returns(false);

            Assert.Throws<UnauthorizedAccessException>(() => Subject.Backup(BackupType.Manual));
        }

        [Test]
        public void should_backup_database()
        {
            Subject.Backup(BackupType.Manual);

            Mocker.GetMock<IMakeDatabaseBackup>()
                .Verify(c => c.BackupDatabase(It.IsAny<IMainDatabase>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_backup_config_file()
        {
            Subject.Backup(BackupType.Manual);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(c => c.TransferFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    TransferMode.Copy,
                    It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_create_zip_archive()
        {
            Subject.Backup(BackupType.Manual);

            Mocker.GetMock<IArchiveService>()
                .Verify(c => c.CreateZip(
                    It.Is<string>(s => s.Contains("gamarr_backup")),
                    It.IsAny<IEnumerable<string>>()), Times.Once());
        }

        [Test]
        public void should_cleanup_old_backups_for_scheduled()
        {
            var scheduledFolder = Path.Combine(TempFolder, "Backups", "scheduled");
            var oldBackupPath = Path.Combine(scheduledFolder, "gamarr_backup_v1.0.0_2020.01.01_00.00.00.zip");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(It.Is<string>(s => s.Contains("scheduled")), false))
                .Returns(new[] { oldBackupPath });

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileGetLastWrite(oldBackupPath))
                .Returns(DateTime.UtcNow.AddDays(-30));

            Subject.Backup(BackupType.Scheduled);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.DeleteFile(oldBackupPath), Times.Once());
        }

        [Test]
        public void should_not_cleanup_recent_backups()
        {
            var scheduledFolder = Path.Combine(TempFolder, "Backups", "scheduled");
            var recentBackupPath = Path.Combine(scheduledFolder, "gamarr_backup_v1.0.0_2024.01.01_00.00.00.zip");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(It.Is<string>(s => s.Contains("scheduled")), false))
                .Returns(new[] { recentBackupPath });

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileGetLastWrite(recentBackupPath))
                .Returns(DateTime.UtcNow.AddDays(-1));

            Subject.Backup(BackupType.Scheduled);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.DeleteFile(recentBackupPath), Times.Never());
        }

        [Test]
        public void restore_should_extract_zip_and_restore_files()
        {
            var backupFile = Path.Combine(TempFolder, "backup.zip");
            var extractPath = Path.Combine(TempFolder, "temp", "gamarr_backup_restore");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(extractPath, false))
                .Returns(new[]
                {
                    Path.Combine(extractPath, "Config.xml"),
                    Path.Combine(extractPath, "gamarr.db")
                });

            Subject.Restore(backupFile);

            Mocker.GetMock<IArchiveService>()
                .Verify(c => c.Extract(backupFile, extractPath), Times.Once());

            // Config.xml restored
            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.MoveFile(
                    It.Is<string>(s => s.Contains("Config.xml")),
                    It.IsAny<string>(),
                    true), Times.Once());

            // Database restored
            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.MoveFile(
                    It.Is<string>(s => s.Contains("gamarr.db")),
                    It.IsAny<string>(),
                    true), Times.Once());
        }

        [Test]
        public void restore_should_handle_legacy_nzbdrone_db()
        {
            var backupFile = Path.Combine(TempFolder, "backup.zip");
            var extractPath = Path.Combine(TempFolder, "temp", "gamarr_backup_restore");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(extractPath, false))
                .Returns(new[]
                {
                    Path.Combine(extractPath, "nzbdrone.db")
                });

            Subject.Restore(backupFile);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.MoveFile(
                    It.Is<string>(s => s.Contains("nzbdrone.db")),
                    It.IsAny<string>(),
                    true), Times.Once());
        }

        [Test]
        public void restore_should_throw_if_no_restorable_files()
        {
            var backupFile = Path.Combine(TempFolder, "backup.zip");
            var extractPath = Path.Combine(TempFolder, "temp", "gamarr_backup_restore");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(extractPath, false))
                .Returns(new[] { Path.Combine(extractPath, "random.txt") });

            Assert.Throws<RestoreBackupFailedException>(() => Subject.Restore(backupFile));
        }

        [Test]
        public void restore_should_cleanup_temp_folder()
        {
            var backupFile = Path.Combine(TempFolder, "backup.zip");
            var extractPath = Path.Combine(TempFolder, "temp", "gamarr_backup_restore");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(extractPath, false))
                .Returns(new[] { Path.Combine(extractPath, "Config.xml") });

            Subject.Restore(backupFile);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.DeleteFolder(extractPath, true), Times.Once());
        }

        [Test]
        public void restore_non_zip_should_move_as_database()
        {
            var dbFile = Path.Combine(TempFolder, "gamarr.db");

            Subject.Restore(dbFile);

            Mocker.GetMock<IDiskProvider>()
                .Verify(c => c.MoveFile(dbFile, It.IsAny<string>(), true), Times.Once());

            Mocker.GetMock<IArchiveService>()
                .Verify(c => c.Extract(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void get_backup_folder_should_combine_relative_path_with_appdata()
        {
            var result = Subject.GetBackupFolder();

            result.Should().Be(Path.Combine(TempFolder, "Backups"));
        }

        [Test]
        public void get_backup_folder_should_use_absolute_path_when_configured()
        {
            var absolutePath = Path.Combine(TempFolder, "CustomBackups");

            Mocker.GetMock<IConfigService>()
                .Setup(c => c.BackupFolder)
                .Returns(absolutePath);

            var result = Subject.GetBackupFolder();

            result.Should().Be(absolutePath);
        }

        [Test]
        public void get_backups_should_return_matching_files()
        {
            var scheduledFolder = Path.Combine(TempFolder, "Backups", "scheduled");
            var matchingFile = Path.Combine(scheduledFolder, "gamarr_backup_v1.0.0_2024.01.01_00.00.00.zip");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(It.Is<string>(s => s.Contains("scheduled")), false))
                .Returns(new[] { matchingFile });

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFileSize(matchingFile))
                .Returns(1024);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileGetLastWrite(matchingFile))
                .Returns(DateTime.UtcNow);

            var result = Subject.GetBackups();

            result.Should().Contain(b => b.Name == "gamarr_backup_v1.0.0_2024.01.01_00.00.00.zip");
        }

        [Test]
        public void get_backups_should_ignore_non_backup_files()
        {
            var scheduledFolder = Path.Combine(TempFolder, "Backups", "scheduled");
            var nonMatchingFile = Path.Combine(scheduledFolder, "random_file.zip");

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFiles(It.IsAny<string>(), false))
                .Returns(new[] { nonMatchingFile });

            var result = Subject.GetBackups();

            result.Should().BeEmpty();
        }
    }
}
