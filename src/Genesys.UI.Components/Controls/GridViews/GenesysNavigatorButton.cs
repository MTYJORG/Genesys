using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysNavigatorButton : Control
    {
        private bool hover;
        private bool pressed;

        public GenesysNavigatorButton()
        {
            Width = 38;
            Height = 34;
            Cursor = Cursors.Hand;
            TabStop = false;
            DoubleBuffered = true;
            Font = new Font("Segoe UI Symbol", 12F, FontStyle.Bold);
            Margin = new Padding(2);
        }

        public string Symbol { get; set; }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            hover = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Enabled && e.Button == MouseButtons.Left)
            {
                pressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            //if (Enabled && pressed)
            //{
            //    pressed = false;
            //    Invalidate();
            //    OnClick(System.EventArgs.Empty);
            //}
            pressed = false;
            Invalidate();

            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(System.EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? Color.AliceBlue);

            Color backColor = Enabled && hover
                ? Color.FromArgb(235, 246, 255)
                : Color.FromArgb(248, 252, 255);

            Color borderColor = Enabled && hover
                ? Color.FromArgb(60, 130, 220)
                : Color.FromArgb(170, 205, 235);

            Color textColor = Enabled ? Color.MidnightBlue : Color.Silver;

            if (Enabled && pressed)
                backColor = Color.FromArgb(215, 235, 250);

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);

            using (var path = RoundedRect(rect, 6))
            using (var brush = new SolidBrush(backColor))
            using (var pen = new Pen(borderColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Symbol,
                Font,
                ClientRectangle,
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter);
        }
        private System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}