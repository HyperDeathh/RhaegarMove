using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal static class Program
    {
        private const int WH_MOUSE_LL = 14;
        private const int HC_ACTION = 0;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;

        private const int VK_MENU = 0x12;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;

        private const int GA_ROOT = 2;
        private const int SW_RESTORE = 9;

        private const uint WM_ENTERSIZEMOVE = 0x0231;
        private const uint WM_EXITSIZEMOVE = 0x0232;
        private const uint WM_SIZING = 0x0214;

        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        private static readonly object Gate = new object();
        private static readonly LowLevelProc MouseProcDelegate = MouseProc;

        private static IntPtr mouseHook = IntPtr.Zero;
        private static Mutex singleInstance;
        private static Settings settings;
        private static State state = new State();
        private static AppLoop appLoop;

        [STAThread]
        private static void Main()
        {
            bool created;
            singleInstance = new Mutex(true, "Local\\RhaegarMove", out created);
            if (!created)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            settings = Settings.Load();

            try
            {
                InstallMouseHook();
                appLoop = new AppLoop();
                Application.Run(appLoop);
            }
            finally
            {
                Cleanup();
                singleInstance.ReleaseMutex();
                singleInstance.Dispose();
            }
        }

        private sealed class AppLoop : ApplicationContext
        {
            private readonly Timer watchdog;

            public AppLoop()
            {
                watchdog = new Timer();
                watchdog.Interval = Math.Max(100, settings.WatchdogMs);
                watchdog.Tick += delegate { WatchdogTick(); };
                watchdog.Start();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    watchdog.Stop();
                    watchdog.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private static void WatchdogTick()
        {
            lock (Gate)
            {
                if (state.Active && !IsAltDown())
                    FinishOperationLocked(true);

                if (state.Active && !IsWindow(state.Target))
                    CancelOperationLocked();
            }
        }

        private static void InstallMouseHook()
        {
            if (mouseHook != IntPtr.Zero)
                return;

            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                IntPtr moduleHandle = GetModuleHandle(currentModule.ModuleName);
                mouseHook = SetWindowsHookEx(WH_MOUSE_LL, MouseProcDelegate, moduleHandle, 0);
            }

            if (mouseHook == IntPtr.Zero)
                throw new InvalidOperationException("Mouse hook could not be installed.");
        }

        private static void Cleanup()
        {
            lock (Gate)
            {
                CancelOperationLocked();
                if (mouseHook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(mouseHook);
                    mouseHook = IntPtr.Zero;
                }
            }
        }

        private static IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode != HC_ACTION)
                    return CallNextHookEx(mouseHook, nCode, wParam, lParam);

                int msg = wParam.ToInt32();
                MSLLHOOKSTRUCT data = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                POINT pt = data.pt;

                lock (Gate)
                {
                    MouseButton upButton = UpButtonFromMessage(msg);
                    if (upButton != MouseButton.None && state.SwallowNextUp == upButton)
                    {
                        state.SwallowNextUp = MouseButton.None;
                        return new IntPtr(1);
                    }

                    if (state.Active)
                    {
                        if (msg == WM_MOUSEMOVE)
                        {
                            if (!IsAltDown())
                                FinishOperationLocked(true);
                            else
                                UpdateOperationLocked(pt);
                            return new IntPtr(1);
                        }

                        if (upButton == state.Button)
                        {
                            FinishOperationLocked(false);
                            return new IntPtr(1);
                        }

                        return new IntPtr(1);
                    }

                    if (!IsAltDown())
                        return CallNextHookEx(mouseHook, nCode, wParam, lParam);

                    if (msg == WM_LBUTTONDOWN)
                    {
                        if (BeginOperationLocked(pt, MouseButton.Left, Operation.Move))
                            return new IntPtr(1);
                    }
                    else if (msg == WM_RBUTTONDOWN)
                    {
                        if (BeginOperationLocked(pt, MouseButton.Right, Operation.Resize))
                            return new IntPtr(1);
                    }
                }

                return CallNextHookEx(mouseHook, nCode, wParam, lParam);
            }
            catch
            {
                lock (Gate)
                {
                    CancelOperationLocked();
                }
                return CallNextHookEx(mouseHook, nCode, wParam, lParam);
            }
        }

        private static bool BeginOperationLocked(POINT pt, MouseButton button, Operation operation)
        {
            IntPtr target = FindTargetWindow(pt);
            if (target == IntPtr.Zero)
                return false;

            RECT rect;
            if (!GetWindowRect(target, out rect))
                return false;

            if (operation == Operation.Move && IsZoomed(target))
            {
                RestoreForDrag(target, pt, rect);
                if (!GetWindowRect(target, out rect))
                    return false;
            }

            state.Target = target;
            state.Active = true;
            state.Button = button;
            state.Operation = operation;
            state.StartMouse = pt;
            state.StartRect = rect;
            state.Edge = operation == Operation.Resize ? ResizeEdge.FromPoint(rect, pt) : ResizeEdge.None;

            PostMessage(target, WM_ENTERSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        private static void UpdateOperationLocked(POINT pt)
        {
            if (!IsWindow(state.Target))
            {
                CancelOperationLocked();
                return;
            }

            if (state.Operation == Operation.Move)
                UpdateMoveLocked(pt);
            else if (state.Operation == Operation.Resize)
                UpdateResizeLocked(pt);
        }

        private static void UpdateMoveLocked(POINT pt)
        {
            int dx = pt.x - state.StartMouse.x;
            int dy = pt.y - state.StartMouse.y;
            int width = state.StartRect.Width;
            int height = state.StartRect.Height;
            int x = state.StartRect.left + dx;
            int y = state.StartRect.top + dy;

            if (settings.EnableEdgeSnap)
                SnapToMonitorEdges(pt, ref x, ref y, width, height);

            SetWindowPos(state.Target, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
        }

        private static void UpdateResizeLocked(POINT pt)
        {
            int left = state.StartRect.left;
            int top = state.StartRect.top;
            int right = state.StartRect.right;
            int bottom = state.StartRect.bottom;
            int dx = pt.x - state.StartMouse.x;
            int dy = pt.y - state.StartMouse.y;

            if (state.Edge.Left) left += dx;
            if (state.Edge.Right) right += dx;
            if (state.Edge.Top) top += dy;
            if (state.Edge.Bottom) bottom += dy;

            if (right - left < settings.MinWidth)
            {
                if (state.Edge.Left) left = right - settings.MinWidth;
                else right = left + settings.MinWidth;
            }

            if (bottom - top < settings.MinHeight)
            {
                if (state.Edge.Top) top = bottom - settings.MinHeight;
                else bottom = top + settings.MinHeight;
            }

            RECT desired = new RECT(left, top, right, bottom);
            SendSizing(state.Target, state.Edge, ref desired);

            SetWindowPos(state.Target, IntPtr.Zero, desired.left, desired.top,
                Math.Max(settings.MinWidth, desired.Width), Math.Max(settings.MinHeight, desired.Height),
                SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
        }

        private static void FinishOperationLocked(bool swallowNextMouseUp)
        {
            if (state.Active && state.Target != IntPtr.Zero)
                PostMessage(state.Target, WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);

            MouseButton oldButton = state.Button;
            state.Reset();
            if (swallowNextMouseUp)
                state.SwallowNextUp = oldButton;
        }

        private static void CancelOperationLocked()
        {
            if (state.Active && state.Target != IntPtr.Zero)
                PostMessage(state.Target, WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
            state.Reset();
        }

        private static void RestoreForDrag(IntPtr hwnd, POINT pt, RECT maximizedRect)
        {
            int oldWidth = Math.Max(1, maximizedRect.Width);
            int oldHeight = Math.Max(1, maximizedRect.Height);
            double ratioX = Clamp01((double)(pt.x - maximizedRect.left) / oldWidth);
            double ratioY = Clamp01((double)(pt.y - maximizedRect.top) / oldHeight);

            ShowWindow(hwnd, SW_RESTORE);
            Thread.Sleep(15);

            RECT restored;
            if (!GetWindowRect(hwnd, out restored))
                return;

            int width = Math.Max(settings.MinWidth, restored.Width);
            int height = Math.Max(settings.MinHeight, restored.Height);
            int x = pt.x - (int)(width * ratioX);
            int y = pt.y - (int)(height * Math.Min(ratioY, 0.35));

            SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
        }

        private static void SnapToMonitorEdges(POINT pt, ref int x, ref int y, int width, int height)
        {
            MONITORINFO info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            IntPtr monitor = MonitorFromPoint(pt, 2);
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
                return;

            RECT work = info.rcWork;
            int threshold = Math.Max(0, settings.SnapThreshold);

            if (Math.Abs(x - work.left) <= threshold) x = work.left;
            if (Math.Abs(y - work.top) <= threshold) y = work.top;
            if (Math.Abs(x + width - work.right) <= threshold) x = work.right - width;
            if (Math.Abs(y + height - work.bottom) <= threshold) y = work.bottom - height;
        }

        private static void SendSizing(IntPtr hwnd, ResizeEdge edge, ref RECT rect)
        {
            int code = edge.ToSizingCode();
            if (code == 0)
                return;

            IntPtr result;
            SendMessageTimeout(hwnd, WM_SIZING, new IntPtr(code), ref rect, SMTO_ABORTIFHUNG, 64, out result);
        }

        private static IntPtr FindTargetWindow(POINT pt)
        {
            IntPtr hwnd = WindowFromPoint(pt);
            if (hwnd == IntPtr.Zero)
                return IntPtr.Zero;

            hwnd = GetAncestor(hwnd, GA_ROOT);
            if (hwnd == IntPtr.Zero)
                return IntPtr.Zero;

            if (!IsWindowVisible(hwnd) || IsIconic(hwnd))
                return IntPtr.Zero;

            string cls = ClassName(hwnd);
            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" || cls == "Button" || cls == "#32768")
                return IntPtr.Zero;

            return hwnd;
        }

        private static string ClassName(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static bool IsAltDown()
        {
            return IsKeyDown(VK_MENU) || IsKeyDown(VK_LMENU) || IsKeyDown(VK_RMENU);
        }

        private static bool IsKeyDown(int vk)
        {
            return (GetAsyncKeyState(vk) & unchecked((short)0x8000)) != 0;
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        private static MouseButton UpButtonFromMessage(int msg)
        {
            if (msg == WM_LBUTTONUP) return MouseButton.Left;
            if (msg == WM_RBUTTONUP) return MouseButton.Right;
            return MouseButton.None;
        }

        private enum Operation { None, Move, Resize }
        private enum MouseButton { None, Left, Right }

        private sealed class State
        {
            public bool Active;
            public IntPtr Target;
            public MouseButton Button;
            public MouseButton SwallowNextUp;
            public Operation Operation;
            public POINT StartMouse;
            public RECT StartRect;
            public ResizeEdge Edge;

            public void Reset()
            {
                Active = false;
                Target = IntPtr.Zero;
                Button = MouseButton.None;
                Operation = Operation.None;
                StartMouse = new POINT();
                StartRect = new RECT();
                Edge = ResizeEdge.None;
            }
        }

        private sealed class Settings
        {
            public int SnapThreshold = 16;
            public int MinWidth = 120;
            public int MinHeight = 80;
            public bool EnableEdgeSnap = true;
            public int WatchdogMs = 250;

            public static Settings Load()
            {
                Settings s = new Settings();
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RhaegarMove.ini");
                if (!File.Exists(path)) return s;

                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("[")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    if (key.Equals("SnapThreshold", StringComparison.OrdinalIgnoreCase)) s.SnapThreshold = ToInt(value, s.SnapThreshold);
                    else if (key.Equals("MinWidth", StringComparison.OrdinalIgnoreCase)) s.MinWidth = ToInt(value, s.MinWidth);
                    else if (key.Equals("MinHeight", StringComparison.OrdinalIgnoreCase)) s.MinHeight = ToInt(value, s.MinHeight);
                    else if (key.Equals("EnableEdgeSnap", StringComparison.OrdinalIgnoreCase)) s.EnableEdgeSnap = ToBool(value, s.EnableEdgeSnap);
                    else if (key.Equals("WatchdogMs", StringComparison.OrdinalIgnoreCase)) s.WatchdogMs = ToInt(value, s.WatchdogMs);
                }
                s.MinWidth = Math.Max(40, s.MinWidth);
                s.MinHeight = Math.Max(40, s.MinHeight);
                s.WatchdogMs = Math.Max(100, s.WatchdogMs);
                return s;
            }

            private static int ToInt(string value, int fallback)
            {
                int parsed;
                return int.TryParse(value, out parsed) ? parsed : fallback;
            }

            private static bool ToBool(string value, bool fallback)
            {
                bool parsed;
                if (bool.TryParse(value, out parsed)) return parsed;
                if (value == "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase)) return true;
                if (value == "0" || value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("off", StringComparison.OrdinalIgnoreCase)) return false;
                return fallback;
            }
        }

        private struct ResizeEdge
        {
            public bool Left;
            public bool Right;
            public bool Top;
            public bool Bottom;

            public static readonly ResizeEdge None = new ResizeEdge();

            public static ResizeEdge FromPoint(RECT rect, POINT pt)
            {
                ResizeEdge e = new ResizeEdge();
                e.Left = pt.x < rect.left + rect.Width / 2;
                e.Right = !e.Left;
                e.Top = pt.y < rect.top + rect.Height / 2;
                e.Bottom = !e.Top;
                return e;
            }

            public int ToSizingCode()
            {
                if (Top && Left) return WMSZ_TOPLEFT;
                if (Top && Right) return WMSZ_TOPRIGHT;
                if (Bottom && Left) return WMSZ_BOTTOMLEFT;
                if (Bottom && Right) return WMSZ_BOTTOMRIGHT;
                return 0;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public int Width { get { return right - left; } }
            public int Height { get { return bottom - top; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int cmd);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, ref RECT lParam, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    }
}
