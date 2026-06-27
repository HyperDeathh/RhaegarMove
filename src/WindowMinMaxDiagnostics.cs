using System;
using System.IO;
using System.Text;

namespace RhaegarMove
{
    internal static class WindowMinMaxDiagnostics
    {
        public static void Record(IntPtr hwnd, MINMAXINFO info, RECT before, RECT after, AppSettings settings)
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                StringBuilder b = new StringBuilder();
                b.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                b.AppendLine("hwnd=" + hwnd);
                b.AppendLine("class=" + Geometry.ClassName(hwnd));
                b.AppendLine("title=" + Geometry.WindowTitle(hwnd));
                b.AppendLine("minTrack=" + info.ptMinTrackSize.x + "x" + info.ptMinTrackSize.y);
                b.AppendLine("maxTrack=" + info.ptMaxTrackSize.x + "x" + info.ptMaxTrackSize.y);
                b.AppendLine("maxSize=" + info.ptMaxSize.x + "x" + info.ptMaxSize.y);
                b.AppendLine("maxPosition=" + info.ptMaxPosition.x + "," + info.ptMaxPosition.y);
                b.AppendLine("before=" + FormatRect(before));
                b.AppendLine("after=" + FormatRect(after));
                b.AppendLine("changed=" + (before.left != after.left || before.top != after.top || before.right != after.right || before.bottom != after.bottom));
                b.AppendLine("---");
                File.AppendAllText(Path.Combine(RuntimeControl.ControlDir, "minmax.txt"), b.ToString());
            }
            catch
            {
            }
        }

        public static void RecordSkipped(IntPtr hwnd, string reason, AppSettings settings)
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                File.AppendAllText(Path.Combine(RuntimeControl.ControlDir, "minmax.txt"),
                    "time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    "hwnd=" + hwnd + Environment.NewLine +
                    "reason=" + reason + Environment.NewLine +
                    "class=" + Geometry.ClassName(hwnd) + Environment.NewLine +
                    "title=" + Geometry.WindowTitle(hwnd) + Environment.NewLine +
                    "---" + Environment.NewLine);
            }
            catch
            {
            }
        }

        private static string FormatRect(RECT r)
        {
            return r.left + "," + r.top + "," + r.right + "," + r.bottom;
        }
    }
}
