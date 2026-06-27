using System;
using System.IO;
using System.Text;

namespace RhaegarMove
{
    internal static class ConfigValidation
    {
        public static void WriteReport(AppSettings s, string reason)
        {
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                File.WriteAllText(Path.Combine(RuntimeControl.ControlDir, "config-report.txt"), BuildReport(s, reason));
            }
            catch
            {
            }
        }

        private static string BuildReport(AppSettings s, string reason)
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("reason=" + reason);
            b.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            b.AppendLine("status=" + GetStatus(s));
            b.AppendLine();
            b.AppendLine("[normalized values]");
            b.AppendLine("SnapThreshold=" + s.SnapThreshold);
            b.AppendLine("MinWidth=" + s.MinWidth);
            b.AppendLine("MinHeight=" + s.MinHeight);
            b.AppendLine("MaxWidth=" + s.MaxWidth);
            b.AppendLine("MaxHeight=" + s.MaxHeight);
            b.AppendLine("WatchdogMs=" + s.WatchdogMs);
            b.AppendLine("AeroThreshold=" + s.AeroThreshold);
            b.AppendLine("AutoSnap=" + s.AutoSnap);
            b.AppendLine("SnapGap=" + s.SnapGap);
            b.AppendLine("ResizeCenterMode=" + s.ResizeCenterMode);
            b.AppendLine("CenterFraction=" + s.CenterFraction);
            b.AppendLine("SidesFraction=" + s.SidesFraction);
            b.AppendLine("AeroMaxSpeed=" + s.AeroMaxSpeed);
            b.AppendLine("AeroSpeedTau=" + s.AeroSpeedTau);
            b.AppendLine();
            b.AppendLine("[warnings]");
            AppendWarnings(b, s);
            return b.ToString();
        }

        private static string GetStatus(AppSettings s)
        {
            if (s.StickyResize || s.EnablePreviewOverlay || s.EnableSnapDiagnostics || s.EnableRuleDiagnostics)
                return "advanced diagnostics/features enabled";
            return "safe defaults or normal options";
        }

        private static void AppendWarnings(StringBuilder b, AppSettings s)
        {
            bool any = false;
            if (s.SnapThreshold == 0)
            {
                b.AppendLine("- SnapThreshold=0 disables practical edge snap distance.");
                any = true;
            }
            if (s.SnapThreshold > 64)
            {
                b.AppendLine("- SnapThreshold is high; windows may snap from farther away than expected.");
                any = true;
            }
            if (s.StickyResize)
            {
                b.AppendLine("- StickyResize is enabled; adjacent windows may be resized too.");
                any = true;
            }
            if (s.EnablePreviewOverlay)
            {
                b.AppendLine("- Preview overlay is enabled; a topmost transparent outline window may appear.");
                any = true;
            }
            if (s.EnableSnapDiagnostics)
            {
                b.AppendLine("- Snap diagnostics are enabled; snap-target reports may be rewritten often during gestures.");
                any = true;
            }
            if (s.EnableRuleDiagnostics)
            {
                b.AppendLine("- Rule diagnostics are enabled; selected window details are written on gesture start.");
                any = true;
            }
            if (s.MaxWidth > 0 || s.MaxHeight > 0)
            {
                b.AppendLine("- MaxWidth/MaxHeight limits are active; some windows may stop resizing before the monitor edge.");
                any = true;
            }
            if (!any)
                b.AppendLine("- none");
        }
    }
}
