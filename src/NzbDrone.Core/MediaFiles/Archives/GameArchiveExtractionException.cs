using System;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.MediaFiles.Archives
{
    public class GameArchiveExtractionException : NzbDroneException
    {
        public GameArchiveExtractionException(string message)
            : base(message)
        {
        }

        public GameArchiveExtractionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
