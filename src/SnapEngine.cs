using System;
using System.Collections.Generic;

namespace RhaegarMove
{
    internal static class SnapEngine
    {
        private sealed class SnapTarget
        {
            public IntPtr Hwnd;
            public RECT Rect;
        }

        public static int LastRestoreFlags { get; private set; }

        public static bool TryApplyMoveSnap(IntPtr hwnd, POINT pt, ref int x, ref int y, ref int width, ref int height, RECT origin, AppSettings settings, int speed)
        {
            LastRestoreFlags = 0;
            bool changed = false;
            RECT before = new RECT(x, y, x + width, y + height);

            if (settings.EnableAeroSnap && speed <= settings.AeroMaxSpeed)
            {
                RECT aero;
                int flags;
                if (TryGetAeroRect(pt, out aero, out flags, settings))
                {
                    int dpi = DpiHelper.GetWindowDpi(hwnd);
                    WindowRestoreStore.Set(hwnd, Math.Max(settings.MinWidth, origin.Width), Math.Max(settings.MinHeight, origin.Height), flags, dpi);
                    x = aero.left;
                    y = aero.top;
                    width = Math.Max(settings.MinWidth, aero.Width);
                    height = Math.Max(settings.MinHeight, aero.Height);
                    LastRestoreFlags = flags;
                    if (settings.EnableSnapDiagnostics)
                    {
                        SnapScoreDiagnostics.BeginSession(settings, "move-aero");
                        SnapScoreDiagnostics.FinalDecision(settings, "move-aero", "aero", before, new RECT(x, y, x + width, y + height));
                    }
                    return true;
                }
            }

            if (!settings.EnableEdgeSnap || settings.AutoSnap <= 0)
                return false;

            SnapScoreDiagnostics.BeginSession(settings, "move");
            RECT desired = before;
            int threshold = Math.Max(0, settings.SnapThreshold);
            SnapToMonitorEdges(ref desired, pt, threshold, settings.SnapGap, settings);
            bool monitorChanged = !SameRect(before, desired);
            RECT afterMonitor = desired;

            bool windowChanged = false;
            if (settings.SnapToWindows && settings.AutoSnap >= 2)
            {
                List<SnapTarget> targets = CollectTargets(hwnd, settings);
                SnapToWindowEdges(ref desired, targets, threshold, settings.SnapGap, settings.AutoSnap >= 3, settings);
                windowChanged = !SameRect(afterMonitor, desired);
            }

            if (desired.left != x || desired.top != y)
            {
                x = desired.left;
                y = desired.top;
                changed = true;
            }

            SnapScoreDiagnostics.FinalDecision(settings, "move", ClassifySource(monitorChanged, windowChanged), before, desired);
            return changed;
        }

        public static void ApplyResizeSnap(IntPtr hwnd, ref RECT desired, ResizeEdge edge, AppSettings settings)
        {
            if (!settings.EnableEdgeSnap || settings.AutoSnap <= 0)
                return;

            RECT before = desired;
            SnapScoreDiagnostics.BeginSession(settings, "resize");
            int threshold = Math.Max(0, settings.SnapThreshold);
            SnapResizeToMonitor(ref desired, edge, threshold, settings.SnapGap, settings);
            bool monitorChanged = !SameRect(before, desired);
            RECT afterMonitor = desired;

            bool windowChanged = false;
            if (settings.SnapToWindows && settings.AutoSnap >= 2)
            {
                List<SnapTarget> targets = CollectTargets(hwnd, settings);
                SnapResizeToWindows(ref desired, edge, targets, threshold, settings.SnapGap, settings.AutoSnap >= 3, settings);
                windowChanged = !SameRect(afterMonitor, desired);
            }

            SnapScoreDiagnostics.FinalDecision(settings, "resize", ClassifySource(monitorChanged, windowChanged), before, desired);
        }

