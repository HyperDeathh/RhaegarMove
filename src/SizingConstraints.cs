using System;

namespace RhaegarMove
{
    internal static class SizingConstraints
    {
        public static RECT Apply(RECT desired, ResizeEdge edge, AppSettings settings)
        {
            int left = desired.left;
            int top = desired.top;
            int right = desired.right;
            int bottom = desired.bottom;

            int width = Math.Max(1, right - left);
            int height = Math.Max(1, bottom - top);
            int minWidth = Math.Max(40, settings.MinWidth);
            int minHeight = Math.Max(40, settings.MinHeight);
            int maxWidth = settings.MaxWidth <= 0 ? int.MaxValue : Math.Max(minWidth, settings.MaxWidth);
            int maxHeight = settings.MaxHeight <= 0 ? int.MaxValue : Math.Max(minHeight, settings.MaxHeight);

            int clampedWidth = Clamp(width, minWidth, maxWidth);
            int clampedHeight = Clamp(height, minHeight, maxHeight);

            if (clampedWidth != width)
            {
                if (edge.Left && !edge.Right)
                    left = right - clampedWidth;
                else if (edge.Right && !edge.Left)
                    right = left + clampedWidth;
                else
                {
                    int center = (left + right) / 2;
                    left = center - clampedWidth / 2;
                    right = left + clampedWidth;
                }
            }

            if (clampedHeight != height)
            {
                if (edge.Top && !edge.Bottom)
                    top = bottom - clampedHeight;
                else if (edge.Bottom && !edge.Top)
                    bottom = top + clampedHeight;
                else
                {
                    int center = (top + bottom) / 2;
                    top = center - clampedHeight / 2;
                    bottom = top + clampedHeight;
                }
            }

            return new RECT(left, top, right, bottom);
        }

        public static RECT KeepInsideWorkAreaIfHuge(RECT desired, POINT anchor)
        {
            RECT work;
            if (!Geometry.TryGetMonitorWorkArea(anchor, out work))
                return desired;

            if (desired.Width > work.Width)
            {
                desired.left = work.left;
                desired.right = work.right;
            }
            if (desired.Height > work.Height)
            {
                desired.top = work.top;
                desired.bottom = work.bottom;
            }
            return desired;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
