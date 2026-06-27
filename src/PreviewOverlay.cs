using System;
using System.Drawing;
using System.Windows.Forms;

namespace RhaegarMove
{
    internal sealed class PreviewOverlay : Form
    {
        private static PreviewOverlay current;
        private readonly Timer hideTimer;

        private PreviewOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Opacity = 0.85;
            Enabled = false;

            hideTimer = new Timer();
            hideTimer.Interval = 1200;
            hideTimer.Tick += delegate { HideOverlay(); };
        }

        public static void ShowRect(RECT rect, AppSettings settings)
        {
            if (!settings.EnablePreviewOverlay)
                return;
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            if (current == null || current.IsDisposed)
                current = new PreviewOverlay();

            current.Bounds = new Rectangle(rect.left, rect.top, rect.Width, rect.Height);
            if (!current.Visible)
                current.Show();
            current.hideTimer.Stop();
            current.hideTimer.Start();
            current.Invalidate();
        }

        public static void HideOverlay()
        {
            if (current == null || current.IsDisposed)
                return;
            current.hideTimer.Stop();
            current.Hide();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020;
                cp.ExStyle |= 0x00000080;
                cp.ExStyle |= 0x00080000;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle r = ClientRectangle;
            if (r.Width <= 1 || r.Height <= 1)
                return;
            r.Width -= 1;
            r.Height -= 1;
            using (Pen pen = new Pen(Color.DeepSkyBlue, 3))
            {
                e.Graphics.DrawRectangle(pen, r);
            }
        }
    }
}
