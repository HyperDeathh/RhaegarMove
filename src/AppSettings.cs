using System;
using System.Collections.Generic;
using System.IO;

namespace RhaegarMove
{
    internal sealed class AppSettings
    {
        public int SnapThreshold = 16;
        public int MinWidth = 120;
        public int MinHeight = 80;
        public int MaxWidth = 0;
        public int MaxHeight = 0;
        public int WatchdogMs = 250;
        public bool EnableEdgeSnap = true;
        public bool EnableAeroSnap = true;
        public int AeroThreshold = 8;
        public bool AeroTopMaximizes = true;
        public int AutoSnap = 2;
        public int SnapGap = 0;
        public int ResizeCenterMode = 1;
        public int CenterFraction = 24;
        public int SidesFraction = 24;
        public int AeroMaxSpeed = 65535;
        public int AeroSpeedTau = 64;
        public bool SnapToWindows = true;
        public bool StickyResize = false;
        public bool RespectWindowMinMaxInfo = true;
        public bool EnableRuleDiagnostics = false;
        public bool EnableSnapDiagnostics = false;
        public bool EnablePreviewState = false;
        public bool EnablePreviewOverlay = false;
        public bool EnablePreviewOnlySnap = false;
        public bool EnableTrayIcon = false;
        public bool AllowFullScreenWindows = false;
        public bool SkipMaximizedWindows = false;
        public bool NotifyMoveSizeEvents = true;

        public readonly List<string> UnknownConfigKeys = new List<string>();
        public readonly List<string> DuplicateConfigKeys = new List<string>();
        public readonly List<string> NormalizationNotes = new List<string>();

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RhaegarMove.ini");
            if (!File.Exists(path)) return s.Normalize();

            Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("[")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    s.UnknownConfigKeys.Add(line);
                    continue;
                }
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                int count;
                seen.TryGetValue(key, out count);
                count++;
                seen[key] = count;
                if (count > 1)
                    s.DuplicateConfigKeys.Add(key + " occurrence " + count + " overrides previous value with '" + value + "'");

