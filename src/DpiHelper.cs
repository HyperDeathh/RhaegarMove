using System;
using System.Runtime.InteropServices;

namespace RhaegarMove
{
    internal static class DpiHelper
    {
        private const int DefaultDpi = 96;

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
    }
}
