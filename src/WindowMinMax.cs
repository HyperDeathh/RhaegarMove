using System;
using System.Runtime.InteropServices;

namespace RhaegarMove
{
    internal static class WindowMinMax
    {
        public static RECT Apply(IntPtr hwnd, RECT desired, ResizeEdge edge, AppSettings settings)
        {
            if (!settings.RespectWindowMinMaxInfo)
                return desired;
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
                return desired;

            MINMAXINFO info;
            if (!TryGet(hwnd, out info))
                return desired;

            int minWidth = Math.Max(1, info.ptMinTrackSize.x);
            int minHeight = Math.Max(1, info.ptMinTrackSize.y);
            int maxWidth = info.ptMaxTrackSize.x > minWidth ? info.ptMaxTrackSize.x : int.MaxValue;
            int maxHeight = info.ptMaxTrackSize.y > minHeight ? info.ptMaxTrackSize.y : int.MaxValue;

            int width = Math.Max(1, desired.Width);
            int height = Math.Max(1, desired.Height);
            int clampedWidth = Clamp(width, minWidth, maxWidth);
            int clampedHeight = Clamp(height, minHeight, maxHeight);
            if (clampedWidth == width && clampedHeight == height)
                return desired;

            return ApplySize(desired, edge, clampedWidth, clampedHeight);
        }

        private static bool TryGet(IntPtr hwnd, out MINMAXINFO info)
        {
            info = new MINMAXINFO();
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MINMAXINFO)));
                Marshal.StructureToPtr(info, ptr, false);
                IntPtr result;
                NativeMethods.SendMessageTimeoutPtr(hwnd, NativeMethods.WM_GETMINMAXINFO, IntPtr.Zero, ptr,
                    NativeMethods.SMTO_ABORTIFHUNG, 80, out result);
                info = (MINMAXINFO)Marshal.PtrToStructure(ptr, typeof(MINMAXINFO));
                return info.ptMinTrackSize.x > 0 && info.ptMinTrackSize.y > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        private static RECT ApplySize(RECT rect, ResizeEdge edge, int width, int height)
        {
            int left = rect.left;
            int top = rect.top;
            int right = rect.right;
            int bottom = rect.bottom;

            if (width != rect.Width)
            {
                if (edge.Left && !edge.Right)
                    left = right - width;
                else if (edge.Right && !edge.Left)
                    right = left + width;
                else
                {
                    int center = (left + right) / 2;
                    left = center - width / 2;
                    right = left + width;
                }
            }

            if (height != rect.Height)
            {
                if (edge.Top && !edge.Bottom)
                    top = bottom - height;
                else if (edge.Bottom && !edge.Top)
                    bottom = top + height;
                else
                {
                    int center = (top + bottom) / 2;
                    top = center - height / 2;
                    bottom = top + height;
                }
            }

            return new RECT(left, top, right, bottom);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
