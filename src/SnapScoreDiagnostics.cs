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

        public SnapScoreDiagnostics(AppSettings settings, string kind, RECT desired, int threshold)
        {
            this.settings = settings;
            if (settings.EnableSnapDiagnostics)
            {
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
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                File.WriteAllText(Path.Combine(RuntimeControl.ControlDir, "snap-score.txt"), builder.ToString());
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
