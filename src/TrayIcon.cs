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
            notifyIcon.Text = "RhaegarMove";
            notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            notifyIcon.Visible = settings.EnableTrayIcon;
            notifyIcon.ContextMenuStrip = BuildMenu();
        }

        private ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem openConfig = new ToolStripMenuItem("Open config");
            openConfig.Click += delegate { OpenConfig(); };
            menu.Items.Add(openConfig);

            ToolStripMenuItem reload = new ToolStripMenuItem("Reload config");
            reload.Click += delegate { reloadAction(); };
            menu.Items.Add(reload);

            ToolStripMenuItem status = new ToolStripMenuItem("Open status folder");
            status.Click += delegate { OpenStatusFolder(); };
            menu.Items.Add(status);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exit = new ToolStripMenuItem("Exit RhaegarMove");
            exit.Click += delegate { exitAction(); };
            menu.Items.Add(exit);

            return menu;
        }

        public void SetVisible(bool visible)
        {
            notifyIcon.Visible = visible;
        }

        private static void OpenConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RhaegarMove.ini");
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
