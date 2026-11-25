namespace NzbDrone.Common.Exceptions
{
    // Marks an exception that always reflects the user's environment or
    // configuration (a root folder that is gone, a missing recycle bin) rather
    // than a Gamarr fault. Sentry does not report these.
    public interface IEnvironmentalException
    {
    }
}
