using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal static class RuntimeCommands
    {
        public static bool TryHandle(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            string command = args[0].Trim().ToLowerInvariant();
            if (command == "--status" || command == "/status")
            {
                WriteStatus();
                return true;
            }
            if (command == "--settings" || command == "/settings")
            {
                ShowSettingsWindow();
                return true;
            }
            if (command == "--reload" || command == "/reload")
            {
                RuntimeControl.RequestReload();
                return true;
            }
            if (command == "--request-exit" || command == "/request-exit")
            {
                RuntimeControl.RequestExit();
                return true;
            }
            if (command == "--config-path" || command == "/config-path")
            {
                RuntimeControl.WriteRuntime(GetConfigPath());
                return true;
            }
            if (command == "--diagnose-cursor" || command == "/diagnose-cursor")
            {
                DiagnoseCursor();
                return true;
            }
            if (command == "--preview-status" || command == "/preview-status")
            {
                RuntimeControl.WriteRuntime(SnapPreview.DescribeLast());
                return true;
            }
            return false;
        }

        private static void ShowSettingsWindow()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SettingsForm(delegate { RuntimeControl.RequestReload(); }));
        }

        private static void WriteStatus()
        {
            bool running = Process.GetProcessesByName("RhaegarMove").Length > 0;
            AppSettings settings = AppSettings.Load();
            string local = RuntimeControl.ControlDir;
            string text =
                "RhaegarMove running=" + running + Environment.NewLine +
                "controlMode=file-marker+watcher" + Environment.NewLine +
                "settingsCommand=available" + Environment.NewLine +
                "trayIconConfigured=" + settings.EnableTrayIcon + Environment.NewLine +
                "trayDefault=false" + Environment.NewLine +
                "respectWindowMinMaxInfo=" + settings.RespectWindowMinMaxInfo + Environment.NewLine +
                "allowCloakedWindows=" + settings.AllowCloakedWindows + Environment.NewLine +
                "config=" + GetConfigPath() + Environment.NewLine +
                "configReport=" + Path.Combine(local, "config-report.txt") + Environment.NewLine +
                "rules=" + Path.Combine(local, "rules.txt") + Environment.NewLine +
                "preview=" + Path.Combine(local, "preview.txt") + Environment.NewLine +
                "snapTargets=" + Path.Combine(local, "snap-targets.txt") + Environment.NewLine +
                "snapScore=" + Path.Combine(local, "snap-score.txt") + Environment.NewLine +
                "minmax=" + Path.Combine(local, "minmax.txt") + Environment.NewLine +
                "dpiSnap=" + Path.Combine(local, "dpi-snap.txt") + Environment.NewLine +
                "runtime=" + RuntimeControl.RuntimePath + Environment.NewLine +
                "runtimeLastWrite=" + LastWrite(RuntimeControl.RuntimePath) + Environment.NewLine +
                "lastReloadFile=" + RuntimeControl.LastReloadPath + Environment.NewLine +
                "lastReload=" + RuntimeControl.ReadLastReloadSummary() + Environment.NewLine +
                "reloadRequest=" + RuntimeControl.ReloadRequestPath + Environment.NewLine +
                "reloadRequestPending=" + File.Exists(RuntimeControl.ReloadRequestPath) + Environment.NewLine +
                "exitRequest=" + RuntimeControl.ExitRequestPath + Environment.NewLine +
                "exitRequestPending=" + File.Exists(RuntimeControl.ExitRequestPath) + Environment.NewLine;
            RuntimeControl.WriteRuntime(text);
        }

        private static string LastWrite(string path)
        {
            try
            {
                if (!File.Exists(path)) return "none";
                return File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "unknown";
            }
        }

        private static string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RhaegarMove.ini");
        }

        private static void DiagnoseCursor()
        {
            System.Drawing.Point p = Cursor.Position;
            POINT pt = new POINT(p.X, p.Y);
            IntPtr hwnd = NativeMethods.WindowFromPoint(pt);
            hwnd = hwnd == IntPtr.Zero ? IntPtr.Zero : NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
            string text = RuleDiagnostics.DescribeWindow(hwnd);
            RuleDiagnostics.WriteSnapshot("diagnose-cursor", hwnd);
            RuntimeControl.WriteRuntime(text);
        }
    }
}
