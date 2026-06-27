param(
    [Parameter(Mandatory=$true)]
    [string]$InputPath,

    [Parameter(Mandatory=$true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$source = [System.IO.File]::ReadAllText($InputPath)

# Temporary compile fixes for the first source layout.
# These should be removed once src/RhaegarMove.cs is cleaned directly.
$source = $source.Replace('private readonly Timer watchdog;', 'private readonly System.Windows.Forms.Timer watchdog;')
$source = $source.Replace('watchdog = new Timer();', 'watchdog = new System.Windows.Forms.Timer();')
$source = $source.Replace('private enum Operation { None, Move, Resize }', 'private enum OperationKind { None, Move, Resize }')
$source = $source.Replace('Operation.Move', 'OperationKind.Move')
$source = $source.Replace('Operation.Resize', 'OperationKind.Resize')
$source = $source.Replace('Operation.None', 'OperationKind.None')
$source = $source.Replace('public Operation Operation;', 'public OperationKind Kind;')
$source = $source.Replace('state.Operation', 'state.Kind')
$source = $source.Replace('RECT desired = new RECT(left, top, right, bottom);', 'RECT desired = new RECT(); desired.left = left; desired.top = top; desired.right = right; desired.bottom = bottom;')

# Phase 2: route target filtering through the clean-room WindowRules layer.
$oldFilter = 'if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" || cls == "Button" || cls == "#32768")'
$newFilter = 'if (WindowRules.ShouldIgnoreWindow(hwnd, cls))'
$source = $source.Replace($oldFilter, $newFilter)

# Phase 2: prefer DWM extended frame bounds when available. This avoids invisible-border drift on Windows 10/11.
$source = $source.Replace('if (!GetWindowRect(target, out rect))', 'if (!TryGetBestWindowRect(target, out rect))')
$source = $source.Replace('if (!GetWindowRect(hwnd, out restored))', 'if (!TryGetBestWindowRect(hwnd, out restored))')

$geometryHelpers = @'
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        private static bool TryGetBestWindowRect(IntPtr hwnd, out RECT rect)
        {
            rect = new RECT();
            if (hwnd == IntPtr.Zero)
                return false;

            RECT dwmRect;
            if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out dwmRect, Marshal.SizeOf(typeof(RECT))) == 0)
            {
                if (dwmRect.Width > 0 && dwmRect.Height > 0)
                {
                    rect = dwmRect;
                    return true;
                }
            }

            return GetWindowRect(hwnd, out rect);
        }

        private static bool TryGetAeroSnapRect(POINT pt, int width, int height, out RECT rect)
        {
            rect = new RECT();
            if (!settings.EnableAeroSnap)
                return false;

            MONITORINFO info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            IntPtr monitor = MonitorFromPoint(pt, 2);
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
                return false;

            RECT work = info.rcWork;
            int threshold = Math.Max(1, settings.AeroThreshold);
            bool nearLeft = Math.Abs(pt.x - work.left) <= threshold;
            bool nearRight = Math.Abs(pt.x - work.right) <= threshold;
            bool nearTop = Math.Abs(pt.y - work.top) <= threshold;
            bool nearBottom = Math.Abs(pt.y - work.bottom) <= threshold;

            if (nearTop && !nearLeft && !nearRight)
            {
                rect.left = work.left;
                rect.top = work.top;
                rect.right = work.right;
                rect.bottom = work.bottom;
                return true;
            }

            int midX = work.left + work.Width / 2;
            int midY = work.top + work.Height / 2;

            if (nearLeft && nearTop)
            {
                rect.left = work.left;
                rect.top = work.top;
                rect.right = midX;
                rect.bottom = midY;
                return true;
            }
            if (nearLeft && nearBottom)
            {
                rect.left = work.left;
                rect.top = midY;
                rect.right = midX;
                rect.bottom = work.bottom;
                return true;
            }
            if (nearRight && nearTop)
            {
                rect.left = midX;
                rect.top = work.top;
                rect.right = work.right;
                rect.bottom = midY;
                return true;
            }
            if (nearRight && nearBottom)
            {
                rect.left = midX;
                rect.top = midY;
                rect.right = work.right;
                rect.bottom = work.bottom;
                return true;
            }
            if (nearLeft)
            {
                rect.left = work.left;
                rect.top = work.top;
                rect.right = midX;
                rect.bottom = work.bottom;
                return true;
            }
            if (nearRight)
            {
                rect.left = midX;
                rect.top = work.top;
                rect.right = work.right;
                rect.bottom = work.bottom;
                return true;
            }

            return false;
        }

'@
$source = $source.Replace('        private static void SnapToMonitorEdges', $geometryHelpers + '        private static void SnapToMonitorEdges')

$oldMove = @'
            if (settings.EnableEdgeSnap)
                SnapToMonitorEdges(pt, ref x, ref y, width, height);

            SetWindowPos(state.Target, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
'@
$newMove = @'
            RECT aeroRect;
            if (TryGetAeroSnapRect(pt, width, height, out aeroRect))
            {
                SetWindowPos(state.Target, IntPtr.Zero, aeroRect.left, aeroRect.top, aeroRect.Width, aeroRect.Height, SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
                return;
            }

            if (settings.EnableEdgeSnap)
                SnapToMonitorEdges(pt, ref x, ref y, width, height);

            SetWindowPos(state.Target, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
'@
$source = $source.Replace($oldMove, $newMove)

# Phase 3 settings.
$source = $source.Replace('public bool EnableEdgeSnap = true;', 'public bool EnableEdgeSnap = true;`r`n            public bool EnableAeroSnap = true;`r`n            public int AeroThreshold = 8;')
$source = $source.Replace('else if (key.Equals("EnableEdgeSnap", StringComparison.OrdinalIgnoreCase)) s.EnableEdgeSnap = ToBool(value, s.EnableEdgeSnap);', 'else if (key.Equals("EnableEdgeSnap", StringComparison.OrdinalIgnoreCase)) s.EnableEdgeSnap = ToBool(value, s.EnableEdgeSnap);`r`n                    else if (key.Equals("EnableAeroSnap", StringComparison.OrdinalIgnoreCase)) s.EnableAeroSnap = ToBool(value, s.EnableAeroSnap);`r`n                    else if (key.Equals("AeroThreshold", StringComparison.OrdinalIgnoreCase)) s.AeroThreshold = ToInt(value, s.AeroThreshold);')
$source = $source.Replace('s.WatchdogMs = Math.Max(100, s.WatchdogMs);', 's.WatchdogMs = Math.Max(100, s.WatchdogMs);`r`n                s.AeroThreshold = Math.Max(1, s.AeroThreshold);')

# Phase 2 native import.
$source = $source.Replace('[DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);', '[DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out RECT rect, int size);`r`n        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);')

$encoding = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($OutputPath, $source, $encoding)
