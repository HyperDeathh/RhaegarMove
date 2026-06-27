using System.Collections.Generic;

namespace RhaegarMove
{
    internal static class ConfigDefaults
    {
        public static Dictionary<string, string> General()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            values["SnapThreshold"] = "16";
            values["MinWidth"] = "120";
            values["MinHeight"] = "80";
            values["MaxWidth"] = "0";
            values["MaxHeight"] = "0";
            values["WatchdogMs"] = "250";
            values["EnableEdgeSnap"] = "true";
            values["EnableAeroSnap"] = "true";
            values["AeroThreshold"] = "8";
            values["AeroMaxSpeed"] = "65535";
            values["AeroSpeedTau"] = "64";
            values["AeroTopMaximizes"] = "true";
            values["AutoSnap"] = "2";
            values["SnapToWindows"] = "true";
            values["SnapGap"] = "0";
            values["StickyResize"] = "false";
            values["EnableRuleDiagnostics"] = "false";
            values["EnableSnapDiagnostics"] = "false";
            values["EnablePreviewState"] = "false";
            values["EnablePreviewOverlay"] = "false";
            values["EnablePreviewOnlySnap"] = "false";
            values["EnableTrayIcon"] = "false";
            values["ResizeCenterMode"] = "1";
            values["CenterFraction"] = "24";
            values["SidesFraction"] = "24";
            values["AllowFullScreenWindows"] = "false";
            values["SkipMaximizedWindows"] = "false";
            values["NotifyMoveSizeEvents"] = "true";
            return values;
        }

        public static Dictionary<string, string> Blacklist()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            values["Classes"] = "Progman,WorkerW,Shell_TrayWnd,Shell_SecondaryTrayWnd,Button,#32768,TaskSwitcherWnd,TaskSwitcherOverlayWnd,MultitaskingViewFrame,NotifyIconOverflowWindow,XamlExplorerHostIslandWindow,Windows.UI.Core.CoreWindow,NativeHWNDHost,Xaml_WindowedPopupClass,TaskListThumbnailWnd";
            values["Processes"] = "StartMenuExperienceHost.exe,SearchApp.exe,TextInputHost.exe,ShellExperienceHost.exe,dwm.exe,mstsc.exe,msrdc.exe,osk.exe";
            values["Titles"] = "Program Manager,Volume Control";
            values["Rules"] = "ApplicationFrameHost.exe:*|Windows.UI.Core.CoreWindow,*:*|Xaml_WindowedPopupClass";
            values["SnapList"] = "";
            values["NoSizingNotify"] = "";
            values["NoResize"] = "";
            return values;
        }
    }
}
