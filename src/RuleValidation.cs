using System;
using System.Collections.Generic;
using System.Text;

namespace RhaegarMove
{
    internal static class RuleValidation
    {
        public static string BuildReport()
        {
            return BuildReport(ConfigFileUpdater.ReadSectionValues("Blacklist"));
        }

        public static string BuildReport(Dictionary<string, string> values)
        {
            StringBuilder b = new StringBuilder();
            bool any = false;

            any |= ValidateCsv(b, values, "Classes", false);
            any |= ValidateCsv(b, values, "Processes", false);
            any |= ValidateCsv(b, values, "Titles", false);
            any |= ValidateCsv(b, values, "Rules", true);
            any |= ValidateCsv(b, values, "SnapList", true);
            any |= ValidateCsv(b, values, "NoSizingNotify", true);
            any |= ValidateCsv(b, values, "NoResize", true);

            if (!any)
                b.AppendLine("- none");
            return b.ToString();
        }

        public static bool HasWarnings(Dictionary<string, string> values)
        {
            string report = BuildReport(values).Trim();
            return report.Length > 0 && report != "- none";
        }

        private static bool ValidateCsv(StringBuilder b, Dictionary<string, string> values, string key, bool composite)
        {
            string value;
            if (!values.TryGetValue(key, out value))
            {
                b.AppendLine("- missing key: " + key);
                return true;
            }

            bool any = false;
            string[] parts = value.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string item = parts[i].Trim();
                if (item.Length == 0)
                    continue;

                if (item.IndexOf('\t') >= 0)
                {
                    b.AppendLine("- " + key + " contains a tab: " + item);
                    any = true;
                }
                if (item.IndexOf("  ", StringComparison.Ordinal) >= 0)
                {
                    b.AppendLine("- " + key + " contains repeated spaces: " + item);
                    any = true;
                }
                if (composite)
                    any |= ValidateComposite(b, key, item);
            }
            return any;
        }

        private static bool ValidateComposite(StringBuilder b, string key, string item)
        {
            bool any = false;
            int colonCount = Count(item, ':');
            int barCount = Count(item, '|');

            if (colonCount > 1)
            {
                b.AppendLine("- " + key + " has more than one ':' separator: " + item);
                any = true;
            }
            if (barCount > 1)
            {
                b.AppendLine("- " + key + " has more than one '|' separator: " + item);
                any = true;
            }
            if (item.StartsWith(":") || item.EndsWith(":") || item.StartsWith("|") || item.EndsWith("|"))
            {
                b.AppendLine("- " + key + " has an empty composite segment: " + item);
                any = true;
            }
            if (key.Equals("SnapList", StringComparison.OrdinalIgnoreCase) && item == "*")
            {
                b.AppendLine("- SnapList=* makes every eligible top-level window a snap target; empty SnapList is usually safer.");
                any = true;
            }
            return any;
        }

        private static int Count(string text, char c)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == c) count++;
            }
            return count;
        }
    }
}
