using System;

namespace RhaegarMove
{
    internal struct ResizeEdge
    {
        public bool Left;
        public bool Right;
        public bool Top;
        public bool Bottom;
        public bool Symmetric;
        public bool MoveInstead;

        public static readonly ResizeEdge None = new ResizeEdge();

        public static ResizeEdge FromDelta(int dxLeft, int dxRight, int dyTop, int dyBottom)
        {
            ResizeEdge e = new ResizeEdge();
            e.Left = dxLeft != 0;
            e.Right = dxRight != 0;
            e.Top = dyTop != 0;
            e.Bottom = dyBottom != 0;
            return e;
        }

        public static ResizeEdge FromPoint(RECT rect, POINT pt, AppSettings settings)
        {
            ResizeEdge e = new ResizeEdge();
            int width = Math.Max(1, rect.Width);
            int height = Math.Max(1, rect.Height);
            int localX = Geometry.Clamp(pt.x - rect.left, 0, width);
            int localY = Geometry.Clamp(pt.y - rect.top, 0, height);
            int xPct = localX * 100 / width;
            int yPct = localY * 100 / height;

            int side = Geometry.Clamp(settings.SidesFraction, 1, 100);
            int center = Geometry.Clamp(settings.CenterFraction, 0, 90);
            int centerMin = 50 - center / 2;
            int centerMax = 50 + center / 2;
            bool inCenter = center > 0 && xPct >= centerMin && xPct <= centerMax && yPct >= centerMin && yPct <= centerMax;

            if (inCenter)
            {
                if (settings.ResizeCenterMode == 1)
                {
                    e.Symmetric = true;
                    return e;
                }
                if (settings.ResizeCenterMode == 2)
                {
                    e.MoveInstead = true;
                    return e;
                }
            }

            bool nearLeft = xPct <= side;
            bool nearRight = xPct >= 100 - side;
            bool nearTop = yPct <= side;
            bool nearBottom = yPct >= 100 - side;

            e.Left = nearLeft;
            e.Right = nearRight;
            e.Top = nearTop;
            e.Bottom = nearBottom;

            if (!e.Left && !e.Right && !e.Top && !e.Bottom)
            {
                e.Left = pt.x < rect.left + width / 2;
                e.Right = !e.Left;
                e.Top = pt.y < rect.top + height / 2;
                e.Bottom = !e.Top;
            }

            return e;
        }

        public int ToSizingCode()
        {
            if (Symmetric || MoveInstead)
                return 0;
            if (Top && Left) return NativeMethods.WMSZ_TOPLEFT;
            if (Top && Right) return NativeMethods.WMSZ_TOPRIGHT;
            if (Bottom && Left) return NativeMethods.WMSZ_BOTTOMLEFT;
            if (Bottom && Right) return NativeMethods.WMSZ_BOTTOMRIGHT;
            if (Left) return NativeMethods.WMSZ_LEFT;
            if (Right) return NativeMethods.WMSZ_RIGHT;
            if (Top) return NativeMethods.WMSZ_TOP;
            if (Bottom) return NativeMethods.WMSZ_BOTTOM;
            return 0;
        }
    }

    internal static class ResizeEngine
    {
        public static RECT Calculate(RECT start, POINT startMouse, POINT currentMouse, ResizeEdge edge, AppSettings settings)
        {
            int dx = currentMouse.x - startMouse.x;
            int dy = currentMouse.y - startMouse.y;
            int left = start.left;
            int top = start.top;
            int right = start.right;
            int bottom = start.bottom;

            if (edge.MoveInstead)
            {
                return new RECT(start.left + dx, start.top + dy, start.right + dx, start.bottom + dy);
            }

            if (edge.Symmetric)
            {
                left -= dx;
                right += dx;
                top -= dy;
                bottom += dy;
            }
            else
            {
                if (edge.Left) left += dx;
                if (edge.Right) right += dx;
                if (edge.Top) top += dy;
                if (edge.Bottom) bottom += dy;
            }

            if (right - left < settings.MinWidth)
            {
                if (edge.Left && !edge.Right) left = right - settings.MinWidth;
                else if (edge.Right && !edge.Left) right = left + settings.MinWidth;
                else
                {
                    int center = (left + right) / 2;
                    left = center - settings.MinWidth / 2;
                    right = left + settings.MinWidth;
                }
            }

            if (bottom - top < settings.MinHeight)
            {
                if (edge.Top && !edge.Bottom) top = bottom - settings.MinHeight;
                else if (edge.Bottom && !edge.Top) bottom = top + settings.MinHeight;
                else
                {
                    int center = (top + bottom) / 2;
                    top = center - settings.MinHeight / 2;
                    bottom = top + settings.MinHeight;
                }
            }

            return new RECT(left, top, right, bottom);
        }
    }
}
