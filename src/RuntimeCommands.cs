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
            if (command == "--config-path" || command == "/config-path")
            {
                WriteRuntimeFile(GetConfigPath());
                return true;
            }
            if (command == "--diagnose-cursor" || command == "/diagnose-cursor")
            {
                DiagnoseCursor();
                return true;
            }
            if (command == "--preview-status" || command == "/preview-status")
            {
                WriteRuntimeFile(SnapPreview.DescribeLast());
                return true;
            }
            return false;
        }

        private static void WriteStatus()
        {
            bool running = Process.GetProcessesByName("RhaegarMove").Length > 0;
            string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhaegarMove");
            string text =
                "RhaegarMove running=" + running + Environment.NewLine +
                "config=" + GetConfigPath() + Environment.NewLine +
                "rules=" + Path.Combine(local, "rules.txt") + Environment.NewLine +
                "preview=" + Path.Combine(local, "preview.txt") + Environment.NewLine;
            WriteRuntimeFile(text);
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
            WriteRuntimeFile(text);
        }

        private static void WriteRuntimeFile(string text)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhaegarMove");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "runtime.txt"), text ?? string.Empty);
            }
            catch
            {
            }
        }
    }
}