        public static void ApplyStickyResize(IntPtr active, RECT before, RECT after, ResizeEdge edge, AppSettings settings)
        {
            if (!settings.StickyResize)
                return;

            int dxLeft = after.left - before.left;
            int dxRight = after.right - before.right;
            int dyTop = after.top - before.top;
            int dyBottom = after.bottom - before.bottom;
            if (dxLeft == 0 && dxRight == 0 && dyTop == 0 && dyBottom == 0)
                return;

            int threshold = Math.Max(1, settings.SnapThreshold);
            List<SnapTarget> targets = CollectTargets(active, settings);
            foreach (SnapTarget target in targets)
            {
                RECT r = target.Rect;
                bool changed = false;

                if (edge.Right && Math.Abs(r.left - before.right) <= threshold && Geometry.RectsOverlapVertically(before, r, threshold))
                {
                    r.left += dxRight;
                    changed = true;
                }
                if (edge.Left && Math.Abs(r.right - before.left) <= threshold && Geometry.RectsOverlapVertically(before, r, threshold))
                {
                    r.right += dxLeft;
                    changed = true;
                }
                if (edge.Bottom && Math.Abs(r.top - before.bottom) <= threshold && Geometry.RectsOverlapHorizontally(before, r, threshold))
                {
                    r.top += dyBottom;
                    changed = true;
                }
                if (edge.Top && Math.Abs(r.bottom - before.top) <= threshold && Geometry.RectsOverlapHorizontally(before, r, threshold))
                {
                    r.bottom += dyTop;
                    changed = true;
                }

                if (changed && r.Width >= settings.MinWidth && r.Height >= settings.MinHeight)
                {
                    NativeMethods.SetWindowPos(target.Hwnd, IntPtr.Zero, r.left, r.top, r.Width, r.Height,
                        NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOOWNERZORDER | NativeMethods.SWP_NOACTIVATE);
                }
            }
        }

        private static bool TryGetAeroRect(POINT pt, out RECT rect, out int flags, AppSettings settings)
        {
            rect = new RECT();
            flags = 0;
            RECT work;
            if (!Geometry.TryGetMonitorWorkArea(pt, out work))
                return false;

            int threshold = Math.Max(1, settings.AeroThreshold);
            bool nearLeft = Math.Abs(pt.x - work.left) <= threshold;
            bool nearRight = Math.Abs(pt.x - work.right) <= threshold;
            bool nearTop = Math.Abs(pt.y - work.top) <= threshold;
            bool nearBottom = Math.Abs(pt.y - work.bottom) <= threshold;

            int midX = work.left + work.Width / 2;
            int midY = work.top + work.Height / 2;

            if (nearLeft && nearTop) { rect = new RECT(work.left, work.top, midX, midY); flags = RestoreFlags.Snapped | RestoreFlags.Left | RestoreFlags.Top; return true; }
            if (nearLeft && nearBottom) { rect = new RECT(work.left, midY, midX, work.bottom); flags = RestoreFlags.Snapped | RestoreFlags.Left | RestoreFlags.Bottom; return true; }
            if (nearRight && nearTop) { rect = new RECT(midX, work.top, work.right, midY); flags = RestoreFlags.Snapped | RestoreFlags.Right | RestoreFlags.Top; return true; }
            if (nearRight && nearBottom) { rect = new RECT(midX, midY, work.right, work.bottom); flags = RestoreFlags.Snapped | RestoreFlags.Right | RestoreFlags.Bottom; return true; }
            if (nearTop) { rect = new RECT(work.left, work.top, work.right, work.bottom); flags = RestoreFlags.Snapped | RestoreFlags.Maximized; return true; }
            if (nearLeft) { rect = new RECT(work.left, work.top, midX, work.bottom); flags = RestoreFlags.Snapped | RestoreFlags.Left; return true; }
            if (nearRight) { rect = new RECT(midX, work.top, work.right, work.bottom); flags = RestoreFlags.Snapped | RestoreFlags.Right; return true; }
            return false;
        }

        private static void SnapToMonitorEdges(ref RECT desired, POINT pt, int threshold, int gap, AppSettings settings)
        {
            RECT work;
            if (!Geometry.TryGetMonitorWorkArea(pt, out work))
                return;

            int width = desired.Width;
            int height = desired.Height;
            SnapScoreDiagnostics score = new SnapScoreDiagnostics(settings, "move-monitor", desired, threshold);
            score.Candidate("monitor-left", work.left + gap - desired.left, threshold);
            score.Candidate("monitor-top", work.top + gap - desired.top, threshold);
            score.Candidate("monitor-right", work.right - width - gap - desired.left, threshold);
            score.Candidate("monitor-bottom", work.bottom - height - gap - desired.top, threshold);
            score.Flush();

            if (Math.Abs(desired.left - work.left) <= threshold) desired.left = work.left + gap;
            if (Math.Abs(desired.top - work.top) <= threshold) desired.top = work.top + gap;
            if (Math.Abs(desired.right - work.right) <= threshold) desired.left = work.right - width - gap;
            if (Math.Abs(desired.bottom - work.bottom) <= threshold) desired.top = work.bottom - height - gap;
            desired.right = desired.left + width;
            desired.bottom = desired.top + height;
        }

