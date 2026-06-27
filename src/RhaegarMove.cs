using System;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal static class Program
    {
        private static System.Threading.Mutex singleInstance;
        private static AppSettings settings;
        private static OperationWorker worker;
        private static MouseHook mouseHook;
        private static AppLoop appLoop;

        [STAThread]
        private static void Main(string[] args)
        {
            if (RuntimeCommands.TryHandle(args))
                return;

            bool created;
            singleInstance = new System.Threading.Mutex(true, "Local\\RhaegarMove", out created);
            if (!created)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            settings = AppSettings.Load();
            ConfigValidation.WriteReport(settings, "startup");
            PreviewOverlay.Initialize();
            worker = new OperationWorker(settings);
            mouseHook = new MouseHook(settings, worker);

            try
            {
                mouseHook.Install();
                appLoop = new AppLoop(worker, settings);
                Application.Run(appLoop);
            }
            finally
            {
                Cleanup();
            }
        }

        private static void Cleanup()
        {
            PreviewOverlay.HideOverlay();

            if (mouseHook != null)
            {
                mouseHook.Dispose();
                mouseHook = null;
            }

            if (worker != null)
            {
                worker.Dispose();
                worker = null;
            }

            if (singleInstance != null)
            {
                singleInstance.ReleaseMutex();
                singleInstance.Dispose();
                singleInstance = null;
            }
        }

        private sealed class AppLoop : ApplicationContext
        {
            private readonly OperationWorker worker;
            private readonly AppSettings settings;
            private readonly RuntimeWatcher runtimeWatcher;
            private readonly Timer watchdog;

            public AppLoop(OperationWorker worker, AppSettings settings)
            {
                this.worker = worker;
                this.settings = settings;
                runtimeWatcher = new RuntimeWatcher();
                watchdog = new Timer();
                watchdog.Interval = Math.Max(100, settings.WatchdogMs);
                watchdog.Tick += delegate { OnTick(); };
                watchdog.Start();
            }

            private void OnTick()
            {
                worker.Watchdog();

                if (runtimeWatcher.ConsumeReload())
                {
                    settings.ReloadFromDisk();
                    WindowRules.Reload();
                    ConfigValidation.WriteReport(settings, "reload");
                    watchdog.Interval = Math.Max(100, settings.WatchdogMs);
                    RuntimeControl.WriteRuntime("reload applied " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                if (runtimeWatcher.ConsumeExit())
                {
                    RuntimeControl.WriteRuntime("exit applied " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    ExitThread();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    watchdog.Stop();
                    watchdog.Dispose();
                    runtimeWatcher.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
