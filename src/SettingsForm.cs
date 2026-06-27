using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal sealed class SettingsForm : Form
    {
        private readonly Action afterSave;
        private NumericUpDown snapThreshold;
        private NumericUpDown minWidth;
        private NumericUpDown minHeight;
        private NumericUpDown aeroThreshold;
        private NumericUpDown autoSnap;
        private NumericUpDown snapGap;
        private NumericUpDown watchdogMs;
        private CheckBox enableEdgeSnap;
        private CheckBox enableAeroSnap;
        private CheckBox snapToWindows;
        private CheckBox enablePreviewOverlay;
        private CheckBox enablePreviewOnlySnap;
        private CheckBox enableRuleDiagnostics;
        private CheckBox enableSnapDiagnostics;
        private CheckBox enableTrayIcon;
        private CheckBox stickyResize;
        private Label warningLabel;

        public SettingsForm(Action afterSave)
        {
            this.afterSave = afterSave;
            Text = "RhaegarMove Settings";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(500, 760);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BuildUi();
            LoadValues(AppSettings.Load());
            UpdateWarnings();
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 26;
            root.Padding = new Padding(12);
            root.AutoScroll = true;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            Controls.Add(root);

            snapThreshold = AddNumber(root, "SnapThreshold", 0, 128);
            minWidth = AddNumber(root, "MinWidth", 40, 2000);
            minHeight = AddNumber(root, "MinHeight", 40, 2000);
            aeroThreshold = AddNumber(root, "AeroThreshold", 1, 128);
            autoSnap = AddNumber(root, "AutoSnap", 0, 3);
            snapGap = AddNumber(root, "SnapGap", -128, 127);
            watchdogMs = AddNumber(root, "WatchdogMs", 100, 5000);

            enableEdgeSnap = AddCheck(root, "EnableEdgeSnap");
            enableAeroSnap = AddCheck(root, "EnableAeroSnap");
            snapToWindows = AddCheck(root, "SnapToWindows");
            enablePreviewOverlay = AddCheck(root, "EnablePreviewOverlay");
            enablePreviewOnlySnap = AddCheck(root, "EnablePreviewOnlySnap");
            enableRuleDiagnostics = AddCheck(root, "EnableRuleDiagnostics");
            enableSnapDiagnostics = AddCheck(root, "EnableSnapDiagnostics");
            enableTrayIcon = AddCheck(root, "EnableTrayIcon optional");
            stickyResize = AddCheck(root, "StickyResize");

            Button rules = new Button();
            rules.Text = "Edit window rules...";
            rules.Dock = DockStyle.Fill;
            rules.Height = 32;
            rules.Click += delegate { new RuleListForm(afterSave).ShowDialog(this); };
            root.Controls.Add(rules, 0, root.RowCount - 7);
            root.SetColumnSpan(rules, 2);

            Button resetGeneral = new Button();
            resetGeneral.Text = "Reset general defaults";
            resetGeneral.Dock = DockStyle.Fill;
            resetGeneral.Height = 32;
            resetGeneral.Click += delegate { ResetGeneralDefaults(); };
            root.Controls.Add(resetGeneral, 0, root.RowCount - 6);

            Button resetRules = new Button();
            resetRules.Text = "Reset window rules";
            resetRules.Dock = DockStyle.Fill;
            resetRules.Height = 32;
            resetRules.Click += delegate { ResetRuleDefaults(); };
            root.Controls.Add(resetRules, 1, root.RowCount - 6);

            warningLabel = new Label();
            warningLabel.AutoSize = true;
            warningLabel.Dock = DockStyle.Fill;
            warningLabel.MaximumSize = new Size(440, 0);
            root.Controls.Add(warningLabel, 0, root.RowCount - 5);
            root.SetColumnSpan(warningLabel, 2);

            Button save = new Button();
            save.Text = "Save and reload";
            save.Dock = DockStyle.Fill;
            save.Height = 32;
            save.Click += delegate { SaveAndReload(); };
            root.Controls.Add(save, 0, root.RowCount - 4);
            root.SetColumnSpan(save, 2);

            Label note = new Label();
            note.Text = "Tray icon is optional and disabled by default. Keep stop.bat as emergency fallback.";
            note.AutoSize = true;
            note.Dock = DockStyle.Fill;
            root.Controls.Add(note, 0, root.RowCount - 3);
            root.SetColumnSpan(note, 2);
        }

        private NumericUpDown AddNumber(TableLayoutPanel root, string label, int min, int max)
        {
            Label l = new Label();
            l.Text = label;
            l.AutoSize = true;
            l.Dock = DockStyle.Fill;
            NumericUpDown n = new NumericUpDown();
            n.Minimum = min;
            n.Maximum = max;
            n.Dock = DockStyle.Fill;
            n.ValueChanged += delegate { UpdateWarnings(); };
            root.Controls.Add(l);
            root.Controls.Add(n);
            return n;
        }

        private CheckBox AddCheck(TableLayoutPanel root, string label)
        {
            CheckBox c = new CheckBox();
            c.Text = label;
            c.AutoSize = true;
            c.Dock = DockStyle.Fill;
            c.CheckedChanged += delegate { UpdateWarnings(); };
            root.Controls.Add(c);
            root.SetColumnSpan(c, 2);
            return c;
        }

        private void LoadValues(AppSettings s)
        {
            snapThreshold.Value = s.SnapThreshold;
            minWidth.Value = s.MinWidth;
            minHeight.Value = s.MinHeight;
            aeroThreshold.Value = s.AeroThreshold;
            autoSnap.Value = s.AutoSnap;
            snapGap.Value = s.SnapGap;
            watchdogMs.Value = s.WatchdogMs;
            enableEdgeSnap.Checked = s.EnableEdgeSnap;
            enableAeroSnap.Checked = s.EnableAeroSnap;
            snapToWindows.Checked = s.SnapToWindows;
            enablePreviewOverlay.Checked = s.EnablePreviewOverlay;
            enablePreviewOnlySnap.Checked = s.EnablePreviewOnlySnap;
            enableRuleDiagnostics.Checked = s.EnableRuleDiagnostics;
            enableSnapDiagnostics.Checked = s.EnableSnapDiagnostics;
            enableTrayIcon.Checked = s.EnableTrayIcon;
            stickyResize.Checked = s.StickyResize;
        }

        private void UpdateWarnings()
        {
            if (warningLabel == null) return;
            StringBuilder b = new StringBuilder();
            if (snapThreshold != null && snapThreshold.Value == 0) b.AppendLine("- SnapThreshold=0 makes practical snap matching nearly disabled.");
            if (snapThreshold != null && snapThreshold.Value > 64) b.AppendLine("- High SnapThreshold may feel too sticky.");
            if (stickyResize != null && stickyResize.Checked) b.AppendLine("- StickyResize can resize adjacent windows too.");
            if (enablePreviewOnlySnap != null && enablePreviewOnlySnap.Checked) b.AppendLine("- PreviewOnlySnap delays movement until mouse release.");
            if (enablePreviewOverlay != null && enablePreviewOverlay.Checked) b.AppendLine("- PreviewOverlay shows a topmost outline window.");
            if (enableSnapDiagnostics != null && enableSnapDiagnostics.Checked) b.AppendLine("- Snap diagnostics rewrites report files frequently during gestures.");
            if (enableTrayIcon != null && enableTrayIcon.Checked) b.AppendLine("- Tray icon is optional; default is off.");
            warningLabel.Text = b.Length == 0 ? "Warnings: none" : "Warnings:\r\n" + b.ToString();
        }

        private void SaveAndReload()
        {
            ConfigFileUpdater.SetGeneralValues(CurrentGeneralValues());
            if (afterSave != null) afterSave();
            Close();
        }

        private void ResetGeneralDefaults()
        {
            ConfigFileUpdater.SetGeneralValues(ConfigDefaults.General());
            LoadValues(AppSettings.Load());
            if (afterSave != null) afterSave();
        }

        private void ResetRuleDefaults()
        {
            ConfigFileUpdater.SetBlacklistValues(ConfigDefaults.Blacklist());
            if (afterSave != null) afterSave();
        }

        private Dictionary<string, string> CurrentGeneralValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            values["SnapThreshold"] = ((int)snapThreshold.Value).ToString();
            values["MinWidth"] = ((int)minWidth.Value).ToString();
            values["MinHeight"] = ((int)minHeight.Value).ToString();
            values["AeroThreshold"] = ((int)aeroThreshold.Value).ToString();
            values["AutoSnap"] = ((int)autoSnap.Value).ToString();
            values["SnapGap"] = ((int)snapGap.Value).ToString();
            values["WatchdogMs"] = ((int)watchdogMs.Value).ToString();
            values["EnableEdgeSnap"] = enableEdgeSnap.Checked.ToString().ToLowerInvariant();
            values["EnableAeroSnap"] = enableAeroSnap.Checked.ToString().ToLowerInvariant();
            values["SnapToWindows"] = snapToWindows.Checked.ToString().ToLowerInvariant();
            values["EnablePreviewOverlay"] = enablePreviewOverlay.Checked.ToString().ToLowerInvariant();
            values["EnablePreviewOnlySnap"] = enablePreviewOnlySnap.Checked.ToString().ToLowerInvariant();
            values["EnableRuleDiagnostics"] = enableRuleDiagnostics.Checked.ToString().ToLowerInvariant();
            values["EnableSnapDiagnostics"] = enableSnapDiagnostics.Checked.ToString().ToLowerInvariant();
            values["EnableTrayIcon"] = enableTrayIcon.Checked.ToString().ToLowerInvariant();
            values["StickyResize"] = stickyResize.Checked.ToString().ToLowerInvariant();
            return values;
        }
    }
}