        private static void SnapResizeToMonitor(ref RECT desired, ResizeEdge edge, int threshold, int gap, AppSettings settings)
        {
            POINT center = new POINT((desired.left + desired.right) / 2, (desired.top + desired.bottom) / 2);
            RECT work;
            if (!Geometry.TryGetMonitorWorkArea(center, out work))
                return;

            SnapScoreDiagnostics score = new SnapScoreDiagnostics(settings, "resize-monitor", desired, threshold);
            if (edge.Left) score.Candidate("resize-left-to-monitor-left", desired.left - work.left, threshold);
            if (edge.Right) score.Candidate("resize-right-to-monitor-right", desired.right - work.right, threshold);
            if (edge.Top) score.Candidate("resize-top-to-monitor-top", desired.top - work.top, threshold);
            if (edge.Bottom) score.Candidate("resize-bottom-to-monitor-bottom", desired.bottom - work.bottom, threshold);
            score.Flush();

            if (edge.Left && Math.Abs(desired.left - work.left) <= threshold) desired.left = work.left + gap;
            if (edge.Right && Math.Abs(desired.right - work.right) <= threshold) desired.right = work.right - gap;
            if (edge.Top && Math.Abs(desired.top - work.top) <= threshold) desired.top = work.top + gap;
            if (edge.Bottom && Math.Abs(desired.bottom - work.bottom) <= threshold) desired.bottom = work.bottom - gap;
        }

        private static void SnapToWindowEdges(ref RECT desired, List<SnapTarget> targets, int threshold, int gap, bool inside, AppSettings settings)
        {
            int width = desired.Width;
            int height = desired.Height;
            int bestDx = 0;
            int bestDy = 0;
            int bestAbsX = threshold + 1;
            int bestAbsY = threshold + 1;
            SnapScoreDiagnostics score = new SnapScoreDiagnostics(settings, "move-window", desired, threshold);

            foreach (SnapTarget target in targets)
            {
                RECT r = target.Rect;
                if (Geometry.RectsOverlapVertically(desired, r, threshold))
                {
                    Consider("move-left-to-target-right", r.right + gap - desired.left, ref bestDx, ref bestAbsX, threshold, score);
                    Consider("move-right-to-target-left", r.left - gap - desired.right, ref bestDx, ref bestAbsX, threshold, score);
                    if (inside)
                    {
                        Consider("move-left-to-target-left", r.left + gap - desired.left, ref bestDx, ref bestAbsX, threshold, score);
                        Consider("move-right-to-target-right", r.right - gap - desired.right, ref bestDx, ref bestAbsX, threshold, score);
                    }
                }
                if (Geometry.RectsOverlapHorizontally(desired, r, threshold))
                {
                    Consider("move-top-to-target-bottom", r.bottom + gap - desired.top, ref bestDy, ref bestAbsY, threshold, score);
                    Consider("move-bottom-to-target-top", r.top - gap - desired.bottom, ref bestDy, ref bestAbsY, threshold, score);
                    if (inside)
                    {
                        Consider("move-top-to-target-top", r.top + gap - desired.top, ref bestDy, ref bestAbsY, threshold, score);
                        Consider("move-bottom-to-target-bottom", r.bottom - gap - desired.bottom, ref bestDy, ref bestAbsY, threshold, score);
                    }
                }
            }

            score.Flush();
            desired.left += bestDx;
            desired.top += bestDy;
            desired.right = desired.left + width;
            desired.bottom = desired.top + height;
        }

        private static void SnapResizeToWindows(ref RECT desired, ResizeEdge edge, List<SnapTarget> targets, int threshold, int gap, bool inside, AppSettings settings)
        {
            SnapScoreDiagnostics score = new SnapScoreDiagnostics(settings, "resize-window", desired, threshold);
            foreach (SnapTarget target in targets)
            {
                RECT r = target.Rect;
                if (Geometry.RectsOverlapVertically(desired, r, threshold))
                {
                    if (edge.Left) ScoreResizeCandidate("resize-left-to-target-right", desired.left - r.right, score, threshold);
                    if (edge.Right) ScoreResizeCandidate("resize-right-to-target-left", desired.right - r.left, score, threshold);
                    if (edge.Left && Math.Abs(desired.left - r.right) <= threshold) desired.left = r.right + gap;
                    if (edge.Right && Math.Abs(desired.right - r.left) <= threshold) desired.right = r.left - gap;
                    if (inside)
                    {
                        if (edge.Left) ScoreResizeCandidate("resize-left-to-target-left", desired.left - r.left, score, threshold);
                        if (edge.Right) ScoreResizeCandidate("resize-right-to-target-right", desired.right - r.right, score, threshold);
                        if (edge.Left && Math.Abs(desired.left - r.left) <= threshold) desired.left = r.left + gap;
                        if (edge.Right && Math.Abs(desired.right - r.right) <= threshold) desired.right = r.right - gap;
                    }
                }
                if (Geometry.RectsOverlapHorizontally(desired, r, threshold))
                {
                    if (edge.Top) ScoreResizeCandidate("resize-top-to-target-bottom", desired.top - r.bottom, score, threshold);
                    if (edge.Bottom) ScoreResizeCandidate("resize-bottom-to-target-top", desired.bottom - r.top, score, threshold);
                    if (edge.Top && Math.Abs(desired.top - r.bottom) <= threshold) desired.top = r.bottom + gap;
                    if (edge.Bottom && Math.Abs(desired.bottom - r.top) <= threshold) desired.bottom = r.top - gap;
                    if (inside)
                    {
                        if (edge.Top) ScoreResizeCandidate("resize-top-to-target-top", desired.top - r.top, score, threshold);
                        if (edge.Bottom) ScoreResizeCandidate("resize-bottom-to-target-bottom", desired.bottom - r.bottom, score, threshold);
                        if (edge.Top && Math.Abs(desired.top - r.top) <= threshold) desired.top = r.top + gap;
                        if (edge.Bottom && Math.Abs(desired.bottom - r.bottom) <= threshold) desired.bottom = r.bottom - gap;
                    }
                }
            }
            score.Flush();
        }

