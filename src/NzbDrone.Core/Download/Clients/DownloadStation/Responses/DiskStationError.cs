using System.Collections.Generic;

namespace NzbDrone.Core.Download.Clients.DownloadStation.Responses
{
    public class DiskStationError
    {
        private const string OtpHint = "Gamarr cannot answer a 2-step verification prompt: use a dedicated DSM account for Gamarr that does not require it (and has Download Station permission).";

        private static readonly Dictionary<int, string> CommonMessages;
        private static readonly Dictionary<int, string> AuthMessages;
        private static readonly Dictionary<int, string> DownloadStationTaskMessages;
        private static readonly Dictionary<int, string> FileStationMessages;

        static DiskStationError()
        {
            CommonMessages = new Dictionary<int, string>
            {
                { 100, "Unknown error" },
                { 101, "Invalid parameter" },
                { 102, "The requested API does not exist" },
                { 103, "The requested method does not exist" },
                { 104, "The requested version does not support the functionality" },
                { 105, "The logged in session does not have permission" },
                { 106, "Session timeout" },
                { 107, "Session interrupted by duplicate login" },
                { 119, "SID not found" }
            };

            AuthMessages = new Dictionary<int, string>
            {
                { 400, "No such account or incorrect password" },
                { 401, "Guest or disabled account" },
                { 402, "Permission denied" },
                { 403, "2-step verification code required" },
                { 404, "Failed to authenticate 2-step verification code" },
                { 406, "2-step verification is enforced for this account" },
                { 407, "Blocked IP source" },
                { 408, "Expired password cannot change" },
                { 409, "Expired password" },
                { 410, "Password must be changed" }
            };

            DownloadStationTaskMessages = new Dictionary<int, string>
            {
                { 400, "File upload failed" },
                { 401, "Max number of tasks reached" },
                { 402, "Destination denied" },
                { 403, "Destination does not exist" },
                { 404, "Invalid task id" },
                { 405, "Invalid task action" },
                { 406, "No default destination" },
                { 407, "Set destination failed" },
                { 408, "File does not exist" }
            };

            FileStationMessages = new Dictionary<int, string>
            {
                { 160, "Permission denied. Give your user access to FileStation." },
                { 400, "Invalid parameter of file operation" },
                { 401, "Unknown error of file operation" },
                { 402, "System is too busy" },
                { 403, "Invalid user does this file operation" },
                { 404, "Invalid group does this file operation" },
                { 405, "Invalid user and group does this file operation" },
                { 406, "Can’t get user/group information from the account server" },
                { 407, "Operation not permitted" },
                { 408, "No such file or directory" },
                { 409, "Non-supported file system" },
                { 410, "Failed to connect internet-based file system (ex: CIFS)" },
                { 411, "Read-only file system" },
                { 412, "Filename too long in the non-encrypted file system" },
                { 413, "Filename too long in the encrypted file system" },
                { 414, "File already exists" },
                { 415, "Disk quota exceeded" },
                { 416, "No space left on device" },
                { 417, "Input/output error" },
                { 418, "Illegal name or path" },
                { 419, "Illegal file name" },
                { 420, "Illegal file name on FAT file system" },
                { 421, "Device or resource busy" },
                { 599, "No such task of the file operation" },
            };
        }

        public int Code { get; set; }

        /// <summary>
        /// Documented SYNO.API.Auth login failures (DSM Login Web API guide). Only the OTP
        /// codes (403, 404, 406) involve 2-step verification; the rest are plain account
        /// problems and must not be reported as such.
        /// </summary>
        public static string GetAuthHint(int code)
        {
            return code switch
            {
                400 => "Check the username and password.",
                401 => "The DSM account is a guest or is disabled. Enable it or use another account.",
                402 => "The DSM account is not permitted to sign in. Give it permission for Download Station.",
                403 => OtpHint,
                404 => "The 2-step verification code was rejected. " + OtpHint,
                406 => OtpHint,
                _ => null
            };
        }

        public bool SessionError => Code == 105 || Code == 106 || Code == 107 || Code == 119;

        public string GetMessage(DiskStationApi api)
        {
            if (api == DiskStationApi.Auth && AuthMessages.ContainsKey(Code))
            {
                return AuthMessages[Code];
            }

            if ((api == DiskStationApi.DownloadStationTask || api == DiskStationApi.DownloadStation2Task) && DownloadStationTaskMessages.ContainsKey(Code))
            {
                return DownloadStationTaskMessages[Code];
            }

            if (api == DiskStationApi.FileStationList && FileStationMessages.ContainsKey(Code))
            {
                return FileStationMessages[Code];
            }

            if (CommonMessages.ContainsKey(Code))
            {
                return CommonMessages[Code];
            }

            return $"{Code} - Unknown error";
        }
    }
}
