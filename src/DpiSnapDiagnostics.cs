using System;
using System.IO;
using System.Text;

namespace RhaegarMove
{
    internal static class DpiSnapDiagnostics
    {
        public static void Record(AppSettings settings, string kind, IntPtr hwnd, POINT point, RECT before, RECT after)
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                RECT work;
                bool hasWork = Geometry.TryGetMonitorWorkArea(point, out work);
                RestoreData restore;
                bool hasRestore = WindowRestoreStore.TryGet(hwnd, out restore);
                int windowDpi = DpiHelper.GetWindowDpi(hwnd);
                int monitorDpi = DpiHelper.GetMonitorDpi(point);

                StringBuilder b = new StringBuilder();
                b.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                b.AppendLine("kind=" + kind);
                b.AppendLine("hwnd=" + hwnd);
                b.AppendLine("windowDpi=" + windowDpi);
                b.AppendLine("monitorDpi=" + monitorDpi);
                b.AppendLine("dpiDelta=" + (windowDpi - monitorDpi));
                b.AppendLine("point=" + point.x + "," + point.y);
                b.AppendLine("hasWorkArea=" + hasWork);
                if (hasWork) b.AppendLine("workArea=" + FormatRect(work));
                b.AppendLine("before=" + FormatRect(before));
                b.AppendLine("after=" + FormatRect(after));
                b.AppendLine("hasRestore=" + hasRestore);
                if (hasRestore)
                {
                    b.AppendLine("restoreSize=" + restore.Width + "x" + restore.Height);
                    b.AppendLine("restoreDpi=" + restore.Dpi);
                    b.AppendLine("restoreFlags=" + restore.Flags);
                    b.AppendLine("restoreToMonitorScaleX1000=" + ScaleX1000(restore.Dpi, monitorDpi));
                }
                b.AppendLine("---");
                File.AppendAllText(Path.Combine(RuntimeControl.ControlDir, "dpi-snap.txt"), b.ToString());
            }
            catch
            {
            }
        }

        private static int ScaleX1000(int fromDpi, int toDpi)
        {
            if (fromDpi <= 0 || toDpi <= 0)
                return 1000;
            return (int)((long)toDpi * 1000L / fromDpi);
        }

        private static string FormatRect(RECT r)
        {
            return r.left + "," + r.top + "," + r.right + "," + r.bottom;
        }
    }
}
