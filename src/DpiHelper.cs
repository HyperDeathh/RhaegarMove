using System;
using System.Runtime.InteropServices;

namespace RhaegarMove
{
    internal static class DpiHelper
    {
        private const int DefaultDpi = 96;
        private const int MDT_EFFECTIVE_DPI = 0;

        public static int GetWindowDpi(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return DefaultDpi;

            try
            {
                uint dpi = GetDpiForWindow(hwnd);
                if (dpi > 0)
                    return (int)dpi;
            }
            catch
            {
            }

            return DefaultDpi;
        }

        public static int GetMonitorDpi(POINT point)
        {
            try
            {
                IntPtr monitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero)
                    return DefaultDpi;
                uint x;
                uint y;
                if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out x, out y) == 0 && x > 0)
                    return (int)x;
            }
            catch
            {
            }
            return DefaultDpi;
        }

        public static int Scale(int value, int fromDpi, int toDpi)
        {
            if (fromDpi <= 0 || toDpi <= 0 || fromDpi == toDpi)
                return value;
            long scaled = (long)value * toDpi / fromDpi;
            if (scaled < 1)
                return 1;
            if (scaled > int.MaxValue)
                return int.MaxValue;
            return (int)scaled;
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    }
}
