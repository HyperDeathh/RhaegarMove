using System;
using System.IO;

namespace RhaegarMove
{
    internal static class RuntimeControl
    {
        public static string ControlDir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhaegarMove");
            }
        }

        public static string ReloadRequestPath
        {
            get { return Path.Combine(ControlDir, "reload.request"); }
        }

        public static string ExitRequestPath
        {
            get { return Path.Combine(ControlDir, "exit.request"); }
        }

        public static string RuntimePath
        {
            get { return Path.Combine(ControlDir, "runtime.txt"); }
        }

        public static void RequestReload()
        {
            Directory.CreateDirectory(ControlDir);
            File.WriteAllText(ReloadRequestPath, DateTime.Now.ToString("O"));
            WriteRuntime("reload request written");
        }

        public static void RequestExit()
        {
            Directory.CreateDirectory(ControlDir);
            File.WriteAllText(ExitRequestPath, DateTime.Now.ToString("O"));
            WriteRuntime("exit request written");
        }

        public static bool ConsumeReloadRequest()
        {
            return Consume(ReloadRequestPath);
        }

        public static bool ConsumeExitRequest()
        {
            return Consume(ExitRequestPath);
        }

        public static void WriteRuntime(string text)
        {
            try
            {
                Directory.CreateDirectory(ControlDir);
                File.WriteAllText(RuntimePath, text ?? string.Empty);
            }
            catch
            {
            }
        }

        private static bool Consume(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;
                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
