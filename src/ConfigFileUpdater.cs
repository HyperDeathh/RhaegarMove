using System;
using System.Collections.Generic;
using System.IO;

namespace RhaegarMove
{
    internal static class ConfigFileUpdater
    {
        public static string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RhaegarMove.ini"); }
        }

        public static void SetGeneralValues(Dictionary<string, string> values)
        {
            SetSectionValues("General", values);
        }

        public static void SetBlacklistValues(Dictionary<string, string> values)
        {
            SetSectionValues("Blacklist", values);
        }

        public static Dictionary<string, string> ReadSectionValues(string section)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string path = ConfigPath;
            if (!File.Exists(path)) return result;

            List<string> lines = new List<string>(File.ReadAllLines(path));
            int start = FindSection(lines, section);
            if (start < 0) return result;
            int end = FindNextSection(lines, start + 1);
            if (end < 0) end = lines.Count;

            for (int i = start + 1; i < end; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith(";") || trimmed.StartsWith("#")) continue;
                int eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;
                string key = trimmed.Substring(0, eq).Trim();
                string value = trimmed.Substring(eq + 1).Trim();
                result[key] = value;
            }

            return result;
        }

        private static void SetSectionValues(string section, Dictionary<string, string> values)
        {
            string path = ConfigPath;
            List<string> lines = new List<string>();
            if (File.Exists(path))
                lines.AddRange(File.ReadAllLines(path));

            int sectionStart = FindSection(lines, section);
            if (sectionStart < 0)
            {
                if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length != 0)
                    lines.Add(string.Empty);
                lines.Add("[" + section + "]");
                sectionStart = lines.Count - 1;
            }

            int sectionEnd = FindNextSection(lines, sectionStart + 1);
            if (sectionEnd < 0) sectionEnd = lines.Count;

            HashSet<string> written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = sectionStart + 1; i < sectionEnd; i++)
            {
                string raw = lines[i];
                string trimmed = raw.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;
                int eq = trimmed.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = trimmed.Substring(0, eq).Trim();
                string value;
                if (values.TryGetValue(key, out value))
                {
                    lines[i] = key + "=" + value;
                    written.Add(key);
                }
            }

            List<string> missing = new List<string>();
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (!written.Contains(pair.Key))
                    missing.Add(pair.Key + "=" + pair.Value);
            }

            if (missing.Count > 0)
                lines.InsertRange(sectionEnd, missing);

            File.WriteAllLines(path, lines.ToArray());
        }

        private static int FindSection(List<string> lines, string section)
        {
            string marker = "[" + section + "]";
            for (int i = 0; i < lines.Count; i++)
            {
                if (string.Equals(lines[i].Trim(), marker, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static int FindNextSection(List<string> lines, int start)
        {
            for (int i = start; i < lines.Count; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    return i;
            }
            return -1;
        }
    }
}
