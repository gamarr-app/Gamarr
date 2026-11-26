namespace NzbDrone.Common.Exceptions
{
    // The app data folder can't be created or written: always the host's
    // permissions (Docker PUID/PGID, volume ownership), never a Gamarr fault.
    public class AppFolderNotWritableException : GamarrStartupException, IEnvironmentalException
    {
        public AppFolderNotWritableException(string message, params object[] args)
            : base(message, args)
        {
        }
    }
}
