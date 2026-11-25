using System;

namespace NzbDrone.Core.Download.Clients.Sabnzbd
{
    public enum SabnzbdAuthError
    {
        None,
        ApiKeyIncorrect,
        ApiKeyRequired
    }

    /// <summary>
    /// SABnzbd's API reports failures only as free text (no status or error codes), so the
    /// handful of strings the client has to react to are kept here, in one place, and pinned
    /// by unit tests against what the real server sends.
    /// </summary>
    public static class SabnzbdErrorParser
    {
        // sabnzbd/api.py: _MSG_NO_KEY / _MSG_BAD_KEY
        public const string ApiKeyRequiredMessage = "API Key Required";
        public const string ApiKeyIncorrectMessage = "API Key Incorrect";

        // The fail_msg SABnzbd sets when the unpacker hits an IO error
        public const string UnpackWriteErrorMessage = "Unpacking failed, write error or disk is full?";

        public static SabnzbdAuthError ParseAuthError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return SabnzbdAuthError.None;
            }

            if (error.Contains(ApiKeyIncorrectMessage, StringComparison.OrdinalIgnoreCase))
            {
                return SabnzbdAuthError.ApiKeyIncorrect;
            }

            if (error.Contains(ApiKeyRequiredMessage, StringComparison.OrdinalIgnoreCase))
            {
                return SabnzbdAuthError.ApiKeyRequired;
            }

            return SabnzbdAuthError.None;
        }

        public static bool IsUnpackWriteError(string failMessage)
        {
            return !string.IsNullOrWhiteSpace(failMessage) &&
                   failMessage.Trim().Equals(UnpackWriteErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
    }
}
