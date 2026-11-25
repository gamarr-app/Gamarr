namespace NzbDrone.Core.Download.Clients.Sabnzbd
{
    public class SabnzbdAuthenticationException : DownloadClientAuthenticationException
    {
        public SabnzbdAuthenticationException(SabnzbdAuthError reason, string error)
            : base("Error response received from SABnzbd: {0}", error)
        {
            Reason = reason;
        }

        public SabnzbdAuthError Reason { get; }
    }
}
