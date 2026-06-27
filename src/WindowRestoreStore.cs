using System;
using System.Collections.Generic;

namespace RhaegarMove
{
    internal sealed class RestoreData
    {
        public int Width;
        public int Height;
        public int Flags;
    }

    internal static class RestoreFlags
    {
        public const int Snapped = 1;
        public const int Left = 1 << 1;
        public const int Right = 1 << 2;
        public const int Top = 1 << 3;
        public const int Bottom = 1 << 4;
        public const int Maximized = 1 << 5;
    }

    internal static class WindowRestoreStore
    {
        private const string PropSize = "RhaegarMove.RestoreSize";
        private const string PropFlags = "RhaegarMove.RestoreFlags";
        private static readonly object Gate = new object();
        private static readonly Dictionary<IntPtr, RestoreData> Fallback = new Dictionary<IntPtr, RestoreData>();

        public static void Set(IntPtr hwnd, int width, int height, int flags)
        {
            if (hwnd == IntPtr.Zero)
                return;

            width = Math.Max(1, width);
            height = Math.Max(1, height);

            long packed = ((long)(uint)height << 32) | (uint)width;
            bool ok1 = NativeMethods.SetProp(hwnd, PropSize, new IntPtr(packed));
            bool ok2 = NativeMethods.SetProp(hwnd, PropFlags, new IntPtr(flags));
            if (ok1 && ok2)
                return;

            lock (Gate)
            {
                Fallback[hwnd] = new RestoreData { Width = width, Height = height, Flags = flags };
            }
        }

        public static bool TryGet(IntPtr hwnd, out RestoreData data)
        {
            data = null;
            if (hwnd == IntPtr.Zero)
                return false;

            IntPtr sizeProp = NativeMethods.GetProp(hwnd, PropSize);
            IntPtr flagProp = NativeMethods.GetProp(hwnd, PropFlags);
            if (sizeProp != IntPtr.Zero && flagProp != IntPtr.Zero)
            {
                long packed = sizeProp.ToInt64();
                data = new RestoreData
                {
                    Width = (int)(packed & 0xffffffffL),
                    Height = (int)((packed >> 32) & 0xffffffffL),
                    Flags = flagProp.ToInt32()
                };
                return data.Width > 0 && data.Height > 0;
            }

            lock (Gate)
            {
                return Fallback.TryGetValue(hwnd, out data);
            }
        }

        public static int GetFlags(IntPtr hwnd)
        {
            RestoreData data;
            return TryGet(hwnd, out data) ? data.Flags : 0;
        }

        public static bool IsSnapped(IntPtr hwnd)
        {
            return (GetFlags(hwnd) & RestoreFlags.Snapped) != 0;
        }

        public static void Clear(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return;

            NativeMethods.RemoveProp(hwnd, PropSize);
            NativeMethods.RemoveProp(hwnd, PropFlags);
            lock (Gate)
            {
                Fallback.Remove(hwnd);
            }
        }
    }
}
