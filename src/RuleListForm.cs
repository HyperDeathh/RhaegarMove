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
        private TextBox noMinMaxInfo;
        private Label validationLabel;

        public RuleListForm(Action afterSave)
        {
            this.afterSave = afterSave;
            Text = "RhaegarMove Window Rules";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(720, 800);
            MinimizeBox = false;
            MaximizeBox = false;
            BuildUi();
            LoadValues();
            UpdateValidation();
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 14;
            root.Padding = new Padding(12);
            root.AutoScroll = true;
            Controls.Add(root);

            classes = AddBox(root, "Classes");
            processes = AddBox(root, "Processes");
            titles = AddBox(root, "Titles");
            rules = AddBox(root, "Rules  example: process.exe:*Title*|WindowClass");
            snapList = AddBox(root, "SnapList  allow-list, empty means all eligible windows");
            noSizingNotify = AddBox(root, "NoSizingNotify  example: app.exe:*|*");
            noResize = AddBox(root, "NoResize  example: app.exe:*|*");
            noMinMaxInfo = AddBox(root, "NoMinMaxInfo  example: app.exe:*|*");

            Label note = new Label();
            note.Text = "Comma-separated wildcard patterns. Keep shell/taskbar/start menu rules unless you know why you are changing them.";
            note.AutoSize = true;
            note.Dock = DockStyle.Fill;
            root.Controls.Add(note);

            Button help = new Button();
            help.Text = "Rule examples and troubleshooting help...";
            help.Height = 32;
            help.Dock = DockStyle.Fill;
            help.Click += delegate { new RuleHelpForm().ShowDialog(this); };
            root.Controls.Add(help);

            validationLabel = new Label();
            validationLabel.AutoSize = true;
            validationLabel.Dock = DockStyle.Fill;
            validationLabel.MaximumSize = new Size(660, 0);
            root.Controls.Add(validationLabel);

            Button reset = new Button();
            reset.Text = "Reset default window rules";
            reset.Height = 32;
            reset.Dock = DockStyle.Fill;
            reset.Click += delegate { ResetDefaults(); };
            root.Controls.Add(reset);

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
            t.TextChanged += delegate { UpdateValidation(); };
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
            noMinMaxInfo.Text = Get(values, "NoMinMaxInfo");
        }

        private static string Get(Dictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : string.Empty;
        }

        private void UpdateValidation()
        {
            if (validationLabel == null || classes == null)
                return;
            string report = RuleValidation.BuildReport(CurrentValues()).Trim();
            validationLabel.Text = report == "- none" ? "Rule validation: no warnings" : "Rule validation warnings:\r\n" + report;
        }

        private void SaveAndReload()
        {
            Dictionary<string, string> values = CurrentValues();
            if (RuleValidation.HasWarnings(values))
            {
                DialogResult result = MessageBox.Show(this, "Rule validation has warnings. Save anyway?", "RhaegarMove", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    return;
            }
            ConfigFileUpdater.SetBlacklistValues(values);
            if (afterSave != null) afterSave();
            Close();
        }

        private void ResetDefaults()
        {
            DialogResult result = MessageBox.Show(this, "Reset window rules to safe defaults?", "RhaegarMove", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;
            ConfigFileUpdater.SetBlacklistValues(ConfigDefaults.Blacklist());
            LoadValues();
            if (afterSave != null) afterSave();
        }

        private Dictionary<string, string> CurrentValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            values["Classes"] = Normalize(classes.Text);
            values["Processes"] = Normalize(processes.Text);
            values["Titles"] = Normalize(titles.Text);
            values["Rules"] = Normalize(rules.Text);
            values["SnapList"] = Normalize(snapList.Text);
            values["NoSizingNotify"] = Normalize(noSizingNotify.Text);
            values["NoResize"] = Normalize(noResize.Text);
            values["NoMinMaxInfo"] = Normalize(noMinMaxInfo.Text);
            return values;
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
