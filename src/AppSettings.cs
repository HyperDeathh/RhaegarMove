using System;
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
        public int AutoSnap = 2;
        public int SnapGap = 0;
        public int ResizeCenterMode = 1;
        public int CenterFraction = 24;
        public int SidesFraction = 24;
        public int AeroMaxSpeed = 65535;
        public int AeroSpeedTau = 64;
        public bool SnapToWindows = true;
        public bool StickyResize = false;
        public bool EnableRuleDiagnostics = false;
        public bool EnablePreviewState = false;
        public bool AllowFullScreenWindows = false;
        public bool SkipMaximizedWindows = false;
        public bool NotifyMoveSizeEvents = true;

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RhaegarMove.ini");
            if (!File.Exists(path)) return s.Normalize();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("[")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                s.Apply(key, value);
            }
            return s.Normalize();
        }

        private void Apply(string key, string value)
        {
            if (key.Equals("SnapThreshold", StringComparison.OrdinalIgnoreCase)) SnapThreshold = ToInt(value, SnapThreshold);
            else if (key.Equals("MinWidth", StringComparison.OrdinalIgnoreCase)) MinWidth = ToInt(value, MinWidth);
            else if (key.Equals("MinHeight", StringComparison.OrdinalIgnoreCase)) MinHeight = ToInt(value, MinHeight);
            else if (key.Equals("MaxWidth", StringComparison.OrdinalIgnoreCase)) MaxWidth = ToInt(value, MaxWidth);
            else if (key.Equals("MaxHeight", StringComparison.OrdinalIgnoreCase)) MaxHeight = ToInt(value, MaxHeight);
            else if (key.Equals("WatchdogMs", StringComparison.OrdinalIgnoreCase)) WatchdogMs = ToInt(value, WatchdogMs);
            else if (key.Equals("EnableEdgeSnap", StringComparison.OrdinalIgnoreCase)) EnableEdgeSnap = ToBool(value, EnableEdgeSnap);
            else if (key.Equals("EnableAeroSnap", StringComparison.OrdinalIgnoreCase)) EnableAeroSnap = ToBool(value, EnableAeroSnap);
            else if (key.Equals("AeroThreshold", StringComparison.OrdinalIgnoreCase)) AeroThreshold = ToInt(value, AeroThreshold);
            else if (key.Equals("AutoSnap", StringComparison.OrdinalIgnoreCase)) AutoSnap = ToInt(value, AutoSnap);
            else if (key.Equals("SnapGap", StringComparison.OrdinalIgnoreCase)) SnapGap = ToInt(value, SnapGap);
            else if (key.Equals("ResizeCenterMode", StringComparison.OrdinalIgnoreCase)) ResizeCenterMode = ToInt(value, ResizeCenterMode);
            else if (key.Equals("CenterFraction", StringComparison.OrdinalIgnoreCase)) CenterFraction = ToInt(value, CenterFraction);
            else if (key.Equals("SidesFraction", StringComparison.OrdinalIgnoreCase)) SidesFraction = ToInt(value, SidesFraction);
            else if (key.Equals("AeroMaxSpeed", StringComparison.OrdinalIgnoreCase)) AeroMaxSpeed = ToInt(value, AeroMaxSpeed);
            else if (key.Equals("AeroSpeedTau", StringComparison.OrdinalIgnoreCase)) AeroSpeedTau = ToInt(value, AeroSpeedTau);
            else if (key.Equals("SnapToWindows", StringComparison.OrdinalIgnoreCase)) SnapToWindows = ToBool(value, SnapToWindows);
            else if (key.Equals("StickyResize", StringComparison.OrdinalIgnoreCase)) StickyResize = ToBool(value, StickyResize);
            else if (key.Equals("EnableRuleDiagnostics", StringComparison.OrdinalIgnoreCase)) EnableRuleDiagnostics = ToBool(value, EnableRuleDiagnostics);
            else if (key.Equals("EnablePreviewState", StringComparison.OrdinalIgnoreCase)) EnablePreviewState = ToBool(value, EnablePreviewState);
            else if (key.Equals("AllowFullScreenWindows", StringComparison.OrdinalIgnoreCase)) AllowFullScreenWindows = ToBool(value, AllowFullScreenWindows);
            else if (key.Equals("SkipMaximizedWindows", StringComparison.OrdinalIgnoreCase)) SkipMaximizedWindows = ToBool(value, SkipMaximizedWindows);
            else if (key.Equals("NotifyMoveSizeEvents", StringComparison.OrdinalIgnoreCase)) NotifyMoveSizeEvents = ToBool(value, NotifyMoveSizeEvents);
        }

        private AppSettings Normalize()
        {
            MinWidth = Math.Max(40, MinWidth);
            MinHeight = Math.Max(40, MinHeight);
            MaxWidth = Math.Max(0, MaxWidth);
            MaxHeight = Math.Max(0, MaxHeight);
            if (MaxWidth > 0) MaxWidth = Math.Max(MinWidth, MaxWidth);
            if (MaxHeight > 0) MaxHeight = Math.Max(MinHeight, MaxHeight);
            SnapThreshold = Math.Max(0, SnapThreshold);
            WatchdogMs = Math.Max(100, WatchdogMs);
            AeroThreshold = Math.Max(1, AeroThreshold);
            AutoSnap = Clamp(AutoSnap, 0, 3);
            SnapGap = Clamp(SnapGap, -128, 127);
            ResizeCenterMode = Clamp(ResizeCenterMode, 0, 3);
            CenterFraction = Clamp(CenterFraction, 0, 90);
            SidesFraction = Clamp(SidesFraction, 1, 100);
            AeroMaxSpeed = Clamp(AeroMaxSpeed, 0, 65535);
            AeroSpeedTau = Math.Max(16, AeroSpeedTau);
            return this;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static int ToInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static bool ToBool(string value, bool fallback)
        {
            bool parsed;
            if (bool.TryParse(value, out parsed)) return parsed;
            if (value == "1") return true;
            if (value == "0") return false;
            return fallback;
        }
    }
}
