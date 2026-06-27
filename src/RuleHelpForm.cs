using System.Drawing;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal sealed class RuleHelpForm : Form
    {
        public RuleHelpForm()
        {
            Text = "RhaegarMove Rule Help";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(760, 620);
            MinimizeBox = false;
            MaximizeBox = false;

            TextBox text = new TextBox();
            text.Dock = DockStyle.Fill;
            text.Multiline = true;
            text.ReadOnly = true;
            text.ScrollBars = ScrollBars.Vertical;
            text.Font = new Font(FontFamily.GenericMonospace, 9f);
            text.Text = BuildHelpText();
            Controls.Add(text);
        }

        private static string BuildHelpText()
        {
            return
@"RhaegarMove window rule format

Most rule fields are comma-separated wildcard patterns.

Simple lists:
  Classes=Shell_TrayWnd,Progman
  Processes=StartMenuExperienceHost.exe,dwm.exe
  Titles=Program Manager,Volume Control

Composite rule format:
  process:title|class

Examples:
  app.exe:*|*
  app.exe:*Settings*|*
  *:*Dialog*|#32770
  ApplicationFrameHost.exe:*|Windows.UI.Core.CoreWindow

Wildcard:
  * matches any text.

Important fields:

Rules
  Windows matched here are ignored completely.
  Use this for taskbar, shell surfaces, popups, or apps that should never move/resize.

SnapList
  If empty, every eligible top-level window can be a snap target.
  If not empty, only matched windows become snap targets.
  Avoid SnapList=* unless you know why.

NoSizingNotify
  Blocks WM_ENTERSIZEMOVE / WM_EXITSIZEMOVE style notifications for matched apps.
  Useful if an app reacts badly to sizing notifications.

NoResize
  Blocks Alt+right resize for matched apps while still allowing move when otherwise eligible.

NoMinMaxInfo
  Disables native WM_GETMINMAXINFO min/max constraints for matched apps.
  Use this when a specific app reports broken min/max track sizes.
  Prefer per-app rules instead of NoMinMaxInfo=*.

Recommended troubleshooting flow:

1. Put cursor over the window.
2. Run diagnose_cursor.bat.
3. Read rules.txt for process/class/title and minMaxInfo details.
4. Add the narrowest possible rule.
5. Run reload.bat.

Safe examples:

  NoResize=game.exe:*|*
  NoMinMaxInfo=weirdapp.exe:*|*
  NoSizingNotify=legacyapp.exe:*|*
  SnapList=notepad.exe:*|*,chrome.exe:*|Chrome_WidgetWin_1
";
        }
    }
}
