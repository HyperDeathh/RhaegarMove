using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon notifyIcon;
        private readonly Action reloadAction;
        private readonly Action exitAction;

        public TrayIcon(AppSettings settings, Action reloadAction, Action exitAction)
        {
            this.reloadAction = reloadAction;
            this.exitAction = exitAction;

            notifyIcon = new NotifyIcon();
            notifyIcon.Text = BuildTooltip(settings);
            notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            notifyIcon.Visible = settings.EnableTrayIcon;
            notifyIcon.ContextMenuStrip = BuildMenu();
        }

        private ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += delegate { new SettingsForm(reloadAction).Show(); };
            menu.Items.Add(settingsItem);

            ToolStripMenuItem openConfig = new ToolStripMenuItem("Open config");
            openConfig.Click += delegate { OpenConfig(); };
            menu.Items.Add(openConfig);

            ToolStripMenuItem reload = new ToolStripMenuItem("Reload config");
            reload.Click += delegate { reloadAction(); };
            menu.Items.Add(reload);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem reports = new ToolStripMenuItem("Reports");
            reports.DropDownItems.Add(MakeOpenFileItem("Config report", "config-report.txt"));
            reports.DropDownItems.Add(MakeOpenFileItem("Rule diagnostics", "rules.txt"));
            reports.DropDownItems.Add(MakeOpenFileItem("Snap targets", "snap-targets.txt"));
            reports.DropDownItems.Add(MakeOpenFileItem("Snap score", "snap-score.txt"));
            reports.DropDownItems.Add(MakeOpenFolderItem("Open status folder"));
            menu.Items.Add(reports);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exit = new ToolStripMenuItem("Exit RhaegarMove");
            exit.Click += delegate { exitAction(); };
            menu.Items.Add(exit);

            return menu;
        }

        public void RefreshSettings(AppSettings settings)
        {
            notifyIcon.Text = BuildTooltip(settings);
            notifyIcon.Visible = settings.EnableTrayIcon;
        }

        public void SetVisible(bool visible)
        {
            notifyIcon.Visible = visible;
        }

        private static ToolStripMenuItem MakeOpenFileItem(string text, string fileName)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += delegate { OpenReport(fileName); };
            return item;
        }

        private static ToolStripMenuItem MakeOpenFolderItem(string text)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += delegate { OpenStatusFolder(); };
            return item;
        }

        private static string BuildTooltip(AppSettings settings)
        {
            string mode = settings.EnablePreviewOnlySnap ? "preview-only" : "live";
            string snap = settings.EnableEdgeSnap ? "snap on" : "snap off";
            return "RhaegarMove - " + mode + ", " + snap;
        }

        private static void OpenConfig()
        {
            string path = ConfigFileUpdater.ConfigPath;
            try
            {
                if (!File.Exists(path))
                    File.WriteAllText(path, string.Empty);
                Process.Start("notepad.exe", path);
            }
            catch
            {
            }
        }

        private static void OpenReport(string fileName)
        {
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                string path = Path.Combine(RuntimeControl.ControlDir, fileName);
                if (!File.Exists(path))
                    File.WriteAllText(path, "Report has not been generated yet." + Environment.NewLine);
                Process.Start("notepad.exe", path);
            }
            catch
            {
            }
        }

        private static void OpenStatusFolder()
        {
            try
            {
                Directory.CreateDirectory(RuntimeControl.ControlDir);
                Process.Start("explorer.exe", RuntimeControl.ControlDir);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
}
