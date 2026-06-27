using System;
using System.Threading;

namespace RhaegarMove
{
    internal static class WindowController
    {
        public static IntPtr FindTargetWindow(POINT pt, AppSettings settings)
        {
            IntPtr hwnd = NativeMethods.WindowFromPoint(pt);
            if (hwnd == IntPtr.Zero)
                return IntPtr.Zero;

            hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
            if (hwnd == IntPtr.Zero)
                return IntPtr.Zero;

            if (!NativeMethods.IsWindowVisible(hwnd) || NativeMethods.IsIconic(hwnd))
                return IntPtr.Zero;

            IntPtr stylePtr = NativeMethods.GetWindowLongPtrSafe(hwnd, NativeMethods.GWL_STYLE);
            long style = stylePtr.ToInt64();
            if ((style & NativeMethods.WS_CHILD) != 0 || (style & NativeMethods.WS_DISABLED) != 0)
                return IntPtr.Zero;

            string cls = Geometry.ClassName(hwnd);
            if (WindowRules.ShouldIgnoreWindow(hwnd, cls))
                return IntPtr.Zero;

            if (!settings.AllowFullScreenWindows && IsFullscreen(hwnd))
                return IntPtr.Zero;

            if (settings.SkipMaximizedWindows && NativeMethods.IsZoomed(hwnd))
                return IntPtr.Zero;

            return hwnd;
        }

        public static bool IsFullscreen(IntPtr hwnd)
        {
            RECT rect;
            RECT monitor;
            if (!NativeMethods.GetWindowRect(hwnd, out rect))
                return false;
            if (!Geometry.TryGetMonitorRectForWindow(hwnd, false, out monitor))
                return false;

            IntPtr stylePtr = NativeMethods.GetWindowLongPtrSafe(hwnd, NativeMethods.GWL_STYLE);
            long style = stylePtr.ToInt64();
            bool noCaption = (style & NativeMethods.WS_CAPTION) != NativeMethods.WS_CAPTION;
            return noCaption && rect.left == monitor.left && rect.top == monitor.top && rect.right == monitor.right && rect.bottom == monitor.bottom;
        }

        public static void RestoreForDrag(IntPtr hwnd, POINT pt, RECT currentRect, AppSettings settings)
        {
            int oldWidth = Math.Max(1, currentRect.Width);
            int oldHeight = Math.Max(1, currentRect.Height);
            double ratioX = Geometry.Clamp01((double)(pt.x - currentRect.left) / oldWidth);
            double ratioY = Geometry.Clamp01((double)(pt.y - currentRect.top) / oldHeight);

            RestoreData data;
            bool hasRestore = WindowRestoreStore.TryGet(hwnd, out data);
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
            Thread.Sleep(15);

            RECT restored;
            if (!Geometry.TryGetBestWindowRect(hwnd, out restored))
                return;

            int width = restored.Width;
            int height = restored.Height;
            if (hasRestore)
            {
                int currentDpi = DpiHelper.GetWindowDpi(hwnd);
                width = DpiHelper.Scale(data.Width, data.Dpi, currentDpi);
                height = DpiHelper.Scale(data.Height, data.Dpi, currentDpi);
            }

            width = Math.Max(settings.MinWidth, width);
            height = Math.Max(settings.MinHeight, height);

            int x = pt.x - (int)(width * ratioX);
            int y = pt.y - (int)(height * Math.Min(ratioY, 0.35));

            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOOWNERZORDER | NativeMethods.SWP_NOACTIVATE);
        }

        public static void NotifySizeMove(IntPtr hwnd, bool start, AppSettings settings)
        {
            if (!WindowRules.ShouldSendSizingNotifications(hwnd, Geometry.ClassName(hwnd)))
                return;

            NativeMethods.PostMessage(hwnd, start ? NativeMethods.WM_ENTERSIZEMOVE : NativeMethods.WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
            if (settings.NotifyMoveSizeEvents)
            {
                NativeMethods.NotifyWinEvent(start ? NativeMethods.EVENT_SYSTEM_MOVESIZESTART : NativeMethods.EVENT_SYSTEM_MOVESIZEEND, hwnd, 0, 0);
            }
        }

        public static void SendSizing(IntPtr hwnd, ResizeEdge edge, ref RECT rect)
        {
            int code = edge.ToSizingCode();
            if (code == 0)
                return;

            IntPtr result;
            NativeMethods.SendMessageTimeout(hwnd, NativeMethods.WM_SIZING, new IntPtr(code), ref rect, NativeMethods.SMTO_ABORTIFHUNG, 64, out result);
        }
    }
}
