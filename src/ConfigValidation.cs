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
            b.AppendLine("EnablePreviewOnlySnap=" + s.EnablePreviewOnlySnap);
            b.AppendLine("EnableTrayIcon=" + s.EnableTrayIcon);
            b.AppendLine();
            b.AppendLine("[unknown keys]");
            AppendListOrNone(b, s.UnknownConfigKeys);
            b.AppendLine();
            b.AppendLine("[duplicate keys]");
            AppendListOrNone(b, s.DuplicateConfigKeys);
            b.AppendLine();
            b.AppendLine("[normalization notes]");
            AppendListOrNone(b, s.NormalizationNotes);
            b.AppendLine();
            b.AppendLine("[rule validation]");
            b.Append(RuleValidation.BuildReport());
            b.AppendLine();
            b.AppendLine("[warnings]");
            AppendWarnings(b, s);
            return b.ToString();
        }

        private static string GetStatus(AppSettings s)
        {
            if (s.UnknownConfigKeys.Count > 0 || s.DuplicateConfigKeys.Count > 0 || s.NormalizationNotes.Count > 0)
                return "config has unknown, duplicate, or normalized values";
            if (s.StickyResize || s.EnablePreviewOverlay || s.EnablePreviewOnlySnap || s.EnableSnapDiagnostics || s.EnableRuleDiagnostics || s.EnableTrayIcon)
                return "advanced diagnostics/features enabled";
            return "safe defaults or normal options";
        }

        private static void AppendListOrNone(StringBuilder b, System.Collections.Generic.List<string> values)
        {
            if (values.Count == 0)
            {
                b.AppendLine("- none");
                return;
            }
            for (int i = 0; i < values.Count; i++)
                b.AppendLine("- " + values[i]);
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
            if (s.EnablePreviewOnlySnap)
            {
                b.AppendLine("- Preview-only snap is enabled; moves/resizes are previewed and committed on release.");
                any = true;
            }
            if (s.EnableSnapDiagnostics)
            {
                b.AppendLine("- Snap diagnostics are enabled; snap-target and scoring reports may be rewritten often during gestures.");
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
            if (s.EnableTrayIcon)
            {
                b.AppendLine("- Tray icon is enabled. Default UX is trayless; use it only if desired.");
                any = true;
            }
            if (!any)
                b.AppendLine("- none");
        }
    }
}
