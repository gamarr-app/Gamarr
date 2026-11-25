using System;
using System.IO;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public class RecycleBinException : DirectoryNotFoundException, IEnvironmentalException
    {
        public RecycleBinException()
        {
        }

        public RecycleBinException(string message)
            : base(message)
        {
        }

        public RecycleBinException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
