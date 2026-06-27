using System;
using System.IO;

namespace RhaegarMove
{
    internal static class SnapPreview
    {
        private static readonly object Gate = new object();
        private static RECT lastRect;
        private static string lastKind = string.Empty;
        private static DateTime lastTime = DateTime.MinValue;

        public static void Record(string kind, RECT rect, AppSettings settings)
        {
            if (!settings.EnablePreviewState)
                return;

            lock (Gate)
            {
                lastKind = kind;
                lastRect = rect;
                lastTime = DateTime.Now;
            }

            WriteSnapshot(kind, rect);
        }

        public static string DescribeLast()
        {
            lock (Gate)
            {
                if (lastTime == DateTime.MinValue)
                    return "no preview";

                return
                    "kind=" + lastKind + Environment.NewLine +
                    "time=" + lastTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    "left=" + lastRect.left + Environment.NewLine +
                    "top=" + lastRect.top + Environment.NewLine +
                    "right=" + lastRect.right + Environment.NewLine +
                    "bottom=" + lastRect.bottom + Environment.NewLine +
                    "width=" + lastRect.Width + Environment.NewLine +
                    "height=" + lastRect.Height + Environment.NewLine;
            }
        }

        private static void WriteSnapshot(string kind, RECT rect)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhaegarMove");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "preview.txt");
                string text =
                    "kind=" + kind + Environment.NewLine +
                    "time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    "left=" + rect.left + Environment.NewLine +
                    "top=" + rect.top + Environment.NewLine +
                    "right=" + rect.right + Environment.NewLine +
                    "bottom=" + rect.bottom + Environment.NewLine +
                    "width=" + rect.Width + Environment.NewLine +
                    "height=" + rect.Height + Environment.NewLine;
                File.WriteAllText(path, text);
            }
            catch
            {
            }
        }
    }
}
