using System;
using System.Diagnostics;
using System.Threading;

namespace RhaegarMove
{
    internal sealed class OperationWorker : IDisposable
    {
        private readonly object gate = new object();
        private readonly AutoResetEvent workEvent = new AutoResetEvent(false);
        private readonly Thread thread;
        private readonly AppSettings settings;
        private bool disposed;
        private bool stopping;
        private bool hasQueuedMove;
        private POINT queuedPoint;
        private OperationState state = new OperationState();

        public OperationWorker(AppSettings settings)
        {
            this.settings = settings;
            thread = new Thread(Loop);
            thread.IsBackground = true;
            thread.Name = "RhaegarMove.OperationWorker";
            thread.Start();
        }

        public bool Active { get { lock (gate) return state.Active; } }
        public MouseButton ActiveButton { get { lock (gate) return state.Button; } }

        public bool Begin(IntPtr target, MouseButton button, OperationKind kind, POINT pt)
        {
            lock (gate)
            {
                if (state.Active || target == IntPtr.Zero)
                    return false;

                RECT rect;
                if (!Geometry.TryGetBestWindowRect(target, out rect))
                    return false;

                if (kind == OperationKind.Move && (NativeMethods.IsZoomed(target) || WindowRestoreStore.IsSnapped(target)))
                {
                    WindowController.RestoreForDrag(target, pt, rect, settings);
                    if (!Geometry.TryGetBestWindowRect(target, out rect))
                        return false;
                }

                state = new OperationState();
                state.Active = true;
                state.Target = target;
                state.Button = button;
                state.Kind = kind;
                state.StartMouse = pt;
                state.StartRect = rect;
                state.LastRect = rect;
                state.Edge = kind == OperationKind.Resize ? ResizeEdge.FromPoint(rect, pt, settings) : ResizeEdge.None;
                state.LastPoint = pt;
                state.LastTick = Stopwatch.GetTimestamp();

                WindowController.NotifySizeMove(target, true, settings);
                return true;
            }
        }

        public void QueueMove(POINT pt)
        {
            lock (gate)
            {
                if (!state.Active)
                    return;
                queuedPoint = pt;
                hasQueuedMove = true;
            }
            workEvent.Set();
        }

        public MouseButton Finish(bool returnButtonForSwallow)
        {
            MouseButton button;
            lock (gate)
            {
                if (!state.Active)
                    return MouseButton.None;
                button = state.Button;
                FinishLocked();
            }
            return returnButtonForSwallow ? button : MouseButton.None;
        }

        public void Cancel()
        {
            lock (gate)
            {
                if (state.Active)
                    FinishLocked();
            }
        }

        public void Watchdog()
        {
            lock (gate)
            {
                if (state.Active && (!Geometry.IsAltDown() || !NativeMethods.IsWindow(state.Target)))
                    FinishLocked();
            }
        }

        private void FinishLocked()
        {
            if (state.Target != IntPtr.Zero && NativeMethods.IsWindow(state.Target))
                WindowController.NotifySizeMove(state.Target, false, settings);
            state = new OperationState();
            hasQueuedMove = false;
        }

        private void Loop()
        {
            while (true)
            {
                workEvent.WaitOne();
                if (stopping)
                    return;

                POINT pt;
                bool doWork;
                lock (gate)
                {
                    doWork = state.Active && hasQueuedMove;
                    pt = queuedPoint;
                    hasQueuedMove = false;
                }

                if (doWork)
                    Update(pt);
            }
        }

        private void Update(POINT pt)
        {
            OperationState snapshot;
            int speed;
            lock (gate)
            {
                if (!state.Active || !NativeMethods.IsWindow(state.Target))
                    return;
                speed = UpdateSpeedLocked(pt);
                snapshot = state.Clone();
            }

            RECT result = snapshot.StartRect;
            if (snapshot.Kind == OperationKind.Move)
                result = UpdateMove(snapshot, pt, speed);
            else if (snapshot.Kind == OperationKind.Resize)
                result = UpdateResize(snapshot, pt);

            lock (gate)
            {
                if (state.Active && state.Target == snapshot.Target)
                    state.LastRect = result;
            }
        }

        private int UpdateSpeedLocked(POINT pt)
        {
            long now = Stopwatch.GetTimestamp();
            long elapsedTicks = Math.Max(1, now - state.LastTick);
            double elapsedMs = elapsedTicks * 1000.0 / Stopwatch.Frequency;
            int dx = pt.x - state.LastPoint.x;
            int dy = pt.y - state.LastPoint.y;
            int dist = (int)Math.Sqrt(dx * dx + dy * dy);
            state.LastPoint = pt;
            state.LastTick = now;
            return (int)(dist * settings.AeroSpeedTau / Math.Max(1.0, elapsedMs));
        }

        private RECT UpdateMove(OperationState s, POINT pt, int speed)
        {
            int dx = pt.x - s.StartMouse.x;
            int dy = pt.y - s.StartMouse.y;
            int width = s.StartRect.Width;
            int height = s.StartRect.Height;
            int x = s.StartRect.left + dx;
            int y = s.StartRect.top + dy;

            SnapEngine.TryApplyMoveSnap(s.Target, pt, ref x, ref y, ref width, ref height, s.StartRect, settings, speed);
            RECT result = new RECT(x, y, x + width, y + height);
            NativeMethods.SetWindowPos(s.Target, IntPtr.Zero, result.left, result.top, result.Width, result.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOOWNERZORDER | NativeMethods.SWP_NOACTIVATE);
            return result;
        }

        private RECT UpdateResize(OperationState s, POINT pt)
        {
            RECT desired = ResizeEngine.Calculate(s.StartRect, s.StartMouse, pt, s.Edge, settings);
            if (!s.Edge.MoveInstead)
            {
                SnapEngine.ApplyResizeSnap(s.Target, ref desired, s.Edge, settings);
                WindowController.SendSizing(s.Target, s.Edge, ref desired);
            }

            desired = new RECT(desired.left, desired.top, desired.left + Math.Max(settings.MinWidth, desired.Width), desired.top + Math.Max(settings.MinHeight, desired.Height));
            NativeMethods.SetWindowPos(s.Target, IntPtr.Zero, desired.left, desired.top, desired.Width, desired.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOOWNERZORDER | NativeMethods.SWP_NOACTIVATE);

            if (!s.Edge.MoveInstead)
                SnapEngine.ApplyStickyResize(s.Target, s.LastRect, desired, s.Edge, settings);

            return desired;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Cancel();
            stopping = true;
            workEvent.Set();
            thread.Join(1000);
            workEvent.Dispose();
        }

        private sealed class OperationState
        {
            public bool Active;
            public IntPtr Target;
            public MouseButton Button;
            public OperationKind Kind;
            public POINT StartMouse;
            public RECT StartRect;
            public RECT LastRect;
            public ResizeEdge Edge;
            public POINT LastPoint;
            public long LastTick;

            public OperationState Clone()
            {
                return (OperationState)MemberwiseClone();
            }
        }
    }
}
