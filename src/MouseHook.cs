using System;
using System.Runtime.InteropServices;

namespace RhaegarMove
{
    internal sealed class MouseHook : IDisposable
    {
        private readonly AppSettings settings;
        private readonly OperationWorker worker;
        private readonly LowLevelProc proc;
        private IntPtr hook;
        private MouseButton swallowNextUp;
        private bool disposed;

        public MouseHook(AppSettings settings, OperationWorker worker)
        {
            this.settings = settings;
            this.worker = worker;
            proc = HookProc;
        }

        public void Install()
        {
            if (hook != IntPtr.Zero)
                return;

            hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc, IntPtr.Zero, 0);
            if (hook == IntPtr.Zero)
                throw new InvalidOperationException("Mouse hook could not be installed.");
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode != NativeMethods.HC_ACTION)
                    return NativeMethods.CallNextHookEx(hook, nCode, wParam, lParam);

                int msg = wParam.ToInt32();
                MSLLHOOKSTRUCT data = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                POINT pt = data.pt;

                MouseButton upButton = UpButtonFromMessage(msg);
                if (upButton != MouseButton.None && swallowNextUp == upButton)
                {
                    swallowNextUp = MouseButton.None;
                    return new IntPtr(1);
                }

                if (worker.Active)
                {
                    if (msg == NativeMethods.WM_MOUSEMOVE)
                    {
                        if (!Geometry.IsAltDown())
                            swallowNextUp = worker.Finish(true);
                        else
                            worker.QueueMove(pt);
                        return new IntPtr(1);
                    }

                    if (upButton == worker.ActiveButton)
                    {
                        worker.Finish(false);
                        return new IntPtr(1);
                    }

                    return new IntPtr(1);
                }

                if (!Geometry.IsAltDown())
                    return NativeMethods.CallNextHookEx(hook, nCode, wParam, lParam);

                if (msg == NativeMethods.WM_LBUTTONDOWN)
                {
                    if (TryBegin(pt, MouseButton.Left, OperationKind.Move))
                        return new IntPtr(1);
                }
                else if (msg == NativeMethods.WM_RBUTTONDOWN)
                {
                    if (TryBegin(pt, MouseButton.Right, OperationKind.Resize))
                        return new IntPtr(1);
                }

                return NativeMethods.CallNextHookEx(hook, nCode, wParam, lParam);
            }
            catch
            {
                worker.Cancel();
                return NativeMethods.CallNextHookEx(hook, nCode, wParam, lParam);
            }
        }

        private bool TryBegin(POINT pt, MouseButton button, OperationKind kind)
        {
            IntPtr target = WindowController.FindTargetWindow(pt, settings);
            if (target == IntPtr.Zero)
                return false;

            if (settings.EnableRuleDiagnostics)
                RuleDiagnostics.WriteSnapshot(kind.ToString(), target);

            if (kind == OperationKind.Resize)
            {
                string cls = Geometry.ClassName(target);
                if (!WindowRules.ShouldAllowResize(target, cls))
                    return false;
            }

            return worker.Begin(target, button, kind, pt);
        }

        private static MouseButton UpButtonFromMessage(int msg)
        {
            if (msg == NativeMethods.WM_LBUTTONUP) return MouseButton.Left;
            if (msg == NativeMethods.WM_RBUTTONUP) return MouseButton.Right;
            return MouseButton.None;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(hook);
                hook = IntPtr.Zero;
            }
        }
    }
}
