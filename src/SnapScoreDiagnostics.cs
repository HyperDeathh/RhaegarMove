using System;
using System.IO;
using System.Text;

namespace RhaegarMove
{
    internal sealed class SnapScoreDiagnostics
    {
        private readonly AppSettings settings;
        private readonly StringBuilder builder = new StringBuilder();
        private int candidates;
        private string bestLabel = "none";
        private int bestAbs = int.MaxValue;
        private int bestDelta;

        public static void BeginSession(AppSettings settings, string reason)
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                File.WriteAllText(Path.Combine(RuntimeControl.ControlDir, "snap-score.txt"),
                    "session=" + reason + Environment.NewLine +
                    "time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void FinalDecision(AppSettings settings, string kind, RECT before, RECT after)
        {
            FinalDecision(settings, kind, "unknown", before, after);
        }

        public static void FinalDecision(AppSettings settings, string kind, string source, RECT before, RECT after)
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            try
            {
                StringBuilder b = new StringBuilder();
                b.AppendLine("---");
                b.AppendLine("kind=" + kind + "-final");
                b.AppendLine("source=" + source);
                b.AppendLine("before=" + FormatRect(before));
                b.AppendLine("after=" + FormatRect(after));
                b.AppendLine("dx=" + (after.left - before.left));
                b.AppendLine("dy=" + (after.top - before.top));
                b.AppendLine("dw=" + (after.Width - before.Width));
                b.AppendLine("dh=" + (after.Height - before.Height));
                b.AppendLine("changed=" + (before.left != after.left || before.top != after.top || before.right != after.right || before.bottom != after.bottom));
                b.AppendLine();
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                File.AppendAllText(Path.Combine(RuntimeControl.ControlDir, "snap-score.txt"), b.ToString());
            }
            catch
            {
            }
        }

        public SnapScoreDiagnostics(AppSettings settings, string kind, RECT desired, int threshold)
        {
            this.settings = settings;
            if (settings.EnableSnapDiagnostics)
            {
                builder.AppendLine("---");
                builder.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.AppendLine("kind=" + kind);
                builder.AppendLine("threshold=" + threshold);
                builder.AppendLine("desired=" + FormatRect(desired));
            }
        }

        public void Candidate(string label, int delta, int threshold)
        {
            if (!settings.EnableSnapDiagnostics)
                return;

            candidates++;
            int abs = Math.Abs(delta);
            bool within = abs <= threshold;
            builder.AppendLine("candidate label=" + label + " delta=" + delta + " abs=" + abs + " within=" + within);
            if (within && abs < bestAbs)
            {
                bestAbs = abs;
                bestDelta = delta;
                bestLabel = label;
            }
        }

        public void Flush()
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            try
            {
                builder.AppendLine("candidates=" + candidates);
                builder.AppendLine("bestLabel=" + bestLabel);
                builder.AppendLine("bestDelta=" + bestDelta);
                builder.AppendLine("bestAbs=" + (bestAbs == int.MaxValue ? -1 : bestAbs));
                builder.AppendLine();
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                File.AppendAllText(Path.Combine(RuntimeControl.ControlDir, "snap-score.txt"), builder.ToString());
            }
            catch
            {
            }
        }

        private static string FormatRect(RECT rect)
        {
            return rect.left + "," + rect.top + "," + rect.right + "," + rect.bottom;
        }
    }
}
