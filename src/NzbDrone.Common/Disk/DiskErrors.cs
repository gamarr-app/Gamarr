using System;
using System.IO;

namespace NzbDrone.Common.Disk
{
    /// <summary>
    /// Separates disk failures that come from the user's own environment (permissions, a
    /// read-only or full or stale mount, a locked file, a failing device) from failures that
    /// point at a bug in Gamarr (a path we built wrongly, a missing directory we assumed,
    /// a path that is too long). Environmental failures are reported at Warn and the work
    /// carries on; everything else belongs at Error so it reaches us.
    /// Classification is by exception type and errno / HResult only, never by message text.
    /// </summary>
    public static class DiskErrors
    {
        // Unix errno values, the same on Linux and macOS unless noted.
        private const int EPERM = 1;
        private const int ENOENT = 2;
        private const int EIO = 5;
        private const int EACCES = 13;
        private const int EBUSY = 16;
        private const int ENOSPC = 28;
        private const int EROFS = 30;
        private const int ESTALE_LINUX = 116;
        private const int ESTALE_MACOS = 70;
        private const int EDQUOT_LINUX = 122;
        private const int EDQUOT_MACOS = 69;

        // Win32 error codes; .NET reports them as HRESULT_FROM_WIN32 (0x8007xxxx).
        private const int ErrorFileNotFound = 2;
        private const int ErrorPathNotFound = 3;
        private const int ErrorAccessDenied = 5;
        private const int ErrorWriteProtect = 19;
        private const int ErrorNotReady = 21;
        private const int ErrorCrc = 23;
        private const int ErrorSharingViolation = 32;
        private const int ErrorLockViolation = 33;
        private const int ErrorHandleDiskFull = 39;
        private const int ErrorNetNameDeleted = 64;
        private const int ErrorDiskFull = 112;
        private const int ErrorIoDevice = 1117;

        /// <summary>
        /// True only for a genuine user-environment condition. A missing file or directory is
        /// treated as environmental only when <paramref name="allowMissing"/> is set, which a
        /// caller does when it knows another actor can legitimately remove the path under it
        /// and has already ruled out its own bug.
        /// </summary>
        public static bool IsEnvironmental(Exception exception, bool allowMissing = false)
        {
            if (exception == null)
            {
                return false;
            }

            if (exception is UnauthorizedAccessException)
            {
                return true;
            }

            if (exception is PathTooLongException)
            {
                // We built a path the filesystem cannot hold: our bug, not the environment.
                return false;
            }

            if (exception is FileNotFoundException || exception is DirectoryNotFoundException)
            {
                return allowMissing;
            }

            if (exception is not IOException)
            {
                return false;
            }

            return OperatingSystem.IsWindows()
                ? IsWindowsEnvironmental(exception.HResult, allowMissing)
                : IsUnixEnvironmental(exception.HResult, allowMissing);
        }

        /// <summary>True when the exception means the path (or its folder) does not exist.</summary>
        public static bool IsMissing(Exception exception)
        {
            if (exception is FileNotFoundException || exception is DirectoryNotFoundException)
            {
                return true;
            }

            if (exception is not IOException)
            {
                return false;
            }

            return OperatingSystem.IsWindows()
                ? exception.HResult == Win32HResult(ErrorFileNotFound) || exception.HResult == Win32HResult(ErrorPathNotFound)
                : exception.HResult == ENOENT;
        }

        private static bool IsUnixEnvironmental(int errno, bool allowMissing)
        {
            switch (errno)
            {
                case EACCES:
                case EPERM:
                case EROFS:
                case EBUSY:
                case EIO:
                case ENOSPC:
                    return true;
                case ENOENT:
                    return allowMissing;
                case ESTALE_LINUX:
                case EDQUOT_LINUX:
                    return !OperatingSystem.IsMacOS();
                case ESTALE_MACOS:
                case EDQUOT_MACOS:
                    return OperatingSystem.IsMacOS();
                default:
                    return false;
            }
        }

        private static bool IsWindowsEnvironmental(int hResult, bool allowMissing)
        {
            if (hResult == Win32HResult(ErrorFileNotFound) || hResult == Win32HResult(ErrorPathNotFound))
            {
                return allowMissing;
            }

            return hResult == Win32HResult(ErrorAccessDenied) ||
                   hResult == Win32HResult(ErrorWriteProtect) ||
                   hResult == Win32HResult(ErrorNotReady) ||
                   hResult == Win32HResult(ErrorCrc) ||
                   hResult == Win32HResult(ErrorSharingViolation) ||
                   hResult == Win32HResult(ErrorLockViolation) ||
                   hResult == Win32HResult(ErrorHandleDiskFull) ||
                   hResult == Win32HResult(ErrorNetNameDeleted) ||
                   hResult == Win32HResult(ErrorDiskFull) ||
                   hResult == Win32HResult(ErrorIoDevice);
        }

        private static int Win32HResult(int code)
        {
            return unchecked((int)(0x80070000u | (uint)code));
        }
    }
}
