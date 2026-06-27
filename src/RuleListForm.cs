using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal sealed class RuleListForm : Form
    {
        private readonly Action afterSave;
        private TextBox classes;
        private TextBox processes;
        private TextBox titles;
        private TextBox rules;
        private TextBox snapList;
        private TextBox noSizingNotify;
        private TextBox noResize;

        public RuleListForm(Action afterSave)
        {
            this.afterSave = afterSave;
            Text = "RhaegarMove Window Rules";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(720, 620);
            MinimizeBox = false;
            MaximizeBox = false;
            BuildUi();
            LoadValues();
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 10;
            root.Padding = new Padding(12);
            root.AutoScroll = true;
            Controls.Add(root);

            classes = AddBox(root, "Classes");
            processes = AddBox(root, "Processes");
            titles = AddBox(root, "Titles");
            rules = AddBox(root, "Rules  process:title|class");
            snapList = AddBox(root, "SnapList  allow-list, empty means all eligible windows");
            noSizingNotify = AddBox(root, "NoSizingNotify");
            noResize = AddBox(root, "NoResize");

            Label note = new Label();
            note.Text = "Comma-separated wildcard patterns. Keep shell/taskbar/start menu rules unless you know why you are changing them.";
            note.AutoSize = true;
            note.Dock = DockStyle.Fill;
            root.Controls.Add(note);

            Button save = new Button();
            save.Text = "Save rules and reload";
            save.Height = 32;
            save.Dock = DockStyle.Fill;
            save.Click += delegate { SaveAndReload(); };
            root.Controls.Add(save);
        }

        private TextBox AddBox(TableLayoutPanel root, string label)
        {
            Label l = new Label();
            l.Text = label;
            l.AutoSize = true;
            l.Dock = DockStyle.Fill;
            root.Controls.Add(l);

            TextBox t = new TextBox();
            t.Multiline = true;
            t.Height = 48;
            t.ScrollBars = ScrollBars.Vertical;
            t.Dock = DockStyle.Fill;
            root.Controls.Add(t);
            return t;
        }

        private void LoadValues()
        {
            Dictionary<string, string> values = ConfigFileUpdater.ReadSectionValues("Blacklist");
            classes.Text = Get(values, "Classes");
            processes.Text = Get(values, "Processes");
            titles.Text = Get(values, "Titles");
            rules.Text = Get(values, "Rules");
            snapList.Text = Get(values, "SnapList");
            noSizingNotify.Text = Get(values, "NoSizingNotify");
            noResize.Text = Get(values, "NoResize");
        }

        private static string Get(Dictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : string.Empty;
        }

        private void SaveAndReload()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            values["Classes"] = Normalize(classes.Text);
            values["Processes"] = Normalize(processes.Text);
            values["Titles"] = Normalize(titles.Text);
            values["Rules"] = Normalize(rules.Text);
            values["SnapList"] = Normalize(snapList.Text);
            values["NoSizingNotify"] = Normalize(noSizingNotify.Text);
            values["NoResize"] = Normalize(noResize.Text);
            ConfigFileUpdater.SetBlacklistValues(values);
            if (afterSave != null) afterSave();
            Close();
        }

        private static string Normalize(string text)
        {
            if (text == null) return string.Empty;
            string[] lines = text.Replace("\r", "").Split('\n');
            List<string> parts = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string[] split = lines[i].Split(',');
                for (int j = 0; j < split.Length; j++)
                {
                    string item = split[j].Trim();
                    if (item.Length > 0) parts.Add(item);
                }
            }
            return string.Join(",", parts.ToArray());
        }
    }
}
