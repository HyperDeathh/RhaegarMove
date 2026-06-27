using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RhaegarMove
{
    internal static class Geometry
    {
        public static bool TryGetBestWindowRect(IntPtr hwnd, out RECT rect)
        {
            rect = new RECT();
            if (hwnd == IntPtr.Zero)
                return false;

            RECT dwmRect;
            try
            {
                if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out dwmRect, Marshal.SizeOf(typeof(RECT))) == 0)
                {
                    if (!dwmRect.IsEmpty)
                    {
                        rect = dwmRect;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return NativeMethods.GetWindowRect(hwnd, out rect);
        }

        public static bool IsDwmCloaked(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return false;
            try
            {
                int cloaked;
                if (NativeMethods.DwmGetWindowAttributeInt(hwnd, NativeMethods.DWMWA_CLOAKED, out cloaked, Marshal.SizeOf(typeof(int))) == 0)
                    return cloaked != 0;
            }
            catch
            {
            }
            return false;
        }

        public static bool TryGetMonitorWorkArea(POINT pt, out RECT work)
        {
            work = new RECT();
            MONITORINFO info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            IntPtr monitor = NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
                return false;
            work = info.rcWork;
            return true;
        }

        public static bool TryGetMonitorRectForWindow(IntPtr hwnd, bool workArea, out RECT rect)
        {
            rect = new RECT();
            MONITORINFO info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            IntPtr monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
                return false;
            rect = workArea ? info.rcWork : info.rcMonitor;
            return true;
        }

        public static string ClassName(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetClassName(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string WindowTitle(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(512);
            NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static bool IsKeyDown(int vk)
        {
            return (NativeMethods.GetAsyncKeyState(vk) & unchecked((short)0x8000)) != 0;
        }

        public static bool IsAltDown()
        {
            return IsKeyDown(NativeMethods.VK_MENU) || IsKeyDown(NativeMethods.VK_LMENU) || IsKeyDown(NativeMethods.VK_RMENU);
        }

        public static bool IsShiftDown()
        {
            return IsKeyDown(NativeMethods.VK_SHIFT);
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        public static bool RectsOverlapVertically(RECT a, RECT b, int threshold)
        {
            return a.top <= b.bottom + threshold && b.top <= a.bottom + threshold;
        }

        public static bool RectsOverlapHorizontally(RECT a, RECT b, int threshold)
        {
            return a.left <= b.right + threshold && b.left <= a.right + threshold;
        }
    }
}
