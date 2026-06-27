using System;
using System.Diagnostics;
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

            IntPtr module = IntPtr.Zero;
            try
            {
                using (Process current = Process.GetCurrentProcess())
                using (ProcessModule main = current.MainModule)
                    module = NativeMethods.GetModuleHandle(main.ModuleName);
            }
            catch
            {
                module = IntPtr.Zero;
            }

            hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc, module, 0);
            if (hook == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                RuntimeControl.WriteRuntime("mouse hook install failed. error=" + error);
                throw new InvalidOperationException("Mouse hook could not be installed. Win32Error=" + error);
            }

            RuntimeControl.WriteRuntime("mouse hook installed " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
            catch (Exception ex)
            {
                RuntimeControl.WriteRuntime("hook callback error: " + ex.GetType().Name + " " + ex.Message);
                worker.Cancel();
                return NativeMethods.CallNextHookEx(hook, nCode, wParam, lParam);
            }
        }

        private bool TryBegin(POINT pt, MouseButton button, OperationKind kind)
        {
            IntPtr target = WindowController.FindTargetWindow(pt, settings);
            if (target == IntPtr.Zero)
            {
                RuntimeControl.WriteRuntime("gesture ignored: no target at " + pt.x + "," + pt.y);
                return false;
            }

            if (settings.EnableRuleDiagnostics)
                RuleDiagnostics.WriteSnapshot(kind.ToString(), target);

            if (kind == OperationKind.Resize)
            {
                string cls = Geometry.ClassName(target);
                if (!WindowRules.ShouldAllowResize(target, cls))
                {
                    RuntimeControl.WriteRuntime("resize ignored by NoResize rule: " + Geometry.ClassName(target));
                    return false;
                }
            }

            bool started = worker.Begin(target, button, kind, pt);
            if (!started)
                RuntimeControl.WriteRuntime("gesture begin failed: " + kind + " hwnd=" + target.ToInt64().ToString("X"));
            return started;
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
