using System;

namespace NzbDrone.Common.Exceptions
{
    public class GamarrStartupException : NzbDroneException
    {
        public GamarrStartupException(string message, params object[] args)
            : base("Gamarr failed to start: " + string.Format(message, args))
        {
        }

        public GamarrStartupException(string message)
            : base("Gamarr failed to start: " + message)
        {
        }

        public GamarrStartupException()
            : base("Gamarr failed to start")
        {
        }

        public GamarrStartupException(Exception innerException, string message, params object[] args)
            : base("Gamarr failed to start: " + string.Format(message, args), innerException)
        {
        }

        public GamarrStartupException(Exception innerException, string message)
            : base("Gamarr failed to start: " + message, innerException)
        {
        }

        public GamarrStartupException(Exception innerException)
            : base("Gamarr failed to start: " + innerException.Message)
        {
        }
    }
}
