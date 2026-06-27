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
                Console.WriteLine(GetConfigPath());
                return true;
            }
            if (command == "--diagnose-cursor" || command == "/diagnose-cursor")
            {
                DiagnoseCursor();
                return true;
            }
            if (command == "--preview-status" || command == "/preview-status")
            {
                Console.WriteLine(SnapPreview.DescribeLast());
                return true;
            }
            return false;
        }

        private static void WriteStatus()
        {
            bool running = Process.GetProcessesByName("RhaegarMove").Length > 0;
            Console.WriteLine("RhaegarMove running=" + running);
            Console.WriteLine("config=" + GetConfigPath());
            Console.WriteLine("rules=" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhaegarMove", "rules.txt"));
            Console.WriteLine("preview=" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhaegarMove", "preview.txt"));
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
            Console.WriteLine(text);
            RuleDiagnostics.WriteSnapshot("diagnose-cursor", hwnd);
        }
    }
}