                if (!s.Apply(key, value))
                    s.UnknownConfigKeys.Add(key + "=" + value);
            }
            return s.Normalize();
        }

        public void ReloadFromDisk()
        {
            AppSettings fresh = Load();
            CopyFrom(fresh);
        }

        private void CopyFrom(AppSettings s)
        {
            SnapThreshold = s.SnapThreshold;
            MinWidth = s.MinWidth;
            MinHeight = s.MinHeight;
            MaxWidth = s.MaxWidth;
            MaxHeight = s.MaxHeight;
            WatchdogMs = s.WatchdogMs;
            EnableEdgeSnap = s.EnableEdgeSnap;
            EnableAeroSnap = s.EnableAeroSnap;
            AeroThreshold = s.AeroThreshold;
            AeroTopMaximizes = s.AeroTopMaximizes;
            AutoSnap = s.AutoSnap;
            SnapGap = s.SnapGap;
            ResizeCenterMode = s.ResizeCenterMode;
            CenterFraction = s.CenterFraction;
            SidesFraction = s.SidesFraction;
            AeroMaxSpeed = s.AeroMaxSpeed;
            AeroSpeedTau = s.AeroSpeedTau;
            SnapToWindows = s.SnapToWindows;
            StickyResize = s.StickyResize;
            RespectWindowMinMaxInfo = s.RespectWindowMinMaxInfo;
            EnableRuleDiagnostics = s.EnableRuleDiagnostics;
            EnableSnapDiagnostics = s.EnableSnapDiagnostics;
            EnablePreviewState = s.EnablePreviewState;
            EnablePreviewOverlay = s.EnablePreviewOverlay;
            EnablePreviewOnlySnap = s.EnablePreviewOnlySnap;
            EnableTrayIcon = s.EnableTrayIcon;
            AllowFullScreenWindows = s.AllowFullScreenWindows;
            SkipMaximizedWindows = s.SkipMaximizedWindows;
            NotifyMoveSizeEvents = s.NotifyMoveSizeEvents;

            UnknownConfigKeys.Clear();
            UnknownConfigKeys.AddRange(s.UnknownConfigKeys);
            DuplicateConfigKeys.Clear();
            DuplicateConfigKeys.AddRange(s.DuplicateConfigKeys);
            NormalizationNotes.Clear();
            NormalizationNotes.AddRange(s.NormalizationNotes);
        }

        private bool Apply(string key, string value)
        {
            if (key.Equals("SnapThreshold", StringComparison.OrdinalIgnoreCase)) SnapThreshold = ToInt(key, value, SnapThreshold);
            else if (key.Equals("MinWidth", StringComparison.OrdinalIgnoreCase)) MinWidth = ToInt(key, value, MinWidth);
            else if (key.Equals("MinHeight", StringComparison.OrdinalIgnoreCase)) MinHeight = ToInt(key, value, MinHeight);
            else if (key.Equals("MaxWidth", StringComparison.OrdinalIgnoreCase)) MaxWidth = ToInt(key, value, MaxWidth);
            else if (key.Equals("MaxHeight", StringComparison.OrdinalIgnoreCase)) MaxHeight = ToInt(key, value, MaxHeight);
            else if (key.Equals("WatchdogMs", StringComparison.OrdinalIgnoreCase)) WatchdogMs = ToInt(key, value, WatchdogMs);
            else if (key.Equals("EnableEdgeSnap", StringComparison.OrdinalIgnoreCase)) EnableEdgeSnap = ToBool(key, value, EnableEdgeSnap);
            else if (key.Equals("EnableAeroSnap", StringComparison.OrdinalIgnoreCase)) EnableAeroSnap = ToBool(key, value, EnableAeroSnap);
            else if (key.Equals("AeroThreshold", StringComparison.OrdinalIgnoreCase)) AeroThreshold = ToInt(key, value, AeroThreshold);
            else if (key.Equals("AeroTopMaximizes", StringComparison.OrdinalIgnoreCase)) AeroTopMaximizes = ToBool(key, value, AeroTopMaximizes);
            else if (key.Equals("AutoSnap", StringComparison.OrdinalIgnoreCase)) AutoSnap = ToInt(key, value, AutoSnap);
            else if (key.Equals("SnapGap", StringComparison.OrdinalIgnoreCase)) SnapGap = ToInt(key, value, SnapGap);
            else if (key.Equals("ResizeCenterMode", StringComparison.OrdinalIgnoreCase)) ResizeCenterMode = ToInt(key, value, ResizeCenterMode);
            else if (key.Equals("CenterFraction", StringComparison.OrdinalIgnoreCase)) CenterFraction = ToInt(key, value, CenterFraction);
            else if (key.Equals("SidesFraction", StringComparison.OrdinalIgnoreCase)) SidesFraction = ToInt(key, value, SidesFraction);
            else if (key.Equals("AeroMaxSpeed", StringComparison.OrdinalIgnoreCase)) AeroMaxSpeed = ToInt(key, value, AeroMaxSpeed);
            else if (key.Equals("AeroSpeedTau", StringComparison.OrdinalIgnoreCase)) AeroSpeedTau = ToInt(key, value, AeroSpeedTau);
            else if (key.Equals("SnapToWindows", StringComparison.OrdinalIgnoreCase)) SnapToWindows = ToBool(key, value, SnapToWindows);
            else if (key.Equals("StickyResize", StringComparison.OrdinalIgnoreCase)) StickyResize = ToBool(key, value, StickyResize);
            else if (key.Equals("RespectWindowMinMaxInfo", StringComparison.OrdinalIgnoreCase)) RespectWindowMinMaxInfo = ToBool(key, value, RespectWindowMinMaxInfo);
            else if (key.Equals("EnableRuleDiagnostics", StringComparison.OrdinalIgnoreCase)) EnableRuleDiagnostics = ToBool(key, value, EnableRuleDiagnostics);
            else if (key.Equals("EnableSnapDiagnostics", StringComparison.OrdinalIgnoreCase)) EnableSnapDiagnostics = ToBool(key, value, EnableSnapDiagnostics);
            else if (key.Equals("EnablePreviewState", StringComparison.OrdinalIgnoreCase)) EnablePreviewState = ToBool(key, value, EnablePreviewState);
            else if (key.Equals("EnablePreviewOverlay", StringComparison.OrdinalIgnoreCase)) EnablePreviewOverlay = ToBool(key, value, EnablePreviewOverlay);
            else if (key.Equals("EnablePreviewOnlySnap", StringComparison.OrdinalIgnoreCase)) EnablePreviewOnlySnap = ToBool(key, value, EnablePreviewOnlySnap);
            else if (key.Equals("EnableTrayIcon", StringComparison.OrdinalIgnoreCase)) EnableTrayIcon = ToBool(key, value, EnableTrayIcon);
            else if (key.Equals("AllowFullScreenWindows", StringComparison.OrdinalIgnoreCase)) AllowFullScreenWindows = ToBool(key, value, AllowFullScreenWindows);
            else if (key.Equals("SkipMaximizedWindows", StringComparison.OrdinalIgnoreCase)) SkipMaximizedWindows = ToBool(key, value, SkipMaximizedWindows);
            else if (key.Equals("NotifyMoveSizeEvents", StringComparison.OrdinalIgnoreCase)) NotifyMoveSizeEvents = ToBool(key, value, NotifyMoveSizeEvents);
            else return false;
            return true;
        }

        private AppSettings Normalize()
        {
            MinWidth = NormalizeMin("MinWidth", MinWidth, 40);
            MinHeight = NormalizeMin("MinHeight", MinHeight, 40);
            MaxWidth = NormalizeMin("MaxWidth", MaxWidth, 0);
            MaxHeight = NormalizeMin("MaxHeight", MaxHeight, 0);
            if (MaxWidth > 0 && MaxWidth < MinWidth) MaxWidth = NoteClamp("MaxWidth", MaxWidth, MinWidth);
            if (MaxHeight > 0 && MaxHeight < MinHeight) MaxHeight = NoteClamp("MaxHeight", MaxHeight, MinHeight);
            SnapThreshold = NormalizeMin("SnapThreshold", SnapThreshold, 0);
            WatchdogMs = NormalizeMin("WatchdogMs", WatchdogMs, 100);
            AeroThreshold = NormalizeMin("AeroThreshold", AeroThreshold, 1);
            AutoSnap = NormalizeRange("AutoSnap", AutoSnap, 0, 3);
            SnapGap = NormalizeRange("SnapGap", SnapGap, -128, 127);
            ResizeCenterMode = NormalizeRange("ResizeCenterMode", ResizeCenterMode, 0, 3);
            CenterFraction = NormalizeRange("CenterFraction", CenterFraction, 0, 90);
            SidesFraction = NormalizeRange("SidesFraction", SidesFraction, 1, 100);
            AeroMaxSpeed = NormalizeRange("AeroMaxSpeed", AeroMaxSpeed, 0, 65535);
            AeroSpeedTau = NormalizeMin("AeroSpeedTau", AeroSpeedTau, 16);
            return this;
        }

        private int NormalizeMin(string key, int value, int min)
        {
            if (value < min) return NoteClamp(key, value, min);
            return value;
        }

        private int NormalizeRange(string key, int value, int min, int max)
        {
            if (value < min) return NoteClamp(key, value, min);
            if (value > max) return NoteClamp(key, value, max);
            return value;
        }

        private int NoteClamp(string key, int oldValue, int newValue)
        {
            NormalizationNotes.Add(key + ": " + oldValue + " -> " + newValue);
            return newValue;
        }

        private int ToInt(string key, string value, int fallback)
        {
            int parsed;
            if (int.TryParse(value, out parsed)) return parsed;
            NormalizationNotes.Add(key + ": invalid integer '" + value + "', fallback " + fallback);
            return fallback;
        }

        private bool ToBool(string key, string value, bool fallback)
        {
            bool parsed;
            if (bool.TryParse(value, out parsed)) return parsed;
            if (value == "1") return true;
            if (value == "0") return false;
            NormalizationNotes.Add(key + ": invalid boolean '" + value + "', fallback " + fallback);
            return fallback;
        }
    }
}
