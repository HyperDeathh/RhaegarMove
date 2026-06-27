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
            string path = ConfigPath;
            List<string> lines = new List<string>();
            if (File.Exists(path))
                lines.AddRange(File.ReadAllLines(path));

            int generalStart = FindSection(lines, "General");
            if (generalStart < 0)
            {
                lines.Insert(0, "[General]");
                generalStart = 0;
            }

            int generalEnd = FindNextSection(lines, generalStart + 1);
            if (generalEnd < 0) generalEnd = lines.Count;

            HashSet<string> written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = generalStart + 1; i < generalEnd; i++)
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
                lines.InsertRange(generalEnd, missing);

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
