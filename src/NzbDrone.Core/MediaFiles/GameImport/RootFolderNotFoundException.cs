using System;
using System.IO;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public class RootFolderNotFoundException : DirectoryNotFoundException, IEnvironmentalException
    {
        public RootFolderNotFoundException()
        {
        }

        public RootFolderNotFoundException(string message)
            : base(message)
        {
        }

        public RootFolderNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
