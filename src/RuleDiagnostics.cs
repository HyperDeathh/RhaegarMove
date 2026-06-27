using System;
using System.IO;
using System.Text;

namespace RhaegarMove
{
    internal static class RuleDiagnostics
    {
        public static string DescribeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return "hwnd=0";

            string cls = Geometry.ClassName(hwnd);
            bool ignored = WindowRules.ShouldIgnoreWindow(hwnd, cls);
            bool snapTarget = WindowRules.ShouldSnapToWindow(hwnd, cls);
            bool sendsSizing = WindowRules.ShouldSendSizingNotifications(hwnd, cls);
            bool canResize = WindowRules.ShouldAllowResize(hwnd, cls);
            bool respectMinMax = WindowRules.ShouldRespectMinMaxInfo(hwnd, cls);
            bool cloaked = Geometry.IsDwmCloaked(hwnd);
            bool visible = NativeMethods.IsWindowVisible(hwnd);
            bool minimized = NativeMethods.IsIconic(hwnd);
            bool maximized = NativeMethods.IsZoomed(hwnd);
            bool fullscreen = WindowController.IsFullscreen(hwnd);

            RECT rect;
            bool hasRect = Geometry.TryGetBestWindowRect(hwnd, out rect);

            StringBuilder b = new StringBuilder();
            b.AppendLine("hwnd=" + hwnd.ToInt64().ToString("X"));
            b.AppendLine("visible=" + visible);
            b.AppendLine("minimized=" + minimized);
            b.AppendLine("maximized=" + maximized);
            b.AppendLine("fullscreen=" + fullscreen);
            b.AppendLine("dwmCloaked=" + cloaked);
            b.AppendLine("hasRect=" + hasRect);
            if (hasRect) b.AppendLine("rect=" + FormatRect(rect));
            b.AppendLine("ignored=" + ignored);
            b.AppendLine("snapTarget=" + snapTarget);
            b.AppendLine("sendsSizingNotifications=" + sendsSizing);
            b.AppendLine("canResize=" + canResize);
            b.AppendLine("respectMinMaxInfo=" + respectMinMax);
            AppendMinMax(b, hwnd, respectMinMax);
            b.Append(WindowRules.ExplainWindow(hwnd, cls));
            return b.ToString();
        }

        private static void AppendMinMax(StringBuilder b, IntPtr hwnd, bool respectMinMax)
        {
            if (!respectMinMax)
            {
                b.AppendLine("minMaxInfo=blocked-by-rule");
                return;
            }

            MINMAXINFO info;
            if (!WindowMinMax.TryGet(hwnd, out info))
            {
                b.AppendLine("minMaxInfo=unavailable");
                return;
            }

            b.AppendLine("minMaxInfo=available");
            b.AppendLine("minTrack=" + info.ptMinTrackSize.x + "x" + info.ptMinTrackSize.y);
            b.AppendLine("maxTrack=" + info.ptMaxTrackSize.x + "x" + info.ptMaxTrackSize.y);
            b.AppendLine("maxSize=" + info.ptMaxSize.x + "x" + info.ptMaxSize.y);
            b.AppendLine("maxPosition=" + info.ptMaxPosition.x + "," + info.ptMaxPosition.y);
        }

        private static string FormatRect(RECT r)
        {
            return r.left + "," + r.top + "," + r.right + "," + r.bottom;
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
