using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RhaegarMove
{
    internal static class WindowRules
    {
        private static readonly object Gate = new object();
        private static bool loaded;
        private static List<string> classPatterns;
        private static List<string> processPatterns;
        private static List<string> titlePatterns;
        private static List<WindowRule> compositeRules;
        private static List<WindowRule> snapListRules;
        private static List<WindowRule> noSizingNotifyRules;
        private static List<WindowRule> noResizeRules;

        public static bool ShouldIgnoreWindow(IntPtr hwnd, string className)
        {
            EnsureLoaded();
            if (hwnd == IntPtr.Zero)
                return true;

            string title = GetTitle(hwnd);
            string processName = GetProcessName(hwnd);

            if (MatchesAny(classPatterns, className)) return true;
            if (MatchesAny(titlePatterns, title)) return true;
            if (MatchesAny(processPatterns, processName)) return true;
            if (MatchesAny(compositeRules, processName, title, className)) return true;
            return false;
        }

        public static bool ShouldSnapToWindow(IntPtr hwnd, string className)
        {
            EnsureLoaded();
            if (snapListRules.Count == 0)
                return true;
            string title = GetTitle(hwnd);
            string processName = GetProcessName(hwnd);
            return MatchesAny(snapListRules, processName, title, className);
        }

        public static bool ShouldSendSizingNotifications(IntPtr hwnd, string className)
        {
            EnsureLoaded();
            if (noSizingNotifyRules.Count == 0)
                return true;
            string title = GetTitle(hwnd);
            string processName = GetProcessName(hwnd);
            return !MatchesAny(noSizingNotifyRules, processName, title, className);
        }

        public static bool ShouldAllowResize(IntPtr hwnd, string className)
        {
            EnsureLoaded();
            if (noResizeRules.Count == 0)
                return true;
            string title = GetTitle(hwnd);
            string processName = GetProcessName(hwnd);
            return !MatchesAny(noResizeRules, processName, title, className);
        }

        private static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (loaded)
                    return;

                classPatterns = new List<string> { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Button", "#32768", "TaskSwitcherWnd", "TaskSwitcherOverlayWnd", "MultitaskingViewFrame", "NotifyIconOverflowWindow" };
                processPatterns = new List<string> { "StartMenuExperienceHost.exe", "SearchApp.exe", "TextInputHost.exe", "ShellExperienceHost.exe", "dwm.exe" };
                titlePatterns = new List<string> { "Program Manager" };
                compositeRules = new List<WindowRule>();
                snapListRules = new List<WindowRule>();
                noSizingNotifyRules = new List<WindowRule>();
                noResizeRules = new List<WindowRule>();

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RhaegarMove.ini");
                if (File.Exists(path))
                    LoadIniRules(path);

                loaded = true;
            }
        }

        private static void LoadIniRules(string path)
        {
            string section = string.Empty;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }
                if (!section.Equals("Blacklist", StringComparison.OrdinalIgnoreCase)) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                if (key.Equals("Classes", StringComparison.OrdinalIgnoreCase)) AddCsv(classPatterns, value);
                else if (key.Equals("Processes", StringComparison.OrdinalIgnoreCase)) AddCsv(processPatterns, value);
                else if (key.Equals("Titles", StringComparison.OrdinalIgnoreCase)) AddCsv(titlePatterns, value);
                else if (key.Equals("Rules", StringComparison.OrdinalIgnoreCase)) AddRuleCsv(compositeRules, value);
                else if (key.Equals("SnapList", StringComparison.OrdinalIgnoreCase)) AddRuleCsv(snapListRules, value);
                else if (key.Equals("NoSizingNotify", StringComparison.OrdinalIgnoreCase)) AddRuleCsv(noSizingNotifyRules, value);
                else if (key.Equals("NoResize", StringComparison.OrdinalIgnoreCase)) AddRuleCsv(noResizeRules, value);
            }
        }

        private static void AddCsv(List<string> list, string csv)
        {
            string[] parts = csv.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string item = parts[i].Trim();
                if (item.Length > 0) list.Add(item);
            }
        }

        private static void AddRuleCsv(List<WindowRule> list, string csv)
        {
            string[] parts = csv.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                WindowRule rule = WindowRule.Parse(parts[i].Trim());
                if (rule != null) list.Add(rule);
            }
        }

        private static bool MatchesAny(List<string> patterns, string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < patterns.Count; i++)
            {
                if (WildcardMatch(value, patterns[i])) return true;
            }
            return false;
        }

        private static bool MatchesAny(List<WindowRule> rules, string process, string title, string cls)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].Matches(process, title, cls)) return true;
            }
            return false;
        }

        private static bool WildcardMatch(string value, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;
            if (pattern == "*") return true;
            if (value == null) value = string.Empty;

            string[] parts = pattern.Split('*');
            int index = 0;
            bool anchoredStart = !pattern.StartsWith("*");
            bool anchoredEnd = !pattern.EndsWith("*");

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0) continue;
                int found = value.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
                if (found < 0) return false;
                if (i == 0 && anchoredStart && found != 0) return false;
                index = found + part.Length;
            }

            if (anchoredEnd && parts.Length > 0)
            {
                string last = parts[parts.Length - 1];
                if (last.Length > 0 && !value.EndsWith(last, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        private static string GetTitle(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(512);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetProcessName(IntPtr hwnd)
        {
            try
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid == 0) return string.Empty;
                using (Process p = Process.GetProcessById((int)pid))
                    return p.ProcessName + ".exe";
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class WindowRule
        {
            private readonly string process;
            private readonly string title;
            private readonly string cls;

            private WindowRule(string process, string title, string cls)
            {
                this.process = string.IsNullOrEmpty(process) ? "*" : process;
                this.title = string.IsNullOrEmpty(title) ? "*" : title;
                this.cls = string.IsNullOrEmpty(cls) ? "*" : cls;
            }

            public static WindowRule Parse(string raw)
            {
                if (string.IsNullOrEmpty(raw)) return null;
                string process = "*";
                string titleClass = raw;
                int colon = raw.IndexOf(':');
                if (colon >= 0)
                {
                    process = raw.Substring(0, colon).Trim();
                    titleClass = raw.Substring(colon + 1).Trim();
                }
                string title = titleClass;
                string cls = "*";
                int bar = titleClass.IndexOf('|');
                if (bar >= 0)
                {
                    title = titleClass.Substring(0, bar).Trim();
                    cls = titleClass.Substring(bar + 1).Trim();
                }
                return new WindowRule(process, title, cls);
            }

            public bool Matches(string processName, string titleName, string className)
            {
                return WildcardMatch(processName, process) && WildcardMatch(titleName, title) && WildcardMatch(className, cls);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    }
}