        private static void ScoreResizeCandidate(string label, int delta, SnapScoreDiagnostics score, int threshold)
        {
            score.Candidate(label, delta, threshold);
        }

        private static void Consider(string label, int delta, ref int bestDelta, ref int bestAbs, int threshold, SnapScoreDiagnostics score)
        {
            score.Candidate(label, delta, threshold);
            int abs = Math.Abs(delta);
            if (abs <= threshold && abs < bestAbs)
            {
                bestAbs = abs;
                bestDelta = delta;
            }
        }

        private static bool SameRect(RECT a, RECT b)
        {
            return a.left == b.left && a.top == b.top && a.right == b.right && a.bottom == b.bottom;
        }

        private static string ClassifySource(bool monitorChanged, bool windowChanged)
        {
            if (monitorChanged && windowChanged) return "monitor+window";
            if (monitorChanged) return "monitor";
            if (windowChanged) return "window";
            return "none";
        }

        private static List<SnapTarget> CollectTargets(IntPtr active, AppSettings settings)
        {
            List<SnapTarget> targets = new List<SnapTarget>();
            SnapDiagnostics diagnostics = new SnapDiagnostics(settings);
            NativeMethods.EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (hwnd == active)
                {
                    diagnostics.Reject(hwnd, "active window");
                    return true;
                }
                if (hwnd == IntPtr.Zero)
                {
                    diagnostics.Reject(hwnd, "zero hwnd");
                    return true;
                }
                if (!NativeMethods.IsWindowVisible(hwnd))
                {
                    diagnostics.Reject(hwnd, "not visible");
                    return true;
                }
                if (NativeMethods.IsIconic(hwnd))
                {
                    diagnostics.Reject(hwnd, "minimized");
                    return true;
                }

                string cls = Geometry.ClassName(hwnd);
                if (WindowRules.ShouldIgnoreWindow(hwnd, cls))
                {
                    diagnostics.Reject(hwnd, "ignored rule " + WindowRules.ExplainWindow(hwnd, cls).Replace(Environment.NewLine, " | "));
                    return true;
                }
                if (!WindowRules.ShouldSnapToWindow(hwnd, cls))
                {
                    diagnostics.Reject(hwnd, "not in SnapList");
                    return true;
                }

                long style = NativeMethods.GetWindowLongPtrSafe(hwnd, NativeMethods.GWL_STYLE).ToInt64();
                long exstyle = NativeMethods.GetWindowLongPtrSafe(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
                if ((exstyle & NativeMethods.WS_EX_NOACTIVATE) != 0)
                {
                    diagnostics.Reject(hwnd, "noactivate");
                    return true;
                }
                if ((style & NativeMethods.WS_CAPTION) == 0 && (style & NativeMethods.WS_THICKFRAME) == 0)
                {
                    diagnostics.Reject(hwnd, "no caption or thickframe");
                    return true;
                }

                RECT rect;
                if (Geometry.TryGetBestWindowRect(hwnd, out rect) && !rect.IsEmpty)
                {
                    targets.Add(new SnapTarget { Hwnd = hwnd, Rect = rect });
                    diagnostics.Accept(hwnd, "top-level window", rect);
                }
                else
                {
                    diagnostics.Reject(hwnd, "empty rect");
                }
                return true;
            }, IntPtr.Zero);
            diagnostics.Flush();
            return targets;
        }
    }
}
