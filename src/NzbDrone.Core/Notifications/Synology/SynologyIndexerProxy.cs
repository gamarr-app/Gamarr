using System;
using NLog;
using NzbDrone.Common.Processes;

namespace NzbDrone.Core.Notifications.Synology
{
    public interface ISynologyIndexerProxy
    {
        bool Test();
        void AddFile(string filepath);
        void DeleteFile(string filepath);
        void AddFolder(string folderpath);
        void DeleteFolder(string folderpath);
        void UpdateFolder(string folderpath);
        void UpdateLibrary();
    }

    public class SynologyIndexerProxy : ISynologyIndexerProxy
    {
        private const string SynoIndexPath = "/usr/syno/bin/synoindex";

        private readonly IProcessProvider _processProvider;
        private readonly Logger _logger;

        public SynologyIndexerProxy(IProcessProvider processProvider, Logger logger)
        {
            _processProvider = processProvider;
            _logger = logger;
        }

        public bool Test()
        {
            try
            {
                ExecuteCommand("--help", false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "synoindex not available");
                return false;
            }
        }

        public void AddFile(string filepath)
        {
            ExecuteCommand("-a " + Escape(filepath));
        }

        public void DeleteFile(string filepath)
        {
            ExecuteCommand("-d " + Escape(filepath));
        }

        public void AddFolder(string folderpath)
        {
            ExecuteCommand("-A " + Escape(folderpath));
        }

        public void DeleteFolder(string folderpath)
        {
            ExecuteCommand("-D " + Escape(folderpath));
        }

        public void UpdateFolder(string folderpath)
        {
            ExecuteCommand("-R " + Escape(folderpath));
        }

        public void UpdateLibrary()
        {
            ExecuteCommand("-R video");
        }

        private void ExecuteCommand(string args, bool throwOnStdOut = true)
        {
            var output = _processProvider.StartAndCapture(SynoIndexPath, args);

            if (output.Standard.Count != 0 && throwOnStdOut)
            {
                throw new SynologyException("synoindex returned an error: {0}", string.Join("\n", output.Standard));
            }

            if (output.Error.Count != 0)
            {
                throw new SynologyException("synoindex returned an error: {0}", string.Join("\n", output.Error));
            }
        }

        private string Escape(string arg)
        {
            return string.Format("\"{0}\"", arg.Replace("\"", "\\\""));
        }
    }
}
