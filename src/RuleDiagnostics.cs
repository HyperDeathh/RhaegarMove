using System;
using System.IO;

namespace RhaegarMove
{
    internal static class RuleDiagnostics
    {
        public static string DescribeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return "hwnd=0";

            string cls = Geometry.ClassName(hwnd);
            string title = Geometry.WindowTitle(hwnd);
            bool ignored = WindowRules.ShouldIgnoreWindow(hwnd, cls);
            bool snapTarget = WindowRules.ShouldSnapToWindow(hwnd, cls);
            bool sendsSizing = WindowRules.ShouldSendSizingNotifications(hwnd, cls);
            bool canResize = WindowRules.ShouldAllowResize(hwnd, cls);

            return
                "hwnd=" + hwnd.ToInt64().ToString("X") + Environment.NewLine +
                "class=" + cls + Environment.NewLine +
                "title=" + title + Environment.NewLine +
                "ignored=" + ignored + Environment.NewLine +
                "snapTarget=" + snapTarget + Environment.NewLine +
                "sendsSizingNotifications=" + sendsSizing + Environment.NewLine +
                "canResize=" + canResize + Environment.NewLine;
        }

        public static void WriteSnapshot(string reason, IntPtr hwnd)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhaegarMove");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "rules.txt");
                string text =
                    "reason=" + reason + Environment.NewLine +
                    "time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    DescribeWindow(hwnd) + Environment.NewLine;
                File.WriteAllText(path, text);
            }
            catch
            {
            }
        }
    }
}
