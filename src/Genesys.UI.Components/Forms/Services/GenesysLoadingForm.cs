using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms.Services
{
    public class GenesysLoadingForm : Form
    {
        private readonly Timer timer;
        private int angle;

        public GenesysLoadingForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;

            Width = 130;
            Height = 130;

            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;

            DoubleBuffered = true;

            timer = new Timer
            {
                Interval = 25
            };

            timer.Tick += delegate
            {
                angle += 10;

                if (angle >= 360)
                    angle = 0;

                Invalidate();
            };

            timer.Start();
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BringToFront();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle spinnerRect = new Rectangle(
                (ClientSize.Width - 64) / 2,
                (ClientSize.Height - 64) / 2,
                64,
                64);

            using (Pen pen = new Pen(Color.DodgerBlue, 5F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawArc(
                    pen,
                    spinnerRect,
                    angle,
                    270);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
