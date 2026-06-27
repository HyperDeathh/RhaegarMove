using System;
using System.IO;
using System.Text;

namespace RhaegarMove
{
    internal sealed class SnapDiagnostics
    {
        private readonly AppSettings settings;
        private readonly StringBuilder builder = new StringBuilder();
        private int accepted;
        private int rejected;

        public SnapDiagnostics(AppSettings settings)
        {
            this.settings = settings;
            if (settings.EnableSnapDiagnostics)
            {
                builder.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public void Accept(IntPtr hwnd, string reason, RECT rect)
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            accepted++;
            builder.AppendLine("ACCEPT hwnd=" + hwnd.ToInt64().ToString("X") + " reason=" + reason + " rect=" + FormatRect(rect));
        }

        public void Reject(IntPtr hwnd, string reason)
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            rejected++;
            builder.AppendLine("REJECT hwnd=" + hwnd.ToInt64().ToString("X") + " reason=" + reason);
        }

        public void Flush()
        {
            if (!settings.EnableSnapDiagnostics)
                return;
            try
            {
                builder.AppendLine("accepted=" + accepted);
                builder.AppendLine("rejected=" + rejected);
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                File.WriteAllText(Path.Combine(RuntimeControl.ControlDir, "snap-targets.txt"), builder.ToString());
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
