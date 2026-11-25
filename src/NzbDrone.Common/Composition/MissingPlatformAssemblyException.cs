using System.IO;

namespace NzbDrone.Common.Composition
{
    // Thrown by AssemblyLoader when a platform assembly (Gamarr.Windows /
    // Gamarr.Mono) is absent from the install folder. A distinct type so the
    // Sentry filter can recognise it without reading the message.
    public class MissingPlatformAssemblyException : FileNotFoundException
    {
        public MissingPlatformAssemblyException(string message, string fileName)
            : base(message, fileName)
        {
        }
    }
}
