using System;
using System.IO;

namespace RhaegarMove
{
    internal sealed class RuntimeWatcher : IDisposable
    {
        private readonly object gate = new object();
        private FileSystemWatcher watcher;
        private bool reloadRequested;
        private bool exitRequested;

        public RuntimeWatcher()
        {
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                watcher = new FileSystemWatcher(RuntimeControl.ControlDir);
                watcher.Filter = "*.request";
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                watcher.Created += OnChanged;
                watcher.Changed += OnChanged;
                watcher.EnableRaisingEvents = true;
            }
            catch
            {
                watcher = null;
            }
        }

        public bool ConsumeReload()
        {
            bool requested;
            lock (gate)
            {
                requested = reloadRequested;
                reloadRequested = false;
            }
            return requested || RuntimeControl.ConsumeReloadRequest();
        }

        public bool ConsumeExit()
        {
            bool requested;
            lock (gate)
            {
                requested = exitRequested;
                exitRequested = false;
            }
            return requested || RuntimeControl.ConsumeExitRequest();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            string name = Path.GetFileName(e.FullPath);
            lock (gate)
            {
                if (string.Equals(name, "reload.request", StringComparison.OrdinalIgnoreCase))
                    reloadRequested = true;
                else if (string.Equals(name, "exit.request", StringComparison.OrdinalIgnoreCase))
                    exitRequested = true;
            }
        }

        public void Dispose()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        }
    }
}
