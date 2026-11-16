using System.Runtime.InteropServices;
using Mono.Unix.Native;

namespace NzbDrone.Mono.Interop
{
    internal sealed class SafeUnixHandle : SafeHandle
    {
        private SafeUnixHandle()
            : base((nint)(-1), true)
        {
        }

        public SafeUnixHandle(int fd)
            : base((nint)(-1), true)
        {
            handle = (nint)fd;
        }

        public override bool IsInvalid
        {
            get { return handle == (nint)(-1); }
        }

        protected override bool ReleaseHandle()
        {
            return Syscall.close(handle.ToInt32()) != -1;
        }
    }
}
